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

[Area("Student"), Authorize(Policy = PortalSecurity.Student), Route("student/subjects")]
public class SubjectsController(ApplicationDbContext db, StudentProgressService progressService) : Controller
{
    [HttpGet(""), HttpGet("/student/courses")]
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
        var progressState = await progressService.GetCourseAsync(id, userId);
        if (progressState is null) return NotFound();
        var submittedAttempts = await db.AssessmentAttempts.AsNoTracking()
            .Where(x => x.UserId == userId && x.Assessment.SubjectId == id && x.SubmittedAtUtc != null)
            .ToListAsync();
        var exam = subject.Assessments.FirstOrDefault(x => x.Type == AssessmentType.FinalExam && x.LessonId is null);
        var rows = subject.Lessons.Where(x => x.IsPublished).OrderBy(x => x.SortOrder).ThenBy(x => x.Title)
            .Select(x =>
            {
                var lessonProgress = progressState.Lessons.Single(p => p.LessonId == x.Id);
                var quizId = x.Assessments.FirstOrDefault(a => a.Type == AssessmentType.LessonQuiz)?.Id;
                var attempts = quizId is null ? [] : submittedAttempts.Where(a => a.AssessmentId == quizId).ToList();
                return new StudentLessonRow(x.Id, x.Title, x.Summary,
                    x.ReviewMaterials.Count(m => m.IsPublished), lessonProgress.ActivityCompleted,
                    attempts.Count == 0 ? null : attempts.Max(a => a.Percentage),
                    lessonProgress.IsUnlocked, lessonProgress.IsCompleted,
                    lessonProgress.PassingPercentage);
            }).ToList();
        var finalAttempts = exam is null ? [] : submittedAttempts.Where(x => x.AssessmentId == exam.Id).ToList();
        return View(new StudentSubjectDetailsViewModel(subject.Id, subject.Code, subject.Title,
            subject.Description, subject.AcademicLevel.Name, rows,
            exam is null ? null : new StudentAssessmentStatusViewModel(exam.Id, exam.Title,
                exam.RequiredQuestionCount, exam.AssessmentQuestions.Count, exam.IsPublished,
                progressState.FinalExamUnlocked,
                finalAttempts.Count == 0 ? null : finalAttempts.Max(x => x.Percentage),
                finalAttempts.Count), progressState.CompletionPercentage));
    }
}
