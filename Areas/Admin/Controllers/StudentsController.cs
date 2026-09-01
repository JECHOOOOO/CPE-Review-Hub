using CPE_Review_Hub.Models;
using CPE_Review_Hub.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CPE_Review_Hub.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Policy = PortalSecurity.Admin)]
public class StudentsController(UserManager<ApplicationUser> users) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(string? search)
    {
        var query = users.Users.Where(u => u.AccountType == PortalSecurity.Student);
        if (!string.IsNullOrWhiteSpace(search))
        {
            search = search.Trim();
            query = query.Where(u => u.FullName.Contains(search) || u.Email!.Contains(search));
        }
        ViewData["Search"] = search;
        return View(await query.OrderByDescending(u => u.CreatedAtUtc).Take(100)
            .Select(u => new StudentRow(u.Id, u.FullName, u.Email ?? "", u.IsActive, u.CreatedAtUtc)).ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> SetActive(string id, bool active)
    {
        var user = await users.FindByIdAsync(id);
        if (user is null || user.AccountType != PortalSecurity.Student) return NotFound();
        user.IsActive = active;
        // Rotate on disable AND re-enable so an older cookie cannot become valid again.
        user.SecurityStamp = Guid.NewGuid().ToString();
        var result = await users.UpdateAsync(user);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? "Student account status updated." : "Could not update this account. Try again.";
        return RedirectToAction(nameof(Index));
    }
}

public record StudentRow(string Id, string FullName, string Email, bool IsActive, DateTime CreatedAtUtc);
