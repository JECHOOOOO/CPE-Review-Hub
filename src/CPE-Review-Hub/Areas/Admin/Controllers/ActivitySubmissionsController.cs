using CPE_Review_Hub.Data;
using CPE_Review_Hub.Security;
using CPE_Review_Hub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CPE_Review_Hub.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Policy = PortalSecurity.Admin), Route("admin/activity-submissions")]
public class ActivitySubmissionsController(ApplicationDbContext db) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? student, int? courseId, int? lessonId,
        DateTime? fromDate, DateTime? toDate, int page = 1)
    {
        page = Math.Clamp(page, 1, 100000);
        student = student?.Trim();
        if (student?.Length > 120) student = student[..120];
        if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
            ModelState.AddModelError("", "The From date must be before or equal to the To date.");
        var query = db.ActivitySubmissions.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(student))
            query = query.Where(x => x.User.FullName.Contains(student) || x.User.Email!.Contains(student));
        if (courseId.HasValue) query = query.Where(x => x.LessonActivity.Lesson.SubjectId == courseId);
        if (lessonId.HasValue) query = query.Where(x => x.LessonActivity.LessonId == lessonId);
        if (fromDate.HasValue) query = query.Where(x => x.CompletedAtUtc >= fromDate.Value.Date);
        if (toDate.HasValue) query = query.Where(x => x.CompletedAtUtc < toDate.Value.Date.AddDays(1));
        var rows = await query.OrderByDescending(x => x.CompletedAtUtc).ThenByDescending(x => x.Id)
            .Skip((page - 1) * 30).Take(31)
            .Select(x => new ActivitySubmissionAdminRow(x.Id, x.User.FullName, x.User.Email ?? "",
                x.LessonActivity.Lesson.Subject.Code, x.LessonActivity.Lesson.Title,
                x.LessonActivity.Title, x.CompletedAtUtc, x.Attachments.Count)).ToListAsync();
        var courses = await db.Subjects.AsNoTracking().OrderBy(x => x.Code)
            .Select(x => new ActivityFilterOption(x.Id, x.Code + " — " + x.Title)).ToListAsync();
        var lessons = await db.Lessons.AsNoTracking()
            .Where(x => !courseId.HasValue || x.SubjectId == courseId)
            .OrderBy(x => x.Subject.Code).ThenBy(x => x.SortOrder)
            .Select(x => new ActivityFilterOption(x.Id, x.Subject.Code + " — " + x.Title)).ToListAsync();
        return View(new ActivitySubmissionIndexViewModel(student, courseId, lessonId, fromDate, toDate,
            page, rows.Count > 30, courses, lessons, rows.Take(30).ToList()));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var model = await LoadDetailsAsync(id);
        return model is null ? NotFound() : View(model);
    }

    private Task<AdminActivitySubmissionViewModel?> LoadDetailsAsync(int id) =>
        db.ActivitySubmissions.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new AdminActivitySubmissionViewModel(x.Id, x.User.FullName, x.User.Email ?? "",
                x.LessonActivity.Lesson.Subject.Code, x.LessonActivity.Lesson.Title, x.LessonActivity.Title,
                x.Response, x.CompletedAtUtc, x.UpdatedAtUtc, x.Attachments.OrderBy(a => a.UploadedAtUtc)
                    .Select(a => new AttachmentViewModel(a.Id, a.FileName, a.ContentType, a.FileSize)).ToList(),
                x.LessonActivity.Instructions, x.LessonActivity.ExpectedOutput))
            .SingleOrDefaultAsync();

    [HttpGet("attachments/{id:int}/preview")]
    public async Task<IActionResult> Preview(int id)
    {
        var file = await db.SubmissionAttachments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (file is null || file.ContentType is not ("image/png" or "image/jpeg" or "image/webp")) return NotFound();
        return File(file.Data, file.ContentType);
    }

    [HttpGet("attachments/{id:int}")]
    public async Task<IActionResult> Download(int id)
    {
        var file = await db.SubmissionAttachments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        return file is null ? NotFound() : File(file.Data, file.ContentType, file.FileName);
    }
}
