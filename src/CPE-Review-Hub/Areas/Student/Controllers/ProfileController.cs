using CPE_Review_Hub.Models;
using CPE_Review_Hub.Security;
using CPE_Review_Hub.Services;
using CPE_Review_Hub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace CPE_Review_Hub.Areas.Student.Controllers;

[Area("Student"), Authorize(Policy = PortalSecurity.Student)]
public class ProfileController(UserManager<ApplicationUser> users, StudentProfileService profiles) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return Challenge(PortalSecurity.Student);
        return View(await BuildPageAsync(user,
            new ProfileViewModel { FullName = user.FullName, University = user.University, YearLevel = user.YearLevel },
            cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> Index([Bind(Prefix = "EditProfile")] ProfileViewModel model)
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return Challenge(PortalSecurity.Student);
        if (!ModelState.IsValid) return View(await BuildPageAsync(user, model, HttpContext.RequestAborted));
        // No user ID, role, email or active status is accepted from the form.
        user.FullName = model.FullName.Trim();
        user.University = model.University?.Trim() ?? "";
        user.YearLevel = model.YearLevel?.Trim() ?? "";
        var result = await users.UpdateAsync(user);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", "Could not save your profile. Please try again.");
            return View(await BuildPageAsync(user, model, HttpContext.RequestAborted));
        }
        TempData["Success"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("/student/change-password")]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost("/student/change-password"), EnableRateLimiting("account")]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await users.GetUserAsync(User);
        if (user is null) return Challenge(PortalSecurity.Student);

        var result = await users.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            if (result.Errors.Any(x => x.Code == "PasswordMismatch"))
                ModelState.AddModelError(nameof(model.CurrentPassword), "The current password is incorrect.");
            else
                foreach (var error in result.Errors)
                    ModelState.AddModelError(string.Empty, error.Description);
            return View(model);
        }

        await users.UpdateSecurityStampAsync(user);
        await HttpContext.SignOutAsync(PortalSecurity.Student);
        TempData["Success"] = "Your password has been changed. Sign in with your new password.";
        return Redirect("/student.html");
    }

    private async Task<StudentProfilePageViewModel> BuildPageAsync(ApplicationUser user,
        ProfileViewModel editModel, CancellationToken cancellationToken)
    {
        var nameParts = user.FullName.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var initials = nameParts.Length > 1
            ? $"{char.ToUpperInvariant(nameParts[0][0])}{char.ToUpperInvariant(nameParts[^1][0])}"
            : nameParts.FirstOrDefault() is { Length: > 0 } name
                ? name[..Math.Min(2, name.Length)].ToUpperInvariant() : "ST";
        var learning = await profiles.GetLearningAsync(user.Id, cancellationToken);
        return new StudentProfilePageViewModel(user.FullName, user.Email ?? "", user.University,
            user.YearLevel, user.IsActive, user.CreatedAtUtc, initials, editModel, learning);
    }
}
