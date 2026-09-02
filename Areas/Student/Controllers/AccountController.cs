using CPE_Review_Hub.Models;
using CPE_Review_Hub.Security;
using CPE_Review_Hub.Services;
using CPE_Review_Hub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.WebUtilities;
using System.Net;
using System.Text;

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
            ModelState.AddModelError("", "Unable to sign in. Check your student credentials, verify your email, or try again later.");
            return View(model);
        }
        if (Url.IsLocalUrl(model.ReturnUrl) && model.ReturnUrl!.StartsWith("/Student/", StringComparison.OrdinalIgnoreCase))
            return LocalRedirect(model.ReturnUrl);
        return RedirectToAction("Index", "Dashboard", new { area = "Student" });
    }

    [AllowAnonymous, HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [AllowAnonymous, HttpPost, EnableRateLimiting("account")]
    public async Task<IActionResult> Register(RegisterViewModel model, [FromServices] PortalAccountService accounts,
        [FromServices] IEmailDeliveryService email, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);
        if (!email.IsConfigured)
        {
            ModelState.AddModelError("", "Email verification is temporarily unavailable. Ask the administrator to configure email delivery.");
            return View(model);
        }
        var (result, user) = await accounts.RegisterStudentAsync(model);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Code.StartsWith("Duplicate")
                    ? "Unable to register with these details. Try signing in or use another email." : error.Description);
            return View(model);
        }
        try { await SendConfirmationAsync(user!, accounts, email, cancellationToken); }
        catch (InvalidOperationException)
        {
            await accounts.DeleteAsync(user!);
            ModelState.AddModelError("", "We could not send the verification email, so the account was not created. Try again later.");
            return View(model);
        }
        TempData["Success"] = "Account created. Open the verification link sent to your email before signing in.";
        return Redirect("/student.html");
    }

    [AllowAnonymous, HttpGet("/student/confirm-email")]
    public async Task<IActionResult> ConfirmEmail(string? userId, string? code,
        [FromServices] PortalAccountService accounts)
    {
        var success = false;
        if (!string.IsNullOrWhiteSpace(userId) && !string.IsNullOrWhiteSpace(code))
        {
            try
            {
                var token = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
                success = (await accounts.ConfirmEmailAsync(userId, token)).Succeeded;
            }
            catch (FormatException) { }
        }
        return View("ConfirmEmail", success);
    }

    [AllowAnonymous, HttpGet("/student/resend-confirmation")]
    public IActionResult ResendConfirmation() => View(new ResendConfirmationViewModel());

    [AllowAnonymous, HttpPost("/student/resend-confirmation"), EnableRateLimiting("account")]
    public async Task<IActionResult> ResendConfirmation(ResendConfirmationViewModel model,
        [FromServices] PortalAccountService accounts, [FromServices] IEmailDeliveryService email,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) return View(model);
        var user = email.IsConfigured ? await accounts.FindUnconfirmedStudentAsync(model.Email) : null;
        if (user is not null)
        {
            try { await SendConfirmationAsync(user, accounts, email, cancellationToken); }
            catch (InvalidOperationException) { }
        }
        TempData["Success"] = "If an unconfirmed student account exists for that address, a new verification email has been sent.";
        return Redirect("/student.html");
    }

    private async Task SendConfirmationAsync(ApplicationUser user, PortalAccountService accounts,
        IEmailDeliveryService email, CancellationToken cancellationToken)
    {
        var token = await accounts.GenerateEmailConfirmationTokenAsync(user);
        var code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
        var link = Url.Action(nameof(ConfirmEmail), "Account", new { area = "Student", userId = user.Id, code },
            Request.Scheme, Request.Host.Value) ?? throw new InvalidOperationException("Could not create confirmation URL.");
        var safeName = WebUtility.HtmlEncode(user.FullName);
        var safeLink = WebUtility.HtmlEncode(link);
        await email.SendAsync(user.Email!, "Verify your CPE Review Hub account", $"""
            <!doctype html><html><body style="font-family:Arial,sans-serif;color:#15253c">
            <h1>Welcome to CPE Review Hub</h1><p>Hello {safeName},</p>
            <p>Confirm your student email address to activate your account.</p>
            <p><a href="{safeLink}" style="display:inline-block;padding:12px 20px;background:#185244;color:#fff;text-decoration:none;border-radius:8px">Verify email address</a></p>
            <p>If you did not create this account, you can ignore this message.</p></body></html>
            """, cancellationToken);
    }

    [Authorize(Policy = PortalSecurity.Student), HttpPost]
    public async Task<IActionResult> Logout([FromServices] PortalAccountService accounts)
    {
        await accounts.LogoutAsync(HttpContext);
        return Redirect("/student.html");
    }
}
