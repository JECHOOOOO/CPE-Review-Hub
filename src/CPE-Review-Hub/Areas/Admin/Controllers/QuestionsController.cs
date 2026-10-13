using CPE_Review_Hub.Data;
using CPE_Review_Hub.Models;
using CPE_Review_Hub.Security;
using CPE_Review_Hub.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CPE_Review_Hub.Areas.Admin.Controllers;

[Area("Admin"), Authorize(Policy = PortalSecurity.Admin), Route("admin/questions")]
public class QuestionsController(ApplicationDbContext db) : Controller
{
    [HttpGet("create")]
    public async Task<IActionResult> Create(int assessmentId)
    {
        var model = new QuestionFormViewModel { AssessmentId = assessmentId };
        return await Prepare(model) ? View("Edit", model) : NotFound();
    }

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, int assessmentId)
    {
        var q = await db.Questions.AsNoTracking().Include(x => x.Choices)
            .SingleOrDefaultAsync(x => x.Id == id && x.AssessmentQuestions.Any(a => a.AssessmentId == assessmentId));
        if (q is null) return NotFound();
        var choices = q.Choices.OrderBy(x => x.SortOrder).ToList();
        var model = new QuestionFormViewModel { Id = id, AssessmentId = assessmentId, LessonId = q.LessonId,
            Prompt = q.Prompt, Explanation = q.Explanation, IsActive = q.IsActive,
            ChoiceA = choices.ElementAtOrDefault(0)?.Text ?? "", ChoiceB = choices.ElementAtOrDefault(1)?.Text ?? "",
            ChoiceC = choices.ElementAtOrDefault(2)?.Text ?? "", ChoiceD = choices.ElementAtOrDefault(3)?.Text ?? "",
            CorrectChoiceIndex = Math.Max(0, choices.FindIndex(x => x.IsCorrect)) };
        await Prepare(model);
        return View(model);
    }

    [HttpPost("save"), ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(QuestionFormViewModel model)
    {
        var assessment = await db.Assessments.Include(x => x.Subject).Include(x => x.Lesson)
            .SingleOrDefaultAsync(x => x.Id == model.AssessmentId);
        if (assessment is null) return NotFound();
        if (assessment.Type == AssessmentType.LessonQuiz) model.LessonId = assessment.LessonId;
        if (!model.LessonId.HasValue || !await db.Lessons.AnyAsync(x => x.Id == model.LessonId && x.SubjectId == assessment.SubjectId))
            ModelState.AddModelError(nameof(model.LessonId), "Select a lesson from this course so every question is based on taught content.");
        var texts = new[] { model.ChoiceA, model.ChoiceB, model.ChoiceC, model.ChoiceD }.Select(x => x?.Trim() ?? "").ToArray();
        if (texts.Any(string.IsNullOrWhiteSpace) || texts.Distinct(StringComparer.OrdinalIgnoreCase).Count() != 4)
            ModelState.AddModelError(nameof(model.ChoiceA), "Enter four different, non-empty choices.");
        if (!ModelState.IsValid) { await Prepare(model, assessment); return View("Edit", model); }

        if (model.Id == 0)
        {
            var next = (await db.AssessmentQuestions.Where(x => x.AssessmentId == assessment.Id)
                .MaxAsync(x => (int?)x.SortOrder) ?? 0) + 1;
            var question = BuildQuestion(assessment.SubjectId, model, texts);
            db.Questions.Add(question);
            db.AssessmentQuestions.Add(new AssessmentQuestion { Assessment = assessment, Question = question, SortOrder = next });
        }
        else
        {
            var mapping = await db.AssessmentQuestions.Include(x => x.Question).ThenInclude(x => x.Choices)
                .SingleOrDefaultAsync(x => x.AssessmentId == assessment.Id && x.QuestionId == model.Id);
            if (mapping is null) return NotFound();
            var mustPreserve = await db.AttemptAnswers.AnyAsync(x => x.QuestionId == model.Id) ||
                await db.AssessmentQuestions.CountAsync(x => x.QuestionId == model.Id) > 1;
            if (mustPreserve)
            {
                var replacement = BuildQuestion(assessment.SubjectId, model, texts);
                db.AssessmentQuestions.Remove(mapping);
                db.AssessmentQuestions.Add(new AssessmentQuestion { AssessmentId = assessment.Id,
                    Question = replacement, SortOrder = mapping.SortOrder });
            }
            else
            {
                mapping.Question.SourceKey = null;
                mapping.Question.LessonId = model.LessonId;
                mapping.Question.Prompt = model.Prompt.Trim();
                mapping.Question.Explanation = model.Explanation.Trim();
                mapping.Question.IsActive = model.IsActive;
                var choices = mapping.Question.Choices.OrderBy(x => x.SortOrder).ToList();
                if (choices.Count != 4)
                {
                    db.Choices.RemoveRange(choices);
                    mapping.Question.Choices = texts.Select((text, i) => new Choice { Text = text,
                        SortOrder = i + 1, IsCorrect = i == model.CorrectChoiceIndex }).ToList();
                }
                else for (var i = 0; i < 4; i++) { choices[i].Text = texts[i]; choices[i].IsCorrect = i == model.CorrectChoiceIndex; }
            }
        }
        await db.SaveChangesAsync();
        TempData["Success"] = model.Id == 0 ? "Question added." : "Question updated without changing past attempt records.";
        return RedirectToAction("Details", "Assessments", new { area = "Admin", id = assessment.Id });
    }

    [HttpGet("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id, int assessmentId)
    {
        var item = await db.AssessmentQuestions.AsNoTracking().Where(x => x.AssessmentId == assessmentId && x.QuestionId == id)
            .Select(x => new QuestionAdminRow(x.QuestionId, x.SortOrder, x.Question.Prompt,
                x.Question.Choices.Where(c => c.IsCorrect).Select(c => c.Text).FirstOrDefault() ?? "Missing",
                x.Question.IsActive, x.Question.Choices.Count, x.Question.AttemptAnswers.Count)).SingleOrDefaultAsync();
        if (item is null) return NotFound();
        ViewData["AssessmentId"] = assessmentId;
        return View(item);
    }

    [HttpPost("{id:int}/delete"), ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, int assessmentId)
    {
        var mapping = await db.AssessmentQuestions.Include(x => x.Assessment)
            .SingleOrDefaultAsync(x => x.AssessmentId == assessmentId && x.QuestionId == id);
        if (mapping is null) return NotFound();
        var used = await db.AttemptAnswers.AnyAsync(x => x.QuestionId == id);
        var shared = await db.AssessmentQuestions.CountAsync(x => x.QuestionId == id) > 1;
        db.AssessmentQuestions.Remove(mapping);
        if (!used && !shared) { var question = await db.Questions.FindAsync(id); if (question != null) db.Questions.Remove(question); }
        var remaining = await db.AssessmentQuestions.CountAsync(x => x.AssessmentId == assessmentId && x.QuestionId != id && x.Question.IsActive);
        if (remaining < mapping.Assessment.RequiredQuestionCount) mapping.Assessment.IsPublished = false;
        await db.SaveChangesAsync();
        TempData["Success"] = "Question removed from this assessment. It was unpublished if it no longer has enough questions.";
        return RedirectToAction("Details", "Assessments", new { area = "Admin", id = assessmentId });
    }

    private static Question BuildQuestion(int subjectId, QuestionFormViewModel model, string[] texts) => new()
    {
        SubjectId = subjectId, LessonId = model.LessonId, Prompt = model.Prompt.Trim(), Explanation = model.Explanation.Trim(),
        IsActive = model.IsActive, Choices = texts.Select((text, i) => new Choice { Text = text, SortOrder = i + 1,
            IsCorrect = i == model.CorrectChoiceIndex }).ToList()
    };

    private async Task<bool> Prepare(QuestionFormViewModel model, Assessment? assessment = null)
    {
        assessment ??= await db.Assessments.AsNoTracking().Include(x => x.Subject).Include(x => x.Lesson)
            .SingleOrDefaultAsync(x => x.Id == model.AssessmentId);
        if (assessment is null) return false;
        model.AssessmentType = assessment.Type;
        model.AssessmentLabel = $"{assessment.Subject.Code} · {assessment.Title}";
        model.Lessons = await db.Lessons.AsNoTracking().Where(x => x.SubjectId == assessment.SubjectId)
            .OrderBy(x => x.SortOrder).Select(x => new SelectionOption(x.Id, $"Lesson {x.SortOrder}: {x.Title}")).ToListAsync();
        if (assessment.Type == AssessmentType.LessonQuiz) model.LessonId = assessment.LessonId;
        return true;
    }
}
