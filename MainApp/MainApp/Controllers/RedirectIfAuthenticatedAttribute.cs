using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using MainApp.Models;

namespace MainApp.Controllers
{
    public class RedirectIfAuthenticatedAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (context.HttpContext.User.Identity.IsAuthenticated)
            {
                // Перенаправление на другую страницу, если пользователь авторизован
                context.Result = new RedirectToActionResult("Index", "Home", null);
            }
        }
    }



    [AttributeUsage(AttributeTargets.Method)]
    public class RedirectIfWrongUserAttribute : ActionFilterAttribute, IAuthorizationFilter
    {
        private MyDbContext _dbContext;

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ActionArguments.ContainsKey("id"))
                return;

            int chatId = (int)context.ActionArguments["id"];
            var chat = _dbContext.Chats
                             .FirstOrDefault(c => c.Id == chatId);

            // Проверка аутентифицированного пользователя и имени пользователя чата
            if (context.HttpContext.User.Identity.Name == null || chat == null || context.HttpContext.User.Identity.Name != chat.Username)
            {
                // Перенаправление на другую страницу, если пользователь авторизован
                context.Result = new RedirectToActionResult("Index", "Chats", null);
            }
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            _dbContext = context.HttpContext
                                   .RequestServices
                                   .GetService(typeof(MyDbContext)) as MyDbContext;
        }
    }
}
