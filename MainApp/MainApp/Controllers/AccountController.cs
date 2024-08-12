using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Npgsql;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using MainApp.Models;

namespace MainApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly MyDbContext _context;

        public AccountController(MyDbContext context)
        {
            _context = context;
        }

        [RedirectIfAuthenticated]
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [RedirectIfAuthenticated]
        [HttpPost]
        public async Task<IActionResult> Register(User user)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    _context.Add(user);
                    await _context.SaveChangesAsync();

                    // После успешной регистрации, выполняем аутентификацию пользователя
                    var claims = new[] { new Claim(ClaimTypes.Name, user.Username) };
                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);
                    HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                    return RedirectToAction("Index", "Home");
                }
                catch (Exception ex)
                {
                    var p_ex = ex.InnerException as PostgresException;
                    if (p_ex.SqlState == "23505")
                    {
                        // Ошибка связана с нарушением ограничения уникальности
                        ModelState.AddModelError("Username", "Данный пользователь уже зарегистрирован.");
                    }
                    else
                    {
                        // Обработка других ошибок
                        ModelState.AddModelError("", "Ошибка базы данных: " + ex.Message);
                    }
                }
            }
            return View(user);
        }

        [RedirectIfAuthenticated]
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [RedirectIfAuthenticated]
        [HttpPost]
        public async Task<IActionResult> Login(User user)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var userFromDb = await _context.Users.FirstOrDefaultAsync(u => u.Username == user.Username);
                    if (userFromDb == null || userFromDb.Password != _context.Encrypt(user.Password))
                    {
                        ModelState.AddModelError("Username", "Неверное имя или пароль.");
                        return View();
                    }

                    // После успешной регистрации, выполняем аутентификацию пользователя
                    var claims = new[] { new Claim(ClaimTypes.Name, user.Username) };
                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);
                    HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                    return RedirectToAction("Index", "Home");
                }
                catch (PostgresException ex)
                {   
                    // Обработка других ошибок
                    ModelState.AddModelError("", "Ошибка базы данных: " + ex.Message);
                }
            }
            return View(user);
        }

        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }
    }
}
