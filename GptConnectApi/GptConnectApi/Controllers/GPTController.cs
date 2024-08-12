using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace GptConnectApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GPTController : ControllerBase
    {
        private readonly ILogger<GPTController> _logger;

        public GPTController(ILogger<GPTController> logger)
        {
            _logger = logger;
        }

        [HttpGet(Name = "GetAPI")]
        public string Get()
        {
            return "Это Yandex GPT API";
        }
        [DisableRequestSizeLimit]
        [HttpPost("GetGPTAnswer", Name = "GetGPTAnswer")]
        public async Task<GPT> GetGPTAnswer([FromBody] string text)
        {
            var gpt = new GPT();
            await gpt.SendRequest(text);
            return gpt;    
        }
        [DisableRequestSizeLimit]
        [HttpPost("GetTimeBorders", Name = "GetTimeBorders")]
        public async Task<string> GetTimeBorders([FromBody] string text)
        {
            var gpt = new GPT();
            return await gpt.GetTimeBorders(text);
        }
        [DisableRequestSizeLimit]
        [HttpPost("GetSpecialTitles", Name = "GetSpecialTitles")]
        public async Task<string> GetSpecialTitles([FromBody] string text)
        {
            Console.WriteLine(text);
            var gpt = new GPT();
            return await gpt.GetSpecialTitles(text);
        }
        [DisableRequestSizeLimit]
        [HttpPost("GetGPTSummarize", Name = "GetGPTSummarize")]
        public async Task<string> GetGPTSummarize([FromBody] Message message)
        {
            var gpt = new GPT();
            return await gpt.GetGptSummarize(message.content);
        }

        private static T CastTo<T>(object value, T targetType)
        {
            // targetType above is just for compiler magic
            // to infer the type to cast value to
            return (T)value;
        }
    }

    public class Message
    {
        public string content { get; set; }
    }
}
