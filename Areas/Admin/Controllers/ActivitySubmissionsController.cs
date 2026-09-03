using CPE_Review_Hub.Data;
using CPE_Review_Hub.Security;
using CPE_Review_Hub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CPE_Review_Hub.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Policy = PortalSecurity.Admin), Route("admin/activity-submissions")]
public class ActivitySubmissionsController(ApplicationDbContext db) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(string? search, string status = "pending", int page = 1)
    {
        page = Math.Clamp(page, 1, 100000);
        status = status is "pending" or "graded" ? status : "all";
        search = search?.Trim();
        if (search?.Length > 120) search = search[..120];
        var query = db.ActivitySubmissions.AsNoTracking();
        if (status == "pending") query = query.Where(x => x.Grade == null);
        if (status == "graded") query = query.Where(x => x.Grade != null);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x => x.User.FullName.Contains(search) || x.User.Email!.Contains(search) ||
                x.LessonActivity.Lesson.Subject.Code.Contains(search));
        var rows = await query.OrderByDescending(x => x.UpdatedAtUtc).ThenByDescending(x => x.Id)
            .Skip((page - 1) * 30).Take(31)
            .Select(x => new ActivitySubmissionAdminRow(x.Id, x.User.FullName, x.User.Email ?? "",
                x.LessonActivity.Lesson.Subject.Code, x.LessonActivity.Lesson.Title,
                x.UpdatedAtUtc, x.Attachments.Count, x.Grade)).ToListAsync();
        return View(new ActivitySubmissionIndexViewModel(search, status, page, rows.Count > 30, rows.Take(30).ToList()));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var model = await LoadDetailsAsync(id);
        return model is null ? NotFound() : View(model);
    }

    [HttpPost("{id:int}/grade")]
    public async Task<IActionResult> Grade(int id, [Bind(Prefix = "Grade")] ActivityGradeViewModel model)
    {
        var submission = await db.ActivitySubmissions.SingleOrDefaultAsync(x => x.Id == id);
        if (submission is null) return NotFound();
        if (ModelState.IsValid && submission.Revision != model.Revision)
            ModelState.AddModelError("", "This submission changed since you opened it. Reload and review the latest answer before grading.");
        if (ModelState.IsValid)
        {
            submission.Grade = decimal.Round(model.Score!.Value, 2);
            submission.Feedback = model.Feedback?.Trim() ?? "";
            submission.GradedAtUtc = DateTime.UtcNow;
            submission.GradedById = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            submission.Revision++;
            try
            {
                await db.SaveChangesAsync();
                TempData["Success"] = "Grade and feedback saved. The student can now see them on the lesson page.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError("", "This submission changed during grading. Reload and review the latest answer before saving.");
            }
        }
        var details = await LoadDetailsAsync(id);
        return details is null ? NotFound() : View("Details", details with { Grade = model });
    }

    private Task<AdminActivitySubmissionViewModel?> LoadDetailsAsync(int id) =>
        db.ActivitySubmissions.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new AdminActivitySubmissionViewModel(x.Id, x.User.FullName, x.User.Email ?? "",
                x.LessonActivity.Lesson.Subject.Code, x.LessonActivity.Lesson.Title, x.LessonActivity.Title,
                x.Response, x.CompletedAtUtc, x.UpdatedAtUtc, x.Attachments.OrderBy(a => a.UploadedAtUtc)
                    .Select(a => new AttachmentViewModel(a.Id, a.FileName, a.ContentType, a.FileSize)).ToList(),
                x.LessonActivity.Instructions, x.LessonActivity.ExpectedOutput, x.GradedAtUtc,
                x.GradedBy == null ? null : x.GradedBy.FullName,
                new ActivityGradeViewModel { Score = x.Grade, Feedback = x.Feedback, Revision = x.Revision }))
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
