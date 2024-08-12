using Microsoft.Extensions.Caching.Memory;
using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace GptConnectApi
{
    public class GPT
    {
        private static readonly string token = "y0_AgAAAAAVuGSzAATuwQAAAAEDCoEQAADr5usIpPhNpZFOY3vBBWGIJQgmZg";
        private static string iamtoken;
        private static DateTime timetoken = DateTime.MinValue;
        private static readonly string folderid = "b1gos58g47rvoj43nt4c";

        private static async void InitializeAsync()
        {
            // Указываем URL для получения токена
            string tokenUrl = "https://iam.api.cloud.yandex.net/iam/v1/tokens";

            // Создаем HttpClient
            using var httpClient = new HttpClient();

            // Создаем данные для отправки
            var tokenData = new
            {
                yandexPassportOauthToken = token
            };

            // Преобразуем данные в JSON
            var jsonTokenData = JsonSerializer.Serialize(tokenData);

            // Создаем содержимое запроса
            var content = new StringContent(jsonTokenData, System.Text.Encoding.UTF8, "application/json");

            // Отправляем POST запрос
            var responseTask = httpClient.PostAsync(tokenUrl, content);
            responseTask.Wait(); // Дожидаемся завершения операции

            // Отправляем POST запрос и получаем ответ
            var response = responseTask.Result;

            // Проверяем успешность запроса
            if (response.IsSuccessStatusCode)
            {
                // Читаем содержимое ответа
                var responseContent = await response.Content.ReadAsStringAsync();

                // Десериализуем JSON ответ и получаем IAM токен
                var type = new { iamToken = "", expiresAt = "" };

                var responseData = JsonSerializer.Deserialize(responseContent, type.GetType());
                iamtoken = responseData.GetType().GetProperty("iamToken").GetValue(responseData).ToString();

                // Используем IAM токен
                Console.WriteLine("IAM токен:", iamtoken);
                timetoken = DateTime.Now;
            }
            else
            {
                Console.WriteLine($"Ошибка: {response.StatusCode}");
            }
        }

        public GPT()
        {
            Date = DateTime.Now;
        }

        public DateTime Date { get; set; }

        public string Answer { get; set; }

        public async Task<int> SendRequest(string request)
        {
            if ((DateTime.Now - timetoken).TotalMinutes > 60)
            {
                InitializeAsync();
            }

            await Tokenizer(request);
            // Задаем данные для запроса
            var requestData = new
            {
                modelUri = $"gpt://{folderid}/yandexgpt-lite/latest",
                completionOptions = new
                {
                    stream = false,
                    temperature = 0.1,
                    maxTokens = "1000000"
                },
                messages = new object[]
                {
                        new { role = "user", text = request },
                        //new { role = "user", text = "" }
                }
            };

            // Преобразуем данные в JSON
            string jsonData = JsonSerializer.Serialize(requestData);

            // Задаем URL
            string url = "https://llm.api.cloud.yandex.net/foundationModels/v1/completion";

            // Создаем HttpClient
            using var httpClient = new HttpClient();

            // Задаем заголовки
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {iamtoken}");
            httpClient.DefaultRequestHeaders.Add("x-folder-id", folderid);

            // Создаем содержимое запроса
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            // Отправляем POST запрос
            var responseTask = httpClient.PostAsync(url, content);
            responseTask.Wait(); // Дожидаемся завершения операции

            var response = responseTask.Result; // Получаем результат операции

            // Обрабатываем ответ
            var answer = "";
            if (response.IsSuccessStatusCode)
            {
                var type = new
                {
                    result = new
                    {
                        alternatives = new[]
                        {
                            new
                            {
                                message = new
                                {
                                    role = "",
                                    text = ""
                                },
                                status = ""
                            }
                        },
                        usage = new
                        {
                            inputTextTokens = "",
                            completionTokens = "",
                            totalTokens = ""
                        },
                        modelVersion = ""
                    }
                };
                var responseContent = await response.Content.ReadAsStringAsync();

                var responseData = JsonSerializer.Deserialize(responseContent, type.GetType());
                type = CastTo(responseData, type);
                answer = type.result.alternatives[0].message.text;
            }
            else
            {
                answer = $"Произошла ошибка: {response.StatusCode}";
            }

            Answer = answer;
            return 0;
        }

        public static T CastTo<T>(object value, T targetType)
        {
            // targetType above is just for compiler magic
            // to infer the type to cast value to
            return (T)value;
        }

        public async Task<int> Tokenizer(string request)
        {
            var body = new
            {
                modelUri = $"gpt://{folderid}/yandexgpt-lite/latest",
                completionOptions = new {
                stream = false,
                temperature = 0.1,
                maxTokens = 8000
                },
                messages = new[] { 
                    new {
                        role = "assistant",
                        text = request
                    }
                }
            };

            string jsonData = JsonSerializer.Serialize(body);

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    var url = "https://llm.api.cloud.yandex.net/foundationModels/v1/tokenizeCompletion";

                    var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                    Console.WriteLine(jsonData);
                    // Задаем заголовки
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {iamtoken}");
                    client.DefaultRequestHeaders.Add("x-folder-id", folderid);

                    HttpResponseMessage response = await client.PostAsync(url, content);
                    response.EnsureSuccessStatusCode(); // Выбрасывает исключение, если код состояния не 2xx

                    string responseBody = await response.Content.ReadAsStringAsync();

                    var model = new
                    {
                        tokens = new[]
                        {
                            new { id = "string", text = "string", special = true }
                        },
                        modelVersion = "string"
                    };

                    var responseData = JsonSerializer.Deserialize(responseBody, model.GetType());
                    model = CastTo(responseData, model);

                    Console.WriteLine(model.tokens.Count());

                    return model.tokens.Count();
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine($"Запрос завершился с ошибкой: {e.Message}, iamtoken: {iamtoken}");
                }
            }

            return 0;
        }

        public async Task<string> GetTimeBorders(string request)
        {
            if ((DateTime.Now - timetoken).TotalMinutes > 60)
            {
                InitializeAsync();
            }

            Console.WriteLine("GetTimeBorders");

            var systemText = $"Твоя задача - ответить в таком виде: \"X Y\", где X - конечная дата, " +
                $"Y - начальная дата. Дата формируется в таком виде: dd.mm.yyyy.\r\nТы должен выдать " +
                $"ответ в соответствии с запросом, вычленив оттуда запрашиваемые даты. Если даты не " +
                $"удалось выявить, то отвечай \"{DateTime.Now.ToString("dd.MM.yyyy")} {DateTime.Now.AddDays(-7).ToString("dd.MM.yyyy")}\".\r\nСегодняшняя дата - {DateTime.Now.ToString("dd.MM.yyyy")}";

            var data = new
            {
                modelUri = $"gpt://{folderid}/yandexgpt/latest",
                completionOptions = new
                {
                    stream = false,
                    temperature = 0.1,
                    maxTokens = "2000"
                },
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        text = systemText
                    },
                    new
                    {
                        role = "user",
                        text = request
                    }
                }
            };

            // Преобразуем данные в JSON
            string jsonData = JsonSerializer.Serialize(data);
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            // Задаем URL
            string url = "https://llm.api.cloud.yandex.net/foundationModels/v1/completion";

            // Создаем HttpClient
            using var httpClient = new HttpClient();

            // Задаем заголовки
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {iamtoken}");
            httpClient.DefaultRequestHeaders.Add("x-folder-id", folderid);


            // Отправляем POST запрос
            var responseTask = httpClient.PostAsync(url, content);
            responseTask.Wait(); // Дожидаемся завершения операции

            var response = responseTask.Result; // Получаем результат операции

            // Обрабатываем ответ
            var answer = "";
            if (response.IsSuccessStatusCode)
            {
                var type = new
                {
                    result = new
                    {
                        alternatives = new[]
                        {
                            new
                            {
                                message = new
                                {
                                    role = "",
                                    text = ""
                                },
                                status = ""
                            }
                        },
                        usage = new
                        {
                            inputTextTokens = "",
                            completionTokens = "",
                            totalTokens = ""
                        },
                        modelVersion = ""
                    }
                };
                var responseContent = await response.Content.ReadAsStringAsync();

                var responseData = JsonSerializer.Deserialize(responseContent, type.GetType());
                type = CastTo(responseData, type);
                answer = type.result.alternatives[0].message.text;
            }
            else
            {
                answer = $"Произошла ошибка: {response.StatusCode}";
            }
            return answer;
        }

        public async Task<string> GetSpecialTitles(string request)
        {
            if ((DateTime.Now - timetoken).TotalMinutes > 60)
            {
                InitializeAsync();
            }

            Console.WriteLine("GetSpecialTitles");

            var systemText = $"Твоя задача - дать ответ в виде чисел, строго разделенных пробелами, никаких запятых, точек, " +
                $"переносов строк. Ответ формируется на основе запроса, в котором содержится просьба и набор записей. Тебе " +
                $"необходимо выбрать подходящие записи и выдать их числа. Если ничего подходящего нет, выведи 0.\r\nТы - очень " +
                $"умный, поэтому должен извлечь из просьбы необходимую тему. На основе этой темы ты должен выбрать записи, " +
                $"похожие на тему.\r\nЗапись представляет собой такую форму: номер: \"Заголовок\", теги через запятую.\r\n" +
                $"Выбирай записи очень осмысленно и не бери что попало. Выбирай не более 10 элементов.\r\nВЫДАВАЙ НОМЕРА ЧЕРЕЗ ПРОБЕЛ";
            Console.WriteLine(systemText);

            var data = new
            {
                modelUri = $"gpt://{folderid}/yandexgpt/latest",
                completionOptions = new
                {
                    stream = false,
                    temperature = 0.1,
                    maxTokens = "8000"
                },
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        text = systemText
                    },
                    new
                    {
                        role = "user",
                        text = request
                    }
                }
            };

            // Преобразуем данные в JSON
            string jsonData = JsonSerializer.Serialize(data);
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            // Задаем URL
            string url = "https://llm.api.cloud.yandex.net/foundationModels/v1/completion";

            // Создаем HttpClient
            using var httpClient = new HttpClient();

            // Задаем заголовки
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {iamtoken}");
            httpClient.DefaultRequestHeaders.Add("x-folder-id", folderid);


            // Отправляем POST запрос
            var responseTask = httpClient.PostAsync(url, content);
            responseTask.Wait(); // Дожидаемся завершения операции

            var response = responseTask.Result; // Получаем результат операции

            // Обрабатываем ответ
            var answer = "";
            if (response.IsSuccessStatusCode)
            {
                var type = new
                {
                    result = new
                    {
                        alternatives = new[]
                        {
                            new
                            {
                                message = new
                                {
                                    role = "",
                                    text = ""
                                },
                                status = ""
                            }
                        },
                        usage = new
                        {
                            inputTextTokens = "",
                            completionTokens = "",
                            totalTokens = ""
                        },
                        modelVersion = ""
                    }
                };
                var responseContent = await response.Content.ReadAsStringAsync();

                var responseData = JsonSerializer.Deserialize(responseContent, type.GetType());
                type = CastTo(responseData, type);
                answer = type.result.alternatives[0].message.text;
            }
            else
            {
                answer = $"Произошла ошибка: {response.StatusCode}";
            }

            return answer;
        }

        public async Task<string> GetGptSummarize(string request)
        {
            if ((DateTime.Now - timetoken).TotalMinutes > 60)
            {
                InitializeAsync();
            }

            Console.WriteLine("GetGptSummarize");

            var systemText = $"Тебе нужно кратко пересказать содержимое запроса. Напиши только пересказ, не надо писать про то, что ты делал.";

            var data = new
            {
                modelUri = $"gpt://{folderid}/yandexgpt-lite/latest",
                completionOptions = new
                {
                    stream = false,
                    temperature = 0.1,
                    maxTokens = "8000"
                },
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        text = systemText
                    },
                    new
                    {
                        role = "user",
                        text = request
                    }
                }
            };

            // Преобразуем данные в JSON
            string jsonData = JsonSerializer.Serialize(data);
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            // Задаем URL
            string url = "https://llm.api.cloud.yandex.net/foundationModels/v1/completion";

            // Создаем HttpClient
            using var httpClient = new HttpClient();

            // Задаем заголовки
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {iamtoken}");
            httpClient.DefaultRequestHeaders.Add("x-folder-id", folderid);


            // Отправляем POST запрос
            var responseTask = httpClient.PostAsync(url, content);
            responseTask.Wait(); // Дожидаемся завершения операции

            var response = responseTask.Result; // Получаем результат операции

            // Обрабатываем ответ
            var answer = "";
            if (response.IsSuccessStatusCode)
            {
                var type = new
                {
                    result = new
                    {
                        alternatives = new[]
                        {
                            new
                            {
                                message = new
                                {
                                    role = "",
                                    text = ""
                                },
                                status = ""
                            }
                        },
                        usage = new
                        {
                            inputTextTokens = "",
                            completionTokens = "",
                            totalTokens = ""
                        },
                        modelVersion = ""
                    }
                };
                var responseContent = await response.Content.ReadAsStringAsync();

                var responseData = JsonSerializer.Deserialize(responseContent, type.GetType());
                type = CastTo(responseData, type);
                answer = type.result.alternatives[0].message.text;
            }
            else
            {
                answer = $"Произошла ошибка: {response.StatusCode}";
            }

            return answer;
        }
    }
}
