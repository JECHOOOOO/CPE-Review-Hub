using CPE_Review_Hub.Models;
using CPE_Review_Hub.Data;
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
    [AllowAnonymous, HttpGet("/student.html"), HttpGet("/student/login"),
     ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.IsInRole(PortalSecurity.Student)) return RedirectToAction("Index", "Dashboard", new { area = "Student" });
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [AllowAnonymous, HttpPost("/student.html"), HttpPost("/student/login"), EnableRateLimiting("account")]
    public async Task<IActionResult> Login(LoginViewModel model, [FromServices] PortalAccountService accounts)
    {
        if (User.IsInRole(PortalSecurity.Student)) return RedirectToAction("Index", "Dashboard", new { area = "Student" });
        if (!ModelState.IsValid) return View(model);
        if (!await accounts.LoginAsync(HttpContext, model, PortalSecurity.Student))
        {
            ModelState.AddModelError("", "Unable to sign in. Check your student email and password, or try again later.");
            return View(model);
        }
        if (Url.IsLocalUrl(model.ReturnUrl) && model.ReturnUrl!.StartsWith("/Student/", StringComparison.OrdinalIgnoreCase))
            return LocalRedirect(model.ReturnUrl);
        return RedirectToAction("Index", "Dashboard", new { area = "Student" });
    }

    [AllowAnonymous, HttpGet("/student/register"), HttpGet("/Student/Account/Register"),
     ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Register()
    {
        if (User.IsInRole(PortalSecurity.Student)) return RedirectToAction("Index", "Dashboard", new { area = "Student" });
        return View(new RegisterViewModel());
    }

    [AllowAnonymous, HttpGet("/student/forgot-password")]
    public IActionResult ForgotPassword()
    {
        ViewData["PortalName"] = "student";
        ViewData["FormAction"] = "/student/forgot-password";
        return View("~/Views/Shared/ForgotPassword.cshtml", new PasswordResetRequestViewModel());
    }

    [AllowAnonymous, HttpPost("/student/forgot-password"), EnableRateLimiting("account")]
    public async Task<IActionResult> ForgotPassword(PasswordResetRequestViewModel model,
        [FromServices] PortalPasswordResetService reset, CancellationToken cancellationToken)
    {
        ViewData["PortalName"] = "student";
        ViewData["FormAction"] = "/student/forgot-password";
        if (!ModelState.IsValid) return View("~/Views/Shared/ForgotPassword.cshtml", model);
        await reset.SendIfEligibleAsync(model.Email, PortalSecurity.Student,
            $"{Request.Scheme}://{Request.Host.Value}", cancellationToken);
        TempData["PasswordResetNotice"] = "If an account exists for this email address, password reset instructions have been sent.";
        return RedirectToAction(nameof(ForgotPassword));
    }

    [AllowAnonymous, HttpGet("/student/reset-password")]
    public IActionResult ResetPassword(string? email, string? token)
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        ViewData["PortalName"] = "student";
        ViewData["FormAction"] = "/student/reset-password";
        return View("~/Views/Shared/ResetPassword.cshtml", new PasswordResetViewModel { Email = email ?? "", Token = token ?? "" });
    }

    [AllowAnonymous, HttpPost("/student/reset-password"), EnableRateLimiting("account")]
    public async Task<IActionResult> ResetPassword(PasswordResetViewModel model,
        [FromServices] PortalPasswordResetService reset)
    {
        Response.Headers["Referrer-Policy"] = "no-referrer";
        ViewData["PortalName"] = "student";
        ViewData["FormAction"] = "/student/reset-password";
        if (!ModelState.IsValid) return View("~/Views/Shared/ResetPassword.cshtml", model);
        if (!await reset.ResetAsync(model.Email, model.Token, PortalSecurity.Student, model.Password))
        {
            ModelState.AddModelError("", "This password reset link is invalid or expired. Request a new one to continue.");
            return View("~/Views/Shared/ResetPassword.cshtml", model);
        }
        TempData["Success"] = "Your password was changed. Sign in with your new password.";
        return Redirect("/student.html");
    }

    [AllowAnonymous, HttpPost("/student/register"), HttpPost("/Student/Account/Register"), EnableRateLimiting("account")]
    public async Task<IActionResult> Register(RegisterViewModel model, [FromServices] PortalAccountService accounts,
        [FromServices] IEmailDeliveryService email, [FromServices] IConfiguration configuration,
        [FromServices] ApplicationDbContext db, CancellationToken cancellationToken)
    {
        if (User.IsInRole(PortalSecurity.Student)) return RedirectToAction("Index", "Dashboard", new { area = "Student" });
        if (!ModelState.IsValid) return View(model);
        if (!email.IsConfigured)
        {
            ModelState.AddModelError("", "Account email delivery is temporarily unavailable. Ask the administrator to check Gmail settings.");
            return View(model);
        }
        // Dispose rolls back even on cancellation or an unexpected email exception.
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var (result, user) = await accounts.RegisterStudentAsync(model);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(error => error.Code.StartsWith("Duplicate", StringComparison.OrdinalIgnoreCase)))
                ModelState.AddModelError("", "An account already uses this email. Try signing in or use another email.");
            else
                foreach (var error in result.Errors.DistinctBy(error => error.Description))
                    ModelState.AddModelError("", error.Description);
            return View(model);
        }
        try { await SendWelcomeEmailAsync(user!, email, configuration, cancellationToken); }
        catch (Exception exception) when (exception is InvalidOperationException or AggregateException or OperationCanceledException or System.IO.IOException or UnauthorizedAccessException)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            ModelState.AddModelError("", "We could not send the account email, so the account was not created. Try again later.");
            return View(model);
        }
        await transaction.CommitAsync(cancellationToken);
        TempData["Success"] = "Account created. A welcome message was sent to your email.";
        return Redirect("/student.html");
    }

    private async Task SendWelcomeEmailAsync(ApplicationUser user, IEmailDeliveryService email,
        IConfiguration configuration, CancellationToken cancellationToken)
    {
        var configuredBaseUrl = configuration["Application:PublicBaseUrl"]?.TrimEnd('/');
        var signInUrl = Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var publicBaseUri) &&
                        publicBaseUri.Scheme is "http" or "https"
            ? new Uri(publicBaseUri, "/student.html").ToString()
            : Url.Action(nameof(Login), "Account", new { area = "Student" }, Request.Scheme, Request.Host.Value)
              ?? throw new InvalidOperationException("The student sign-in URL could not be created.");

        var html = StudentWelcomeEmailTemplate.Build(user.FullName, user.Email!, signInUrl);
        await email.SendAsync(user.Email!, StudentWelcomeEmailTemplate.Subject, html, cancellationToken);
    }

    [Authorize(Policy = PortalSecurity.Student), HttpPost]
    public async Task<IActionResult> Logout([FromServices] PortalAccountService accounts)
    {
        await accounts.LogoutAsync(HttpContext, PortalSecurity.Student);
        return Redirect("/student.html");
    }
}
