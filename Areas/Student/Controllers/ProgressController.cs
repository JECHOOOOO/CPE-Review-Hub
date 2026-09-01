using System.Security.Claims;
using CPE_Review_Hub.Data;
using CPE_Review_Hub.Models;
using CPE_Review_Hub.Security;
using CPE_Review_Hub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CPE_Review_Hub.Areas.Student.Controllers;

[Area("Student"), Authorize(Policy = PortalSecurity.Student), Route("student/progress")]
public class ProgressController(ApplicationDbContext db) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var subjects = await db.Subjects.AsNoTracking().AsSplitQuery().Where(x => x.IsPublished)
            .Include(x => x.Lessons.Where(l => l.IsPublished)).ThenInclude(x => x.Activity)
            .Include(x => x.Lessons.Where(l => l.IsPublished)).ThenInclude(x => x.Assessments)
            .Include(x => x.Assessments)
            .OrderBy(x => x.AcademicLevelId).ThenBy(x => x.Code).ToListAsync();
        var lessonIds = subjects.SelectMany(x => x.Lessons).Select(x => x.Id).ToList();
        var completedLessonIds = await db.ActivitySubmissions.AsNoTracking()
            .Where(x => x.UserId == userId && lessonIds.Contains(x.LessonActivity.LessonId))
            .Select(x => x.LessonActivity.LessonId).ToHashSetAsync();
        var attempts = await db.AssessmentAttempts.AsNoTracking()
            .Where(x => x.UserId == userId && x.SubmittedAtUtc != null).ToListAsync();

        var courses = subjects.Select(subject =>
        {
            var quizIds = subject.Lessons.SelectMany(x => x.Assessments)
                .Where(x => x.Type == AssessmentType.LessonQuiz).Select(x => x.Id).ToHashSet();
            var completedQuizzes = attempts.Where(x => quizIds.Contains(x.AssessmentId))
                .Select(x => x.AssessmentId).Distinct().Count();
            var completedActivities = subject.Lessons.Count(x => completedLessonIds.Contains(x.Id));
            var stepCount = Math.Max(1, subject.Lessons.Count * 2);
            var finalId = subject.Assessments.FirstOrDefault(x =>
                x.Type == AssessmentType.FinalExam && x.LessonId is null)?.Id;
            var finalAttempts = finalId.HasValue ? attempts.Where(x => x.AssessmentId == finalId.Value).ToList() : [];
            return new CourseProgressRow(subject.Id, subject.Code, subject.Title,
                completedActivities, completedQuizzes, subject.Lessons.Count,
                (int)Math.Round(100m * (completedActivities + completedQuizzes) / stepCount),
                finalAttempts.Count == 0 ? null : finalAttempts.Max(x => x.Percentage));
        }).ToList();

        var history = await db.AssessmentAttempts.AsNoTracking()
            .Where(x => x.UserId == userId && x.SubmittedAtUtc != null)
            .OrderByDescending(x => x.SubmittedAtUtc)
            .Select(x => new AssessmentHistoryRow(x.Id, x.Assessment.Subject.Code,
                x.Assessment.Title, x.Assessment.Type == AssessmentType.FinalExam ? "Final exam" : "Lesson quiz",
                x.Score, x.TotalQuestions, x.Percentage, x.IsPassed, x.SubmittedAtUtc!.Value))
            .ToListAsync();
        return View(new StudentProgressViewModel(courses, history));
    }
}
