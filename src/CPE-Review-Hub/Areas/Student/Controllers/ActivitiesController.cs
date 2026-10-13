using System.Security.Claims;
using CPE_Review_Hub.Data;
using CPE_Review_Hub.Models;
using CPE_Review_Hub.Security;
using CPE_Review_Hub.Services;
using CPE_Review_Hub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CPE_Review_Hub.Areas.Student.Controllers;

[Area("Student"), Authorize(Policy = PortalSecurity.Student), Route("student/activities")]
public class ActivitiesController(ApplicationDbContext db, StudentProgressService progressService,
    ActivityAttachmentService attachmentService) : Controller
{
    [HttpPost("{id:int}/submit"), RequestSizeLimit(ActivityAttachmentService.MaximumTotalSize + 1024 * 1024)]
    public async Task<IActionResult> Submit(int id, ActivitySubmissionViewModel model, CancellationToken cancellationToken)
    {
        var activity = await db.LessonActivities.Include(x => x.Lesson).ThenInclude(x => x.Subject)
            .SingleOrDefaultAsync(x => x.Id == id && x.IsPublished && x.Lesson.IsPublished && x.Lesson.Subject.IsPublished);
        if (activity is null) return NotFound();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await progressService.IsLessonUnlockedAsync(activity.LessonId, userId))
        {
            TempData["Error"] = "Complete and pass the previous lesson before submitting this activity.";
            return RedirectToAction("Details", "Subjects", new { area = "Student", id = activity.Lesson.SubjectId });
        }
        var response = BuildResponse(activity, model);
        if (!ModelState.IsValid || (string.IsNullOrWhiteSpace(response) && model.Attachments.Count == 0))
        {
            TempData["Error"] = "Complete the activity response or attach the requested work before taking the quiz.";
            return RedirectToAction("Details", "Lessons", new { area = "Student", id = activity.LessonId });
        }
        var submission = await db.ActivitySubmissions.Include(x => x.Attachments).SingleOrDefaultAsync(x =>
            x.UserId == userId && x.LessonActivityId == id);
        var (attachments, attachmentError) = await attachmentService.ReadAsync(model.Attachments, cancellationToken);
        if (attachmentError is not null)
        {
            TempData["Error"] = attachmentError;
            return RedirectToAction("Details", "Lessons", new { area = "Student", id = activity.LessonId });
        }
        if ((submission?.Attachments.Count ?? 0) + attachments.Count > ActivityAttachmentService.MaximumFiles ||
            (submission?.Attachments.Sum(x => x.FileSize) ?? 0) + attachments.Sum(x => x.FileSize) > ActivityAttachmentService.MaximumTotalSize)
        {
            TempData["Error"] = "Keep no more than 3 attachments totaling 10 MB. Remove an existing file before adding another.";
            return RedirectToAction("Details", "Lessons", new { area = "Student", id = activity.LessonId });
        }
        if (submission is null)
        {
            submission = new ActivitySubmission
            {
                UserId = userId, LessonActivityId = id, CompletedAtUtc = DateTime.UtcNow
            };
            db.ActivitySubmissions.Add(submission);
        }
        submission.Response = response;
        submission.UpdatedAtUtc = DateTime.UtcNow;
        foreach (var attachment in attachments) submission.Attachments.Add(attachment);
        await db.SaveChangesAsync();
        TempData["Success"] = "Your response has been saved. The lesson quiz is now available.";
        return RedirectToAction("Details", "Lessons", new { area = "Student", id = activity.LessonId }, "quiz");
    }

    private static string BuildResponse(LessonActivity activity, ActivitySubmissionViewModel model)
    {
        if (activity.ResponseType == ActivityResponseType.Matching)
        {
            var pairs = activity.Options.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Split('|', 2, StringSplitOptions.TrimEntries)).Where(x => x.Length == 2).ToList();
            if (pairs.Count == 0 || model.MatchingResponses.Count != pairs.Count ||
                model.MatchingResponses.Any(string.IsNullOrWhiteSpace)) return "";
            var allowed = pairs.Select(x => x[1]).ToHashSet(StringComparer.Ordinal);
            if (model.MatchingResponses.Any(x => !allowed.Contains(x))) return "";
            return string.Join('\n', pairs.Select((x, index) => $"{x[0]} → {model.MatchingResponses[index]}"));
        }
        var response = model.Response?.Trim() ?? "";
        if (activity.ResponseType == ActivityResponseType.MultipleChoice)
        {
            var allowed = activity.Options.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            return allowed.Contains(response, StringComparer.Ordinal) ? response : "";
        }
        return response;
    }

    [HttpGet("attachments/{id:int}")]
    public async Task<IActionResult> Download(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var file = await db.SubmissionAttachments.AsNoTracking().SingleOrDefaultAsync(x =>
            x.Id == id && x.ActivitySubmission.UserId == userId);
        return file is null ? NotFound() : File(file.Data, file.ContentType, file.FileName);
    }

    [HttpPost("attachments/{id:int}/delete")]
    public async Task<IActionResult> DeleteAttachment(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var file = await db.SubmissionAttachments.Include(x => x.ActivitySubmission)
            .ThenInclude(x => x.LessonActivity).SingleOrDefaultAsync(x =>
                x.Id == id && x.ActivitySubmission.UserId == userId);
        if (file is null) return NotFound();
        var lessonId = file.ActivitySubmission.LessonActivity.LessonId;
        db.SubmissionAttachments.Remove(file);
        file.ActivitySubmission.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        TempData["Success"] = "Attachment removed. Your written activity response remains saved.";
        return RedirectToAction("Details", "Lessons", new { area = "Student", id = lessonId });
    }
}
