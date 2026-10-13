using CPE_Review_Hub.Data;
using CPE_Review_Hub.Models;
using CPE_Review_Hub.Security;
using CPE_Review_Hub.Services;
using CPE_Review_Hub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CPE_Review_Hub.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Policy = PortalSecurity.Admin)]
public class StudentsController(UserManager<ApplicationUser> users, ApplicationDbContext db,
    StudentProgressService progressService, IMemoryCache cache) : Controller
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
        var previousStamp = user.SecurityStamp;
        user.IsActive = active;
        // Rotate on disable AND re-enable so an older cookie cannot become valid again.
        user.SecurityStamp = Guid.NewGuid().ToString();
        var result = await users.UpdateAsync(user);
        if (result.Succeeded && !string.IsNullOrWhiteSpace(previousStamp))
            cache.Remove(PortalSecurity.PrincipalCacheKey(PortalSecurity.Student, user.Id, previousStamp));
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? "Student account status updated." : "Could not update this account. Try again.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Progress(string id)
    {
        var student = await users.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.AccountType == PortalSecurity.Student);
        if (student is null) return NotFound();
        var subjects = await db.Subjects.AsNoTracking().Where(x => x.IsPublished)
            .OrderBy(x => x.AcademicLevelId).ThenBy(x => x.Code).Select(x => new { x.Id, x.Code, x.Title }).ToListAsync();
        var rows = new List<AdminStudentCourseProgressRow>();
        foreach (var subject in subjects)
        {
            var state = await progressService.GetCourseAsync(subject.Id, id);
            if (state is null) continue;
            var titles = await db.Lessons.AsNoTracking().Where(x => x.SubjectId == subject.Id && x.IsPublished)
                .ToDictionaryAsync(x => x.Id, x => x.Title);
            rows.Add(new AdminStudentCourseProgressRow(subject.Id, subject.Code, subject.Title,
                state.CompletionPercentage, state.FinalExamUnlocked, state.Lessons.Select((x, index) =>
                    new AdminStudentLessonProgressRow(index + 1, titles[x.LessonId], x.ActivityCompleted, x.QuizPassed,
                        x.IsCompleted, x.IsUnlocked, x.BestQuizPercentage, x.ActivitySubmissionId)).ToList()));
        }
        return View(new AdminStudentProgressViewModel(student.Id, student.FullName, student.Email ?? "", rows));
    }
}

public record StudentRow(string Id, string FullName, string Email, bool IsActive, DateTime CreatedAtUtc);
