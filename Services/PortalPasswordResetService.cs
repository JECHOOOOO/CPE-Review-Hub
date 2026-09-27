using System.Text.Encodings.Web;
using CPE_Review_Hub.Models;
using CPE_Review_Hub.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;

namespace CPE_Review_Hub.Services;

public sealed class PortalPasswordResetService(UserManager<ApplicationUser> users,
    IEmailDeliveryService email, IConfiguration configuration, IWebHostEnvironment environment,
    ILogger<PortalPasswordResetService> logger)
{
    public async Task SendIfEligibleAsync(string? address, string portal, string requestOrigin,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(address) || !email.IsConfigured) return;
        var user = await users.FindByEmailAsync(address.Trim());
        if (user is null || !user.IsActive || user.AccountType != portal || string.IsNullOrWhiteSpace(user.Email))
            return;

        var token = await users.GeneratePasswordResetTokenAsync(user);
        var configuredBase = configuration["Application:PublicBaseUrl"]?.TrimEnd('/');
        var originCandidate = string.IsNullOrWhiteSpace(configuredBase) ? requestOrigin : configuredBase;
        if (!Uri.TryCreate(originCandidate, UriKind.Absolute, out var publicUri) ||
            (publicUri.Scheme != Uri.UriSchemeHttps &&
             !(environment.IsDevelopment() && publicUri.Scheme == Uri.UriSchemeHttp &&
               (publicUri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
                publicUri.Host.Equals("127.0.0.1", StringComparison.Ordinal)))) ||
            !string.IsNullOrEmpty(publicUri.UserInfo))
        {
            logger.LogWarning("Password reset email was not sent because a safe public COLMS URL is unavailable.");
            return;
        }
        var origin = publicUri.GetLeftPart(UriPartial.Authority);
        var route = portal == PortalSecurity.Admin ? "/admin/reset-password" : "/student/reset-password";
        var resetUrl = QueryHelpers.AddQueryString(origin + route, new Dictionary<string, string?>
        {
            ["email"] = user.Email,
            ["token"] = token
        });

        var safeName = HtmlEncoder.Default.Encode(user.FullName);
        var safeUrl = HtmlEncoder.Default.Encode(resetUrl);
        var html = $"""
            <!doctype html><html><body style="margin:0;background:#f2f6fa;font-family:Arial,sans-serif;color:#172b45">
            <div style="max-width:560px;margin:32px auto;background:#fff;border-radius:16px;overflow:hidden">
            <header style="padding:24px;background:#10243b;color:#fff"><strong style="font-size:22px">COLMS</strong><div style="color:#a9c9dc;font-size:13px">CentOs Learning Management System</div></header>
            <main style="padding:28px"><h1 style="font-size:22px">Reset your password</h1><p>Hello {safeName},</p>
            <p>Use the button below to choose a new password. This link expires after one hour and can be used once.</p>
            <p style="margin:28px 0"><a href="{safeUrl}" style="background:#087d8a;color:#fff;text-decoration:none;padding:13px 20px;border-radius:8px;font-weight:bold">RESET PASSWORD</a></p>
            <p style="font-size:13px;color:#53677e">If you did not request a password reset, you can ignore this email.</p></main>
            <footer style="padding:18px 28px;background:#f7f9fb;color:#62748a;font-size:12px">Automated COLMS account email.</footer>
            </div></body></html>
            """;
        try
        {
            await email.SendAsync(user.Email, "Reset your COLMS password", html, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Keep the response identical for existing and missing accounts.
            logger.LogWarning("Password reset email delivery failed ({ErrorType}).", exception.GetType().Name);
        }
    }

    public async Task<bool> ResetAsync(string emailAddress, string token, string portal, string password)
    {
        var user = await users.FindByEmailAsync(emailAddress.Trim());
        if (user is null || !user.IsActive || user.AccountType != portal) return false;
        var result = await users.ResetPasswordAsync(user, token, password);
        return result.Succeeded;
    }
}
