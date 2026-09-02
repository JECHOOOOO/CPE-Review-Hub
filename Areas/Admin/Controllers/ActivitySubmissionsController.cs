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
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var model = await db.ActivitySubmissions.AsNoTracking().Where(x => x.Id == id)
            .Select(x => new AdminActivitySubmissionViewModel(x.Id, x.User.FullName, x.User.Email ?? "",
                x.LessonActivity.Lesson.Subject.Code, x.LessonActivity.Lesson.Title, x.LessonActivity.Title,
                x.Response, x.CompletedAtUtc, x.UpdatedAtUtc, x.Attachments.OrderBy(a => a.UploadedAtUtc)
                    .Select(a => new AttachmentViewModel(a.Id, a.FileName, a.ContentType, a.FileSize)).ToList()))
            .SingleOrDefaultAsync();
        return model is null ? NotFound() : View(model);
    }

    [HttpGet("attachments/{id:int}")]
    public async Task<IActionResult> Download(int id)
    {
        var file = await db.SubmissionAttachments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        return file is null ? NotFound() : File(file.Data, file.ContentType, file.FileName);
    }
}
