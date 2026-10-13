using CPE_Review_Hub.Data;
using CPE_Review_Hub.Models;
using CPE_Review_Hub.Security;
using CPE_Review_Hub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CPE_Review_Hub.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Policy = PortalSecurity.Admin), Route("admin/assessments")]
public class AssessmentsController(ApplicationDbContext db) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(AssessmentType type = AssessmentType.LessonQuiz)
    {
        ViewData["Type"] = type;
        var rows = await db.Assessments.AsNoTracking().Where(x => x.Type == type)
            .OrderBy(x => x.Subject.AcademicLevelId).ThenBy(x => x.Subject.Code).ThenBy(x => x.Lesson!.SortOrder)
            .Select(x => new AssessmentAdminRow(x.Id, x.Subject.Code, x.Lesson != null ? x.Lesson.Title : null,
                x.Title, x.Type, x.RequiredQuestionCount, x.AssessmentQuestions.Count(q => q.Question.IsActive),
                x.PassingPercentage, x.IsPublished, x.Attempts.Count(a => a.SubmittedAtUtc != null))).ToListAsync();
        return View(rows);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var item = await db.Assessments.AsNoTracking().Where(x => x.Id == id).Select(x => new
        {
            x.Id, SubjectCode = x.Subject.Code, LessonTitle = x.Lesson != null ? x.Lesson.Title : null,
            x.Title, x.Type, x.RequiredQuestionCount, x.PassingPercentage, x.DurationMinutes, x.IsPublished,
            AttemptCount = x.Attempts.Count(a => a.SubmittedAtUtc != null),
            Questions = x.AssessmentQuestions.OrderBy(q => q.SortOrder).Select(q => new QuestionAdminRow(
                q.QuestionId, q.SortOrder, q.Question.Prompt,
                q.Question.Choices.Where(c => c.IsCorrect).Select(c => c.Text).FirstOrDefault() ?? "Missing",
                q.Question.IsActive, q.Question.Choices.Count, q.Question.AttemptAnswers.Count)).ToList()
        }).SingleOrDefaultAsync();
        if (item is null) return NotFound();
        return View(new AssessmentAdminDetailsViewModel(item.Id, item.SubjectCode, item.LessonTitle, item.Title,
            item.Type, item.RequiredQuestionCount, item.PassingPercentage, item.DurationMinutes,
            item.IsPublished, item.AttemptCount, item.Questions));
    }

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id)
    {
        var assessment = await db.Assessments.AsNoTracking().Include(x => x.Subject).Include(x => x.Lesson)
            .SingleOrDefaultAsync(x => x.Id == id);
        if (assessment is null) return NotFound();
        return View(new AssessmentSettingsViewModel { Id = id, Title = assessment.Title,
            RequiredQuestionCount = assessment.RequiredQuestionCount, DurationMinutes = assessment.DurationMinutes,
            PassingPercentage = assessment.PassingPercentage, IsPublished = assessment.IsPublished,
            ContextLabel = Label(assessment), CurrentQuestionCount = await ActiveCount(id) });
    }

    [HttpPost("{id:int}/edit"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AssessmentSettingsViewModel model)
    {
        if (id != model.Id) return BadRequest();
        var assessment = await db.Assessments.Include(x => x.Subject).Include(x => x.Lesson)
            .SingleOrDefaultAsync(x => x.Id == id);
        if (assessment is null) return NotFound();
        model.ContextLabel = Label(assessment);
        model.CurrentQuestionCount = await ActiveCount(id);
        if (model.IsPublished && model.CurrentQuestionCount < model.RequiredQuestionCount)
            ModelState.AddModelError(nameof(model.IsPublished), "Add enough active questions before publishing.");
        if (!ModelState.IsValid) return View(model);
        assessment.Title = model.Title.Trim();
        assessment.RequiredQuestionCount = model.RequiredQuestionCount;
        assessment.DurationMinutes = model.DurationMinutes;
        assessment.PassingPercentage = model.PassingPercentage;
        assessment.IsPublished = model.IsPublished;
        await db.SaveChangesAsync();
        TempData["Success"] = "Assessment settings saved.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private Task<int> ActiveCount(int id) => db.AssessmentQuestions.CountAsync(x => x.AssessmentId == id && x.Question.IsActive);
    private static string Label(Assessment x) => $"{x.Subject.Code} · {(x.Type == AssessmentType.FinalExam ? "Final exam" : x.Lesson?.Title)}";
}
