using System.Security.Claims;
using CPE_Review_Hub.Data;
using CPE_Review_Hub.Models;
using CPE_Review_Hub.Security;
using CPE_Review_Hub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CPE_Review_Hub.Areas.Student.Controllers;

[Area("Student"), Authorize(Policy = PortalSecurity.Student), Route("student/activities")]
public class ActivitiesController(ApplicationDbContext db) : Controller
{
    [HttpPost("{id:int}/submit")]
    public async Task<IActionResult> Submit(int id, ActivitySubmissionViewModel model)
    {
        var activity = await db.LessonActivities.Include(x => x.Lesson).ThenInclude(x => x.Subject)
            .SingleOrDefaultAsync(x => x.Id == id && x.IsPublished && x.Lesson.IsPublished && x.Lesson.Subject.IsPublished);
        if (activity is null) return NotFound();
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Write at least 50 characters and complete the required activity before taking the quiz.";
            return RedirectToAction("Details", "Lessons", new { area = "Student", id = activity.LessonId });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var submission = await db.ActivitySubmissions.SingleOrDefaultAsync(x =>
            x.UserId == userId && x.LessonActivityId == id);
        if (submission is null)
        {
            submission = new ActivitySubmission
            {
                UserId = userId, LessonActivityId = id, CompletedAtUtc = DateTime.UtcNow
            };
            db.ActivitySubmissions.Add(submission);
        }
        submission.Response = model.Response.Trim();
        submission.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        TempData["Success"] = "Activity saved. The 10-item lesson quiz is now unlocked.";
        return RedirectToAction("Details", "Lessons", new { area = "Student", id = activity.LessonId }, "quiz");
    }
}
