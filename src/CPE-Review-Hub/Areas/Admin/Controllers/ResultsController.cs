using CPE_Review_Hub.Data;
using CPE_Review_Hub.Models;
using CPE_Review_Hub.Security;
using CPE_Review_Hub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CPE_Review_Hub.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Policy = PortalSecurity.Admin), Route("admin/results")]
public class ResultsController(ApplicationDbContext db) : Controller
{
    [HttpGet("")]
    public async Task<IActionResult> Index(AssessmentType? type, int? subjectId)
    {
        var query = db.AssessmentAttempts.AsNoTracking().Where(x => x.SubmittedAtUtc != null);
        if (type.HasValue) query = query.Where(x => x.Assessment.Type == type);
        if (subjectId.HasValue) query = query.Where(x => x.Assessment.SubjectId == subjectId);
        var data = await query.OrderByDescending(x => x.SubmittedAtUtc).Take(300).Select(x => new
        {
            x.Id, x.UserId, x.AssessmentId, StudentName = x.User.FullName, StudentEmail = x.User.Email ?? "",
            SubjectCode = x.Assessment.Subject.Code, AssessmentTitle = x.Assessment.Title, x.Assessment.Type,
            LessonTitle = x.Assessment.Lesson != null ? x.Assessment.Lesson.Title : null,
            x.Score, x.TotalQuestions, x.Percentage, x.IsPassed, Submitted = x.SubmittedAtUtc!.Value
        }).ToListAsync();
        var attemptIds = data.Select(x => x.Id).ToList();
        var numbers = await db.AssessmentAttempts.AsNoTracking().Where(x => x.SubmittedAtUtc != null && attemptIds.Contains(x.Id))
            .Select(x => new { x.Id, Number = db.AssessmentAttempts.Count(a => a.UserId == x.UserId && a.AssessmentId == x.AssessmentId && a.SubmittedAtUtc != null && a.Id <= x.Id) })
            .ToDictionaryAsync(x => x.Id, x => x.Number);
        ViewBag.Subjects = await db.Subjects.AsNoTracking().OrderBy(x => x.Code).Select(x => new SelectionOption(x.Id, x.Code + " — " + x.Title)).ToListAsync();
        ViewData["Type"] = type; ViewData["SubjectId"] = subjectId;
        return View(data.Select(x => new AttemptAdminRow(x.Id, x.StudentName, x.StudentEmail, x.SubjectCode,
            x.AssessmentTitle, x.Type, x.LessonTitle, x.Score, x.TotalQuestions, x.Percentage, x.IsPassed,
            numbers[x.Id], x.Submitted)).ToList());
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Details(int id)
    {
        var x = await db.AssessmentAttempts.AsNoTracking().Where(a => a.Id == id && a.SubmittedAtUtc != null)
            .Select(a => new { a.Id, a.UserId, a.AssessmentId, StudentName = a.User.FullName, StudentEmail = a.User.Email ?? "",
                SubjectCode = a.Assessment.Subject.Code, AssessmentTitle = a.Assessment.Title, a.Assessment.Type,
                LessonTitle = a.Assessment.Lesson != null ? a.Assessment.Lesson.Title : null, a.Score, a.TotalQuestions,
                a.Percentage, a.IsPassed, Submitted = a.SubmittedAtUtc!.Value,
                Answers = a.Answers.OrderBy(z => z.QuestionId).Select(z => new AttemptAnswerAdminRow(0,
                    z.Question.Prompt, z.Choice.Text,
                    z.Question.Choices.Where(c => c.IsCorrect).Select(c => c.Text).FirstOrDefault() ?? "Unavailable",
                    z.IsCorrect, z.Question.Explanation)).ToList() }).SingleOrDefaultAsync();
        if (x is null) return NotFound();
        var number = await db.AssessmentAttempts.CountAsync(a => a.UserId == x.UserId && a.AssessmentId == x.AssessmentId && a.SubmittedAtUtc != null && a.Id <= id);
        var answers = x.Answers.Select((a, i) => a with { Number = i + 1 }).ToList();
        return View(new AttemptAdminDetailsViewModel(x.Id, x.StudentName, x.StudentEmail, x.SubjectCode,
            x.AssessmentTitle, x.Type, x.LessonTitle, x.Score, x.TotalQuestions, x.Percentage, x.IsPassed,
            number, x.Submitted, answers));
    }
}
