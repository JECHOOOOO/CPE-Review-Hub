using CPE_Review_Hub.Data;
using CPE_Review_Hub.Models;
using CPE_Review_Hub.Security;
using CPE_Review_Hub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace CPE_Review_Hub.Areas.Student.Controllers;

[Area("Student"), Authorize(Policy = PortalSecurity.Student), Route("student/subjects")]
public class SubjectsController(ApplicationDbContext db) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(int? level)
    {
        if (level.HasValue && !await db.AcademicLevels.AnyAsync(x => x.Id == level)) return NotFound();
        var query = db.Subjects.AsNoTracking().Where(x => x.IsPublished);
        if (level.HasValue) query = query.Where(x => x.AcademicLevelId == level);
        var levels = await db.AcademicLevels.AsNoTracking().OrderBy(x => x.Id)
            .Select(x => new SelectionOption(x.Id, x.Name)).ToListAsync();
        var subjects = await query.OrderBy(x => x.AcademicLevelId).ThenBy(x => x.Code)
            .Select(x => new StudentSubjectRow(x.Id, x.Code, x.Title, x.Description,
                x.AcademicLevel.Name, x.Lessons.Count(l => l.IsPublished))).ToListAsync();
        return View(new StudentSubjectIndexViewModel(level, levels, subjects));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var subject = await db.Subjects.AsNoTracking().Include(x => x.AcademicLevel)
            .Include(x => x.Lessons).ThenInclude(x => x.ReviewMaterials)
            .Include(x => x.Lessons).ThenInclude(x => x.Activity)
            .Include(x => x.Lessons).ThenInclude(x => x.Assessments)
            .Include(x => x.Assessments).ThenInclude(x => x.AssessmentQuestions)
            .SingleOrDefaultAsync(x => x.Id == id && x.IsPublished);
        if (subject is null) return NotFound();
        var lessonIds = subject.Lessons.Where(x => x.IsPublished).Select(x => x.Id).ToList();
        var completedActivityIds = await db.ActivitySubmissions.AsNoTracking()
            .Where(x => x.UserId == userId && lessonIds.Contains(x.LessonActivity.LessonId))
            .Select(x => x.LessonActivity.LessonId).ToHashSetAsync();
        var submittedAttempts = await db.AssessmentAttempts.AsNoTracking()
            .Where(x => x.UserId == userId && x.Assessment.SubjectId == id && x.SubmittedAtUtc != null)
            .ToListAsync();
        var exam = subject.Assessments.FirstOrDefault(x => x.Type == AssessmentType.FinalExam && x.LessonId is null);
        var rows = subject.Lessons.Where(x => x.IsPublished).OrderBy(x => x.SortOrder).ThenBy(x => x.Title)
            .Select(x =>
            {
                var quizId = x.Assessments.FirstOrDefault(a => a.Type == AssessmentType.LessonQuiz)?.Id;
                var attempts = quizId is null ? [] : submittedAttempts.Where(a => a.AssessmentId == quizId).ToList();
                return new StudentLessonRow(x.Id, x.Title, x.Summary,
                    x.ReviewMaterials.Count(m => m.IsPublished), completedActivityIds.Contains(x.Id),
                    attempts.Count == 0 ? null : attempts.Max(a => a.Percentage));
            }).ToList();
        var completedQuizzes = rows.Count(x => x.BestQuizPercentage is not null);
        var progress = rows.Count == 0 ? 0 : (int)Math.Round(
            100m * (rows.Count(x => x.ActivityCompleted) + completedQuizzes) / (rows.Count * 2));
        var finalAttempts = exam is null ? [] : submittedAttempts.Where(x => x.AssessmentId == exam.Id).ToList();
        var finalUnlocked = rows.Count == 6 && rows.All(x => x.ActivityCompleted && x.BestQuizPercentage is not null);
        return View(new StudentSubjectDetailsViewModel(subject.Id, subject.Code, subject.Title,
            subject.Description, subject.AcademicLevel.Name, rows,
            exam is null ? null : new StudentAssessmentStatusViewModel(exam.Id, exam.Title,
                exam.RequiredQuestionCount, exam.AssessmentQuestions.Count, exam.IsPublished,
                finalUnlocked, finalAttempts.Count == 0 ? null : finalAttempts.Max(x => x.Percentage),
                finalAttempts.Count), progress));
    }
}
