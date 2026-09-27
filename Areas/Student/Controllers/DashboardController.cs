using CPE_Review_Hub.Data;
using CPE_Review_Hub.Models;
using CPE_Review_Hub.Security;
using CPE_Review_Hub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CPE_Review_Hub.Areas.Student.Controllers;

[Area("Student"), Authorize(Policy = PortalSecurity.Student)]
public class DashboardController(UserManager<ApplicationUser> users, ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return Challenge(PortalSecurity.Student);
        var levels = await db.AcademicLevels.AsNoTracking().OrderBy(x => x.Id)
            .Select(x => new StudentLevelCard(x.Id, $"{x.Name} — {x.Id} Level",
                x.Subjects.Count(s => s.IsPublished))).ToListAsync();
        return View(new StudentDashboardViewModel(user.FullName, user.Email ?? "", levels));
    }
}
