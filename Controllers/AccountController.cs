using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PhotoGallery.Data;
using PhotoGallery.ViewModels;

namespace PhotoGallery.Controllers;

public class AccountController : Controller
{
    private readonly ILogger<AccountController> _logger;
    private readonly PhotoGalleryDbContext _photoGalleryDbContext;

    public AccountController(ILogger<AccountController> logger, PhotoGalleryDbContext photoGalleryDbContext)
    {
        _logger = logger;
        _photoGalleryDbContext = photoGalleryDbContext;
    }

    public IActionResult Login()
    {
        var loginViewModel = new LoginViewModel
        {
            ReturnUrl = Request.Query["ReturnUrl"]
        };

        return View(loginViewModel);
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel loginViewModel)
    {
        if (!ModelState.IsValid)
        {
            return View(loginViewModel);
        }

        var user = await _photoGalleryDbContext.Users.FirstOrDefaultAsync(x => x.Username == loginViewModel.Username);
        if (user != null)
        {
            var hashedPassword = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: loginViewModel.Password!,
                salt: Convert.FromBase64String(user.Salt!),
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: 100000,
                numBytesRequested: 256 / 8));

            if (user.Password == hashedPassword)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, loginViewModel!.Username!),
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
                };
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = loginViewModel.RememberMe,
                    RedirectUri = loginViewModel.ReturnUrl
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return LocalRedirect(authProperties!.RedirectUri!);
            }
        }

        ModelState.AddModelError("", "Invalid username or password");
        return View(loginViewModel);
    }

    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }
}