using CPE_Review_Hub.Data;
using CPE_Review_Hub.Models;
using CPE_Review_Hub.Security;
using CPE_Review_Hub.Services;
using CPE_Review_Hub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CPE_Review_Hub.Areas.Student.Controllers;

[Area("Student"), Authorize(Policy = PortalSecurity.Student), Route("student/lessons")]
public class LessonsController(ApplicationDbContext db, StudentProgressService progressService) : Controller
{
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var lesson = await db.Lessons.AsNoTracking().Include(x => x.Subject)
            .Include(x => x.ReviewMaterials).Include(x => x.Activity)
            .Include(x => x.Assessments).ThenInclude(x => x.AssessmentQuestions)
            .SingleOrDefaultAsync(x => x.Id == id && x.IsPublished && x.Subject.IsPublished);
        if (lesson is null) return NotFound();
        if (!await progressService.IsLessonUnlockedAsync(lesson.Id, userId))
        {
            TempData["Error"] = "Complete and pass the previous lesson before opening this lesson.";
            return RedirectToAction("Details", "Subjects", new { area = "Student", id = lesson.SubjectId });
        }
        var quiz = lesson.Assessments.FirstOrDefault(x => x.Type == AssessmentType.LessonQuiz);
        var submission = lesson.Activity is null ? null : await db.ActivitySubmissions.AsNoTracking().Include(x => x.Attachments)
            .SingleOrDefaultAsync(x => x.UserId == userId && x.LessonActivityId == lesson.Activity.Id);
        var quizAttempts = quiz is null ? [] : await db.AssessmentAttempts.AsNoTracking()
            .Where(x => x.UserId == userId && x.AssessmentId == quiz.Id && x.SubmittedAtUtc != null)
            .ToListAsync();
        return View(new StudentLessonDetailsViewModel(lesson.Id, lesson.Title, lesson.Summary,
            lesson.Subject.Code, lesson.SubjectId, lesson.ReviewMaterials.Where(x => x.IsPublished)
                .OrderBy(x => x.SortOrder).ThenBy(x => x.Title)
                .Select(x => new StudentMaterialViewModel(x.Title, x.Content, x.ResourceUrl)).ToList(),
            lesson.Activity is { IsPublished: true } activity
                ? new StudentActivityViewModel(activity.Id, activity.Title, activity.Instructions,
                    activity.ExpectedOutput, submission is not null, submission?.Response ?? "",
                    submission?.CompletedAtUtc, submission?.Attachments.OrderBy(x => x.UploadedAtUtc)
                        .Select(x => new AttachmentViewModel(x.Id, x.FileName, x.ContentType, x.FileSize)).ToList() ?? []) : null,
            quiz is null ? null : new StudentAssessmentStatusViewModel(quiz.Id, quiz.Title,
                quiz.RequiredQuestionCount, quiz.AssessmentQuestions.Count, quiz.IsPublished,
                submission is not null, quizAttempts.Count == 0 ? null : quizAttempts.Max(x => x.Percentage),
                quizAttempts.Count)));
    }
}
