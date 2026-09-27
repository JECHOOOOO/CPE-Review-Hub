using CPE_Review_Hub.Security;
using CPE_Review_Hub.Services;
using CPE_Review_Hub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using CPE_Review_Hub.Models;

namespace CPE_Review_Hub.Areas.Admin.Controllers;

[Area("Admin")]
public class AccountController : Controller
{
    [AllowAnonymous, HttpGet("/admin.html"), HttpGet("/admin/login")]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [AllowAnonymous, HttpGet("/admin/forgot-password")]
    public IActionResult ForgotPassword()
    {
        ViewData["PortalName"] = "administrator";
        ViewData["FormAction"] = "/admin/forgot-password";
        return View("~/Views/Shared/ForgotPassword.cshtml", new PasswordResetRequestViewModel());
    }

    [AllowAnonymous, HttpPost("/admin/forgot-password"), EnableRateLimiting("account")]
    public async Task<IActionResult> ForgotPassword(PasswordResetRequestViewModel model,
        [FromServices] PortalPasswordResetService reset, CancellationToken cancellationToken)
    {
        ViewData["PortalName"] = "administrator";
        ViewData["FormAction"] = "/admin/forgot-password";
        if (!ModelState.IsValid) return View("~/Views/Shared/ForgotPassword.cshtml", model);
        await reset.SendIfEligibleAsync(model.Email, PortalSecurity.Admin,
            $"{Request.Scheme}://{Request.Host.Value}", cancellationToken);
        TempData["PasswordResetNotice"] = "If an account exists for this email address, password reset instructions have been sent.";
        return RedirectToAction(nameof(ForgotPassword));
    }

    [AllowAnonymous, HttpGet("/admin/reset-password")]
    public IActionResult ResetPassword(string? email, string? token)
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        ViewData["PortalName"] = "administrator";
        ViewData["FormAction"] = "/admin/reset-password";
        return View("~/Views/Shared/ResetPassword.cshtml", new PasswordResetViewModel { Email = email ?? "", Token = token ?? "" });
    }

    [AllowAnonymous, HttpPost("/admin/reset-password"), EnableRateLimiting("account")]
    public async Task<IActionResult> ResetPassword(PasswordResetViewModel model,
        [FromServices] PortalPasswordResetService reset)
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        ViewData["PortalName"] = "administrator";
        ViewData["FormAction"] = "/admin/reset-password";
        if (!ModelState.IsValid) return View("~/Views/Shared/ResetPassword.cshtml", model);
        if (!await reset.ResetAsync(model.Email, model.Token, PortalSecurity.Admin, model.Password))
        {
            ModelState.AddModelError("", "This password reset link is invalid or expired. Request a new one to continue.");
            return View("~/Views/Shared/ResetPassword.cshtml", model);
        }
        TempData["Success"] = "Your password was changed. Sign in with your new password.";
        return Redirect("/admin.html");
    }

    [AllowAnonymous, HttpPost("/admin.html"), HttpPost("/admin/login"), EnableRateLimiting("account")]
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
