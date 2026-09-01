using CPE_Review_Hub.Models;
using CPE_Review_Hub.Security;
using CPE_Review_Hub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CPE_Review_Hub.Areas.Student.Controllers;

[Area("Student"), Authorize(Policy = PortalSecurity.Student)]
public class ProfileController(UserManager<ApplicationUser> users) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return Challenge(PortalSecurity.Student);
        return View(new ProfileViewModel { FullName = user.FullName, University = user.University, YearLevel = user.YearLevel });
    }

    [HttpPost]
    public async Task<IActionResult> Index(ProfileViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await users.GetUserAsync(User);
        if (user is null) return Challenge(PortalSecurity.Student);
        // No user ID, role, email or active status is accepted from the form.
        user.FullName = model.FullName.Trim();
        user.University = model.University?.Trim() ?? "";
        user.YearLevel = model.YearLevel?.Trim() ?? "";
        var result = await users.UpdateAsync(user);
        if (!result.Succeeded)
        {
            ModelState.AddModelError("", "Could not save your profile. Please try again.");
            return View(model);
        }
        TempData["Success"] = "Your profile has been updated.";
        return RedirectToAction(nameof(Index));
    }
}
