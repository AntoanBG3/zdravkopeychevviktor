using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using VacationManager.Data;
using VacationManager.Models;

namespace VacationManager.Controllers;

public class AccountController : Controller
{
    private readonly ApplicationDbContext _context;

    public AccountController(ApplicationDbContext context) => _context = context;

    // GET: Account/Login
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginForm form, string? returnUrl = null)
    {
        if (!ModelState.IsValid)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(form);
        }

        var user = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.Team)
            .FirstOrDefaultAsync(u => u.Username == form.Username);

        if (user == null || !BCrypt.Net.BCrypt.Verify(form.Password, user.PasswordHash))
        {
            ModelState.AddModelError(string.Empty, "Невалидно потребителско име или парола.");
            ViewData["ReturnUrl"] = returnUrl;
            return View(form);
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
        };
        var identity = new ClaimsIdentity(claims, "CookieAuth");
        await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(identity));

        HttpContext.Items["CurrentUser"] = user;

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync("CookieAuth");
        return RedirectToAction(nameof(Login));
    }
}

public class LoginForm
{
    [Required, Display(Name = "Потребителско име")]
    public string Username { get; set; } = string.Empty;

    [Required, DataType(DataType.Password), Display(Name = "Парола")]
    public string Password { get; set; } = string.Empty;
}
