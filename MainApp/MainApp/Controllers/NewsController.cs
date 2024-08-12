using MainApp.Models;
using Microsoft.AspNetCore.Mvc;

namespace MainApp.Controllers
{
    public class NewsController : Controller
    {
        private readonly MyDbContext _context;

        public NewsController(MyDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            var tags = _context.HabrTags.ToList();
            var news = _context.HabrNews
                .Select(n => new
                {
                    n.Description,
                    n.Title,
                    n.Link
                })
                .ToList();
            var newnews = news.Select(n => new HabrNews() { Description = n.Description, Title = n.Title, Link = n.Link }).Reverse().ToList();
            
            return View(newnews);
        }
    }
}
