using CPE_Review_Hub.Data;
using CPE_Review_Hub.Models;
using CPE_Review_Hub.Security;
using CPE_Review_Hub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CPE_Review_Hub.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Policy = PortalSecurity.Admin)]
public class DashboardController(UserManager<ApplicationUser> users, ApplicationDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var user = await users.GetUserAsync(User);
        if (user is null) return Challenge(PortalSecurity.Admin);
        var students = await users.Users.CountAsync(u => u.AccountType == PortalSecurity.Student);
        return View(new AdminDashboardViewModel(user.FullName, user.Email ?? "", students,
            await db.Subjects.CountAsync(), await db.Lessons.CountAsync(), await db.ReviewMaterials.CountAsync()));
    }
}
