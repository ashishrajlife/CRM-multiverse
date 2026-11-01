using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using MultiverseCRM.Data;
using System.Security.Claims;

namespace MultiverseCRM.Controllers;

public class AccountController : Controller
{
    private readonly AppDbContext _context;

    public AccountController(AppDbContext context) => _context = context;

    [HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToAction("Index", "Dashboard");
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Login(string username, string password)
    {
        var ajax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";

        var user = _context.Users.FirstOrDefault(u =>
            u.Username == username && u.Password == password);

        if (user == null)
        {
            if (ajax) return Json(new { success = false, message = "Invalid username or password." });
            ViewBag.Error = "Invalid credentials";
            return View();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim("FullName", user.FullName),
            new Claim(ClaimTypes.Role, user.Role)
        };

        await HttpContext.SignInAsync(
            "MyCookieAuth",
            new ClaimsPrincipal(new ClaimsIdentity(claims, "MyCookieAuth")));

        var target = Url.Action("Index", "Dashboard")!;
        return ajax ? Json(new { success = true, redirectUrl = target }) : Redirect(target);
    }

    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("MyCookieAuth");
        return RedirectToAction("Login");
    }
}