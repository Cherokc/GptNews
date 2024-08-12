using HtmlAgilityPack;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Globalization;
using static System.Reflection.Metadata.BlobBuilder;

namespace HabrParser.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NewsController : ControllerBase
    {
        private readonly ILogger<NewsController> _logger;

        public NewsController(ILogger<NewsController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public string Get()
        {
            return "Это HabrParser API";
        }

        [HttpPost]
        public async Task<News[]> Post([FromBody] string lastNews)
        {
            Console.WriteLine("Got post sygnal");
            var url = "https://habr.com/ru/news/page";
            var lastId = 0;
            if (lastNews.Length > 0)
                int.TryParse(lastNews, out lastId);

            News debugNews;

            List<News> newsList = new List<News>();
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    List<Task<HttpResponseMessage>> tasksForHTML = new List<Task<HttpResponseMessage>>();
                    var responseList = new List<HttpResponseMessage>();
                    for (int i = 1; i <= 50; i++)
                    {
                        var aa = await client.GetAsync(url + i.ToString() + "/");
                        responseList.Add(aa);
                    }

                    var results = responseList.ToArray();

                    tasksForHTML.Clear();

                    List<Task<string>> tasksForContent = new List<Task<string>>();
                    List<Task<List<(HtmlNode, string)>>> tasksForNodes = new List<Task<List<(HtmlNode, string)>>>();
                    List<Task<News>> tasksForNews = new List<Task<News>>();

                    foreach (var result in results)
                    {
                        if (result.IsSuccessStatusCode)
                            tasksForContent.Add(result.Content.ReadAsStringAsync());
                    }

                    var contents = await Task.WhenAll(tasksForContent);
                    tasksForContent.Clear();

                    foreach (var content in contents)
                    {
                        tasksForNodes.Add(ExtractBlocksAsync(content, lastId));
                    }

                    var nodeListArray = await Task.WhenAll(tasksForNodes);

                    int j = 0;
                    foreach (var nodeList in nodeListArray)
                    {
                        foreach (var node in nodeList)
                        {
                            newsList.Add(new News());
                            tasksForNews.Add(FindNecessaryDataAsync(node.Item1, newsList[j]));
                            j++;
                        }
                    }

                    await Task.WhenAll(tasksForNews);

                    responseList = new List<HttpResponseMessage>();
                    var k = 1;
                    foreach (var page in newsList)
                    {
                        debugNews = page;
                        Console.WriteLine(k++ + " " + page.Link);
                        responseList.Add(await client.GetAsync(page.Link));
                    }

                    results = responseList.ToArray();

                    foreach (var result in results)
                    {
                        result.EnsureSuccessStatusCode();
                        tasksForContent.Add(result.Content.ReadAsStringAsync());
                    }

                    contents = await Task.WhenAll(tasksForContent);
                    tasksForNodes.Clear();

                    foreach (var content in contents)
                    {
                        tasksForNodes.Add(ExtractBlocksAsync(content, lastId, 2));
                    }

                    nodeListArray = await Task.WhenAll(tasksForNodes);

                    k = 1;
                    foreach (var nodeList in nodeListArray)
                    {
                        foreach (var node in nodeList)
                        {
                            Console.WriteLine(k++ + " " + node.Item2);
                            tasksForNews.Add(GetArticleContentAsync(newsList, node));
                        }
                    }

                    await Task.WhenAll(tasksForNews);
                }
            }
            catch(Exception e)
            {

            }

            return newsList.ToArray();
        }

        public static async Task<List<(HtmlNode,string)>> ExtractBlocksAsync(string html, int lastId, int stage = 1)
        {
            return await Task.Run(() =>
            {
                var blocks = new List<(HtmlNode, string)>();

                // Load the HTML document
                var doc = new HtmlDocument();
                doc.LoadHtml(html);

                // Extract all block elements (div, p, section, article, etc.)
                var blockElements = doc.DocumentNode.SelectNodes("//article");

                if (stage == 2)
                {
                    var metaNode = doc.DocumentNode.SelectNodes("//meta")
                            .FirstOrDefault(n => n.Attributes.Any(a => a.Name == "property" && a.Value == "aiturec:item_id"));

                    string id = "";
                    if (metaNode != null)
                        id = metaNode.Attributes
                                     .FirstOrDefault(a => a.Name == "content")
                                     .Value;

                    if(id.Length != 0)
                        blocks.Add((blockElements.FirstOrDefault(), id));

                    return blocks;
                }

                var links = doc.DocumentNode.SelectNodes("//a[@class='tm-title__link']");

                var i = 0;
                if (blockElements != null)
                {
                    foreach (var element in blockElements)
                    {
                        var temp = links[i].Attributes.FirstOrDefault(a => a.Name == "href").Value.Split('/');
                        if (int.Parse(temp[temp.Length - 2]) != lastId)
                            blocks.Add((element, ""));
                        else 
                            break;
                        i++;
                    }
                }

                return blocks;
            });
        }

        static async Task<News> FindNecessaryDataAsync(HtmlNode block, News news)
        {
            return await Task.Run(() =>
            {
                foreach (var child in block.ChildNodes)
                {
                    if (child.Name != "div" && child.Name != "span" && child.Name != "a" && child.Name != "h2")
                        continue;

                    var classes = child.GetClasses();

                    if (classes.Contains("tm-article-datetime-published"))
                    {
                        string dateTimeString = child.ChildNodes.FirstOrDefault(c => c.Name == "time").Attributes.FirstOrDefault(a => a.Name == "datetime").Value;
                        string format = "yyyy-MM-ddTHH:mm:ss.fffZ";
                        news.Time = DateTime.ParseExact(dateTimeString, format, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal);
                    }

                    if (classes.Contains("tm-title__link"))
                    {
                        news.Title = child.InnerText;
                    }

                    if (classes.Contains("tm-article-reading-time__label"))
                    {
                        news.TimeToRead = child.InnerText;
                    }

                    if (classes.Contains("article-formatted-body"))
                    {
                        news.Description = child.InnerText.Replace("&nbsp;"," ");
                    }

                    if (classes.Contains("tm-title__link"))
                    {
                        news.Link = "https://habr.com" + child.Attributes.FirstOrDefault(a => a.Name == "href").Value;
                        news.Id = news.Link.Split('/')[news.Link.Split('/').Length - 2];

                    }


                    FindNecessaryDataAsync(child, news);
                }

                return news;
            });
        }

        static async Task<News> GetArticleContentAsync(List<News> news, (HtmlNode,string) blockTuple)
        {
            return await Task.Run(() =>
            {
                var block = blockTuple.Item1;

                var specNews = news.FirstOrDefault(s => s.Id == blockTuple.Item2);
                specNews.Content = String.Join('\n',ExtractParagraphs(block.InnerHtml));

                specNews.Tags = ExtractTags(block.InnerHtml).ToArray();

                return specNews;
            });
        }

        public static List<string> ExtractParagraphs(string html)
        {
            var paragraphs = new List<string>();

            // Load the HTML document
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Extract all <p> elements
            var pElements = doc.DocumentNode.SelectNodes("//p");

            if (pElements != null)
            {
                paragraphs = pElements.Select(p => p.InnerText.Trim()).ToList();
            }

            return paragraphs;
        }

        public static List<string> ExtractTags(string html)
        {
            var tags = new List<string>();

            // Load the HTML document
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            // Extract all <span> elements inside <a> elements
            var spanElements = doc.DocumentNode.SelectNodes("//li[@class='tm-separated-list__item']/a/span");

            if (spanElements != null)
            {
                tags = spanElements.Select(span => span.InnerText).ToList();
            }

            return tags;
        }
    }
}
