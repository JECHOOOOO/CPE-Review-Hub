using CPE_Review_Hub.Security;
using CPE_Review_Hub.Services;
using CPE_Review_Hub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CPE_Review_Hub.Areas.Student.Controllers;

[Area("Student")]
public class AccountController : Controller
{
    [AllowAnonymous, HttpGet("/student.html")]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [AllowAnonymous, HttpPost("/student.html"), EnableRateLimiting("account")]
    public async Task<IActionResult> Login(LoginViewModel model, [FromServices] PortalAccountService accounts)
    {
        if (!ModelState.IsValid) return View(model);
        if (!await accounts.LoginAsync(HttpContext, model, PortalSecurity.Student))
        {
            ModelState.AddModelError("", "Unable to sign in. Check your student credentials or try again later.");
            return View(model);
        }
        if (Url.IsLocalUrl(model.ReturnUrl) && model.ReturnUrl!.StartsWith("/Student/", StringComparison.OrdinalIgnoreCase))
            return LocalRedirect(model.ReturnUrl);
        return RedirectToAction("Index", "Dashboard", new { area = "Student" });
    }

    [AllowAnonymous, HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [AllowAnonymous, HttpPost, EnableRateLimiting("account")]
    public async Task<IActionResult> Register(RegisterViewModel model, [FromServices] PortalAccountService accounts)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await accounts.RegisterStudentAsync(model);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Code.StartsWith("Duplicate")
                    ? "Unable to register with these details. Try signing in or use another email." : error.Description);
            return View(model);
        }
        TempData["Success"] = "Student account created. You can now sign in.";
        return Redirect("/student.html");
    }

    [Authorize(Policy = PortalSecurity.Student), HttpPost]
    public async Task<IActionResult> Logout([FromServices] PortalAccountService accounts)
    {
        await accounts.LogoutAsync(HttpContext);
        return Redirect("/student.html");
    }
}
