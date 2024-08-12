using MainApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
using System;
using System.Globalization;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace MainApp.Controllers
{
    public class ChatController : Controller
    {
        private readonly MyDbContext _context;
        private readonly HttpClient _httpClient;
        const long ConstTicks = 0;
        public ChatController(MyDbContext context)
        {
            _context = context;
            _httpClient = new HttpClient();
        }

        [RedirectIfWrongUser]
        public async Task<IActionResult> Index(int id, bool success = true)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            var model = new ChatViewModel();
            model.ChatId = id;
            model.Messages = _context.Messages.Where(m => m.ChatId == id).ToList();
            model.Chats = _context.Chats.Where(c => c.Username == User.Identity.Name).ToList();
            model.LastTimeMessage = model.Chats.Select(c => _context.Messages.Where(m => m.ChatId == c.Id).OrderBy(m => m.Time).LastOrDefault() != null ? _context.Messages.Where(m => m.ChatId == c.Id).OrderBy(m => m.Time).LastOrDefault().Time : DateTime.MinValue).ToList();
            model.Success = success;
            return View(model);
        }

        [RedirectIfWrongUser]
        [HttpPost]
        public async Task<IActionResult> Send(ChatViewModel chatVM)
        {
            bool chatIsFake = false;
            if (chatVM.ChatId == 0)
            {
                var chat = new Chat() { Username = User.Identity.Name, GPT = "YandexGPT", Source = "Habr" };
                try
                {
                    _context.Add(chat);
                    await _context.SaveChangesAsync();
                    chatVM.ChatId = chat.Id;
                    chatIsFake = true;
                }
                catch(Exception e)
                {
                    if (chat.Id > 0)
                    {
                        _context.Chats.Remove(chat);
                        await _context.SaveChangesAsync();
                    }
                    Console.WriteLine("Ошибка Send при создании фейк чата");
                    return RedirectToAction("Index", "Chat", new { id = chatVM.ChatId, success = false });
                }
            }
            
            var message = new Message();
            message.Text = chatVM.NewMessageText;
            message.By = true;
            message.ChatId = chatVM.ChatId;

            //if (!ModelState.IsValid)
            //{
            //    if(chatIsFake)
            //    {
            //        var fakechat = _context.Chats.FirstOrDefault(c => c.Id == chatVM.ChatId);
            //        if (fakechat != null)
            //        {
            //            _context.Chats.Remove(fakechat);
            //            await _context.SaveChangesAsync();
            //        }
            //    }
            //    Console.WriteLine("Ошибка Send: ModelState Is Invalid");
            //    return RedirectToAction("Index", "Chat", new { id = chatVM.ChatId, success = false });
            //}

            Console.WriteLine("Попытка отправки сообщения");
            Console.WriteLine(await _httpClient.GetAsync("http://gptapi:8080/GPT"));

            var timeBorders = await GetTimeBorders(message);

            var news = _context.HabrNews.Where(n => n.Time >= timeBorders.Item1 && n.Time <= timeBorders.Item2).ToList();
            Console.WriteLine($"Нашлось {news.Count()} новостей за даты: {timeBorders.Item1}-{timeBorders.Item2}");

            if(news.Count == 0)
            {
                if (chatIsFake)
                {
                    var fakechat = _context.Chats.FirstOrDefault(c => c.Id == chatVM.ChatId);
                    if (fakechat != null)
                    {
                        _context.Chats.Remove(fakechat);
                        await _context.SaveChangesAsync();
                    }
                }
                Console.WriteLine("Ошибка Send: не нашлось новостей");
                return RedirectToAction("Index", "Chat", new { id = chatVM.ChatId, success = false });
            }

            var tags = _context.HabrTags.ToList();

            var formattedNews = news.Select(n => $"{n.Id}:{n.Title}," + string.Join(',', tags.Where(t => t.Link == n.Link).Select(t => t.Name))).ToArray();

            var specialIds = await GetSpecialTitles(formattedNews,message);

            if(specialIds.Count > 0)
            {
                if(specialIds.Count > 10)
                    specialIds.RemoveRange(10, specialIds.Count - 10);

                var gptAnswer = new List<string>();

                foreach (var id in specialIds)
                {
                    var specialNews = news.FirstOrDefault(n => n.Id == id.ToString());
                    if(specialNews != null && specialNews.Content?.Length > 40)
                    {
                        var answer = await GetGptSummarize(specialNews.Content, chatVM);
                        gptAnswer.Add($"<a style=\"font-weight: bold;\" href=\"{specialNews.Link}\">{specialNews.Title}</a>\n <div>{answer}</div>");
                    }
                }

                var gptMessage = new Message() { ChatId = message.ChatId, Time = DateTime.Now.ToUniversalTime(), By = false, Text = $"Вот что нашлось за даты {timeBorders.Item1.ToString("dd.MM.yyyy")}-{timeBorders.Item2.ToString("dd.MM.yyyy")}:\n\n" + string.Join("\n\n", gptAnswer) };

                try
                {
                    _context.Add(message);
                    _context.Add(gptMessage);
                    await _context.SaveChangesAsync();
                    chatIsFake = false;
                    var messages = chatVM.Messages;
                    if (messages == null)
                        messages = new List<Message>();
                    messages.Add(message);
                    return RedirectToAction("Index", new { id = message.ChatId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("Text", "Ошибка базы данных: " + ex.Message);
                }
            }
            else
            {
                Console.WriteLine("Ошибка: не удалось извлечь новости");
                if (chatIsFake)
                {
                    var fakechat = _context.Chats.FirstOrDefault(c => c.Id == chatVM.ChatId);
                    if (fakechat != null)
                    {
                        _context.Chats.Remove(fakechat);
                        await _context.SaveChangesAsync();
                    }
                }
                return RedirectToAction("Index", "Chat", new { id = chatVM.ChatId, success = false });
            }

            if (chatIsFake)
            {
                var fakechat = _context.Chats.FirstOrDefault(c => c.Id == chatVM.ChatId);
                if (fakechat != null)
                {
                    _context.Chats.Remove(fakechat);
                    await _context.SaveChangesAsync();
                }
            }
            Console.WriteLine("Ошибка Send: конец метода");
            return RedirectToAction("Index", "Chat", new { id = chatVM.ChatId, success = false });
        }

        public async Task<(DateTime, DateTime)> GetTimeBorders(Message message)
        {
            var responseTimeBorders = await _httpClient.PostAsync("http://gptapi:8080/GPT/GetTimeBorders", new StringContent($"\"{message.Text}\"", Encoding.UTF8, "application/json"));
            var timeBorders = (DateTime.Now.ToUniversalTime().AddDays(-7), DateTime.Now.ToUniversalTime());
            if (responseTimeBorders.IsSuccessStatusCode)
            {
                string responseTimeBordersContent = await responseTimeBorders.Content.ReadAsStringAsync();
                Console.WriteLine($"TimeBorders: {responseTimeBordersContent}");
                var splits = responseTimeBordersContent.Split(' ');
                if (splits.Length == 2)
                {
                    try
                    {
                        DateTime date1 = DateTime.ParseExact(splits[0], "dd.MM.yyyy", CultureInfo.InvariantCulture).ToUniversalTime();
                        DateTime date2 = DateTime.ParseExact(splits[1], "dd.MM.yyyy", CultureInfo.InvariantCulture).ToUniversalTime();
                        if (date1 > date2)
                            timeBorders = (date2, date1);
                        else
                            timeBorders = (date1, date2);
                    }
                    catch (Exception ex) { }
                }
            }
            timeBorders.Item2.AddHours(23).AddMinutes(59).AddSeconds(59);
            return timeBorders;
        }

        public async Task<List<int>> GetSpecialTitles(string[] formattedNews, Message message)
        {
            var specialIds = new List<int>();
            var i = 0;
            while(i < formattedNews.Length)
            {
                var array = new string[Math.Min(10, formattedNews.Length-i)];
                Array.Copy(formattedNews, i, array, 0, array.Length);

                var formattedNewsRequest = $"Просьба: {message.Text}\\nМассив записей:\\n" + string.Join("\\n", array);
                formattedNewsRequest.Replace("\"", "\\\"");

                Console.WriteLine(formattedNewsRequest);

                var responseSpecialTitles = await _httpClient.PostAsync("http://gptapi:8080/GPT/GetSpecialTitles", new StringContent($"\"{formattedNewsRequest}\"", Encoding.UTF8, "application/json"));
                if (responseSpecialTitles.IsSuccessStatusCode)
                {
                    string responseTimeBordersContent = await responseSpecialTitles.Content.ReadAsStringAsync();
                    Console.WriteLine("responseTimeBordersContent: " + responseTimeBordersContent);
                    var splits = responseTimeBordersContent.Split(' ');
                    if (splits.Length != 0)
                    {
                        var numbers = splits
                                        .Select(s => { int.TryParse(s, out int n); return n; })
                                        .Where(s => s != 0);
                        specialIds.AddRange(numbers);
                    }
                }
                else
                {
                    Console.WriteLine(await responseSpecialTitles.Content.ReadAsStringAsync());
                    Console.WriteLine(responseSpecialTitles.RequestMessage);
                }

                i += 10;
            }
            return specialIds;
        }

        //public async Task<string> GetGptAnswer(Message message, ChatViewModel chatVM)
        //{
        //    // Отправляем запрос в другой контейнер
        //    var response = await _httpClient.PostAsync("http://gptapi:8080/GPT/GetGptAnswer", new StringContent($"\"{message.Text}\"", Encoding.UTF8, "application/json"));

        //    // Проверяем успешность запроса
        //    if (response.IsSuccessStatusCode)
        //    {
        //        var gptAnswer = new { date = DateTime.Now, answer = "" };
        //        // Получаем ответ
        //        string responseContent = await response.Content.ReadAsStringAsync();
        //        var responseData = JsonSerializer.Deserialize(responseContent, gptAnswer.GetType());
        //        gptAnswer = CastTo(responseData, gptAnswer);

                //var gptMessage = new Message() { ChatId = message.ChatId, Time = gptAnswer.date, By = false, Text = gptAnswer.answer };

                //try
                //{
                //    _context.Add(message);
                //    _context.Add(gptMessage);
                //    await _context.SaveChangesAsync();
                //    var messages = chatVM.Messages;
                //    if (messages == null)
                //        messages = new List<Message>();
                //    messages.Add(message);
                //    return RedirectToAction("Index", new { id = message.ChatId });
                //}
                //catch (Exception ex)
                //{
                //    ModelState.AddModelError("Text", "Ошибка базы данных: " + ex.Message);
                //}

        //        // Возвращаем ответ в представление
        //        return null;
        //    }
        //}

        public async Task<string> GetGptSummarize(string message, ChatViewModel chatVM)
        {
            var data = new { content = message };
            message = JsonSerializer.Serialize(data);
            Console.WriteLine(message);
            // Отправляем запрос в другой контейнер
            var response = await _httpClient.PostAsync("http://gptapi:8080/GPT/GetGPTSummarize", new StringContent(message, Encoding.UTF8, "application/json"));

            message.Replace("\"", "\\\"");
            // Проверяем успешность запроса
            if (response.IsSuccessStatusCode)
            {
                // Получаем ответ
                string responseContent = await response.Content.ReadAsStringAsync();

                // Возвращаем ответ в представление
                return responseContent;
            }
            else
                Console.WriteLine(await response.Content.ReadAsStringAsync());

            return null;
        }

        private static T CastTo<T>(object value, T targetType)
        {
            // targetType above is just for compiler magic
            // to infer the type to cast value to
            return (T)value;
        }
    }
}
