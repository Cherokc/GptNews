using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using MainApp.Models;
using System.Text;
using Newtonsoft.Json;
using System.Net.Http;

public class HabrNewsBackgroundGetter : BackgroundService
{
    private readonly ILogger<HabrNewsBackgroundGetter> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public HabrNewsBackgroundGetter(ILogger<HabrNewsBackgroundGetter> logger, IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            Console.WriteLine("Starting execution");
            // Создаем новый скоуп для использования DbContext
            using (var scope = _scopeFactory.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<MyDbContext>();
                await SendHttpRequestAndUpdateDatabaseAsync(dbContext);
            }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task SendHttpRequestAndUpdateDatabaseAsync(MyDbContext dbContext)
    {
        try
        {
            using (var client = new HttpClient())
            {

                client.Timeout = TimeSpan.FromMinutes(20);

                var lastRecord = await dbContext.HabrNews
                    .OrderByDescending(t => t.Time) // or any other column that defines the order
                    .FirstOrDefaultAsync();

                Console.WriteLine(await client.GetStringAsync("http://habrparser:8080/api/News"));

                // Создаем тело запроса
                var lastRecordId = lastRecord != null ? lastRecord.Id : "";

                var content = new StringContent($"\"{lastRecordId}\"", Encoding.UTF8, "application/json");

                // Устанавливаем заголовки
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("text/plain"));

                // Отправляем запрос
                var response = await client.PostAsync("http://habrparser:8080/api/News", content);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Success got answer");
                    var stringContent = await response.Content.ReadAsStringAsync();
                    NewsItem[] newsItems = JsonConvert.DeserializeObject<NewsItem[]>(stringContent);

                    foreach (var news in newsItems)
                    {
                        if (news != null)
                        {
                            var existingHabrNews = await dbContext.HabrNews.SingleOrDefaultAsync(n => n.Link == news.Link);
                            if (existingHabrNews != null)
                                continue;

                            HabrTag[] tags = null;
                            if (news.Tags != null)
                            {
                                tags = news.Tags.Select(tag => tag == null ? null : new HabrTag(news.Link, tag)).ToArray();
                                await dbContext.HabrTags.AddRangeAsync(tags);
                            }

                            await dbContext.HabrNews.AddAsync(new HabrNews(news.Link, news.Id, news.Time, news.TimeToRead, news.Title, news.Description, news.Content ?? ""));
                        }
                    }

                    await dbContext.SaveChangesAsync();
                }
                else
                {
                    Console.WriteLine("Unluck with sending: " + response.StatusCode);
                }
            }
        }
        catch(Exception e)
        {
            Console.WriteLine("Error occured: " + e.Message);
        }
    }

    public class NewsItem
    {
        public DateTime Time { get; set; }
        public string TimeToRead { get; set; }
        public string Title { get; set; }
        public List<string> Tags { get; set; }
        public string Description { get; set; }
        public string Content { get; set; }
        public string Link { get; set; }
        public string Id { get; set; }
    }
}
