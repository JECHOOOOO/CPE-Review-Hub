using CPE_Review_Hub.Data;
using CPE_Review_Hub.Models;
using CPE_Review_Hub.Security;
using CPE_Review_Hub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CPE_Review_Hub.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Policy = PortalSecurity.Admin), Route("admin/statistics")]
public class StatisticsController(ApplicationDbContext db) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index()
    {
        var students = await db.Users.AsNoTracking().Where(x => x.AccountType == PortalSecurity.Student)
            .Select(x => x.Id).ToListAsync();
        var subjects = await db.Subjects.AsNoTracking().Where(x => x.IsPublished).OrderBy(x => x.AcademicLevelId).ThenBy(x => x.Code)
            .Select(x => new { x.Id, x.Code, x.Title }).ToListAsync();
        var lessons = await db.Lessons.AsNoTracking().Where(x => x.IsPublished && x.Subject.IsPublished)
            .OrderBy(x => x.SortOrder).Select(x => new { x.Id, x.SubjectId,
                ActivityId = x.Activity != null && x.Activity.IsPublished ? (int?)x.Activity.Id : null,
                QuizId = x.Assessments.Where(a => a.Type == AssessmentType.LessonQuiz && a.IsPublished)
                    .Select(a => (int?)a.Id).FirstOrDefault() }).ToListAsync();
        var attempts = await db.AssessmentAttempts.AsNoTracking().Where(x => x.SubmittedAtUtc != null)
            .Select(x => new { x.UserId, x.AssessmentId, x.Percentage, x.IsPassed, x.Assessment.SubjectId, x.Assessment.Type }).ToListAsync();
        var submissions = await db.ActivitySubmissions.AsNoTracking().Select(x => new { x.UserId, x.LessonActivityId }).ToListAsync();
        var publishedActivityCount = lessons.Count(x => x.ActivityId.HasValue);
        var validActivityIds = lessons.Where(x => x.ActivityId.HasValue).Select(x => x.ActivityId!.Value).ToHashSet();
        var studentIds = students.ToHashSet();
        var participation = submissions.Where(x => studentIds.Contains(x.UserId) && validActivityIds.Contains(x.LessonActivityId)).ToList();
        var possibleActivityCompletions = (long)students.Count * publishedActivityCount;
        var rows = new List<CourseStatisticsRow>();
        long completedPairs = 0, possiblePairs = 0;
        foreach (var subject in subjects)
        {
            var subjectAttempts = attempts.Where(x => x.SubjectId == subject.Id).ToList();
            var subjectLessons = lessons.Where(x => x.SubjectId == subject.Id).ToList();
            var completedStudents = 0;
            foreach (var student in students)
            {
                var completed = 0;
                foreach (var lesson in subjectLessons)
                {
                    var quizOkay = lesson.QuizId.HasValue && subjectAttempts.Any(x => x.UserId == student && x.AssessmentId == lesson.QuizId && x.IsPassed);
                    if (quizOkay) completed++;
                }
                completedPairs += completed; possiblePairs += subjectLessons.Count;
                if (subjectLessons.Count == 6 && completed == 6) completedStudents++;
            }
            rows.Add(new CourseStatisticsRow(subject.Id, subject.Code, subject.Title,
                subjectAttempts.Count(x => x.Type == AssessmentType.LessonQuiz), subjectAttempts.Count(x => x.Type == AssessmentType.FinalExam),
                subjectAttempts.Count == 0 ? 0 : Math.Round(subjectAttempts.Average(x => x.Percentage), 1), completedStudents,
                students.Count == 0 ? 0 : Math.Round(100m * completedStudents / students.Count, 1)));
        }
        var quizzes = attempts.Where(x => x.Type == AssessmentType.LessonQuiz).ToList();
        var exams = attempts.Where(x => x.Type == AssessmentType.FinalExam).ToList();
        return View(new AdminStatisticsViewModel(students.Count, quizzes.Count, exams.Count,
            quizzes.Count == 0 ? 0 : Math.Round(quizzes.Average(x => x.Percentage), 1),
            exams.Count == 0 ? 0 : Math.Round(exams.Average(x => x.Percentage), 1), attempts.Count(x => x.IsPassed),
            attempts.Count(x => !x.IsPassed), possiblePairs == 0 ? 0 : Math.Round(100m * completedPairs / possiblePairs, 1),
            participation.Count, participation.Select(x => x.UserId).Distinct().Count(),
            possibleActivityCompletions == 0 ? 0 : Math.Round(100m * participation.Count / possibleActivityCompletions, 1), rows));
    }
}
