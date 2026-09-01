using CPE_Review_Hub.Security;
using CPE_Review_Hub.Services;
using CPE_Review_Hub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CPE_Review_Hub.Areas.Admin.Controllers;

[Area("Admin")]
public class AccountController : Controller
{
    [AllowAnonymous, HttpGet("/admin.html")]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [AllowAnonymous, HttpPost("/admin.html"), EnableRateLimiting("account")]
    public async Task<IActionResult> Login(LoginViewModel model, [FromServices] PortalAccountService accounts)
    {
        if (!ModelState.IsValid) return View(model);
        if (!await accounts.LoginAsync(HttpContext, model, PortalSecurity.Admin))
        {
            ModelState.AddModelError("", "Unable to sign in. Check your administrator credentials or try again later.");
            return View(model);
        }
        if (Url.IsLocalUrl(model.ReturnUrl) && model.ReturnUrl!.StartsWith("/Admin/", StringComparison.OrdinalIgnoreCase))
            return LocalRedirect(model.ReturnUrl);
        return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
    }

    [Authorize(Policy = PortalSecurity.Admin), HttpPost]
    public async Task<IActionResult> Logout([FromServices] PortalAccountService accounts)
    {
        await accounts.LogoutAsync(HttpContext);
        return Redirect("/admin.html");
    }
}
