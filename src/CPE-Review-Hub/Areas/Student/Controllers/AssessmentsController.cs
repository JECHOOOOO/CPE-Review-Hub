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

[Area("Student"), Authorize(Policy = PortalSecurity.Student), Route("student/assessments")]
public class AssessmentsController(ApplicationDbContext db, StudentProgressService progressService,
    AssessmentScoringService scoringService) : Controller
{
    [HttpPost("{id:int}/start")]
    public async Task<IActionResult> Start(int id)
    {
        var assessment = await LoadAssessmentAsync(id);
        if (assessment is null || !assessment.IsPublished ||
            assessment.AssessmentQuestions.Count < assessment.RequiredQuestionCount) return NotFound();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await progressService.IsAssessmentUnlockedAsync(assessment, userId))
        {
            TempData["Error"] = assessment.Type == AssessmentType.LessonQuiz
                ? "Complete the current lesson activity and any previous lesson before starting this quiz."
                : "Complete every lesson activity and pass all six lesson quizzes before starting the final exam.";
            return ReturnToContent(assessment);
        }

        var attempt = new AssessmentAttempt
        {
            AssessmentId = assessment.Id, UserId = userId,
            StartedAtUtc = DateTime.UtcNow, TotalQuestions = assessment.RequiredQuestionCount
        };
        db.AssessmentAttempts.Add(attempt);
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Take), new { attemptId = attempt.Id });
    }

    [HttpGet("attempts/{attemptId:int}")]
    public async Task<IActionResult> Take(int attemptId)
    {
        var attempt = await LoadAttemptAsync(attemptId);
        if (attempt is null) return NotFound();
        if (attempt.SubmittedAtUtc is not null) return RedirectToAction(nameof(Result), new { attemptId });
        return View(BuildTakeModel(attempt, await GetAttemptNumberAsync(attempt)));
    }

    [HttpPost("attempts/{attemptId:int}/submit")]
    public async Task<IActionResult> Submit(int attemptId, AssessmentSubmissionViewModel model)
    {
        var attempt = await LoadAttemptAsync(attemptId);
        if (attempt is null || model.AttemptId != attemptId) return NotFound();
        if (attempt.SubmittedAtUtc is not null) return RedirectToAction(nameof(Result), new { attemptId });

        var questions = attempt.Assessment.AssessmentQuestions.OrderBy(x => x.SortOrder)
            .Take(attempt.TotalQuestions).Select(x => x.Question).ToList();
        var invalid = questions.Any(q => !model.Answers.TryGetValue(q.Id, out var choiceId) ||
            q.Choices.All(c => c.Id != choiceId));
        if (invalid)
        {
            return View("Take", BuildTakeModel(attempt, await GetAttemptNumberAsync(attempt),
                $"Answer all {attempt.TotalQuestions} questions before submitting."));
        }

        foreach (var question in questions)
        {
            var choice = question.Choices.Single(x => x.Id == model.Answers[question.Id]);
            attempt.Answers.Add(new AttemptAnswer
            {
                AssessmentAttempt = attempt, QuestionId = question.Id,
                ChoiceId = choice.Id, IsCorrect = choice.IsCorrect
            });
        }
        var result = scoringService.Calculate(attempt.Answers.Count(x => x.IsCorrect),
            attempt.TotalQuestions, attempt.Assessment.PassingPercentage);
        attempt.Score = result.Score;
        attempt.Percentage = result.Percentage;
        attempt.IsPassed = result.IsPassed;
        attempt.SubmittedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Result), new { attemptId });
    }

    [HttpGet("attempts/{attemptId:int}/result")]
    public async Task<IActionResult> Result(int attemptId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var attempt = await db.AssessmentAttempts.AsNoTracking().AsSplitQuery()
            .Include(x => x.Assessment).ThenInclude(x => x.Subject)
            .Include(x => x.Assessment).ThenInclude(x => x.AssessmentQuestions)
            .Include(x => x.Answers).ThenInclude(x => x.Choice)
            .Include(x => x.Answers).ThenInclude(x => x.Question).ThenInclude(x => x.Choices)
            .SingleOrDefaultAsync(x => x.Id == attemptId && x.UserId == userId && x.SubmittedAtUtc != null);
        if (attempt is null) return NotFound();
        var questionOrder = attempt.Assessment.AssessmentQuestions.ToDictionary(x => x.QuestionId, x => x.SortOrder);
        var answers = attempt.Answers.OrderBy(x => questionOrder.GetValueOrDefault(x.QuestionId))
            .Select((answer, index) => new AssessmentAnswerResultViewModel(index + 1,
                answer.Question.Prompt, answer.Choice.Text,
                answer.Question.Choices.Single(x => x.IsCorrect).Text,
                answer.IsCorrect, answer.Question.Explanation)).ToList();
        var attemptNumber = await db.AssessmentAttempts.CountAsync(x => x.UserId == userId &&
            x.AssessmentId == attempt.AssessmentId && x.SubmittedAtUtc != null && x.Id <= attempt.Id);
        return View(new AssessmentResultViewModel(attempt.Id, attempt.AssessmentId,
            attempt.Assessment.Title, attempt.Assessment.Subject.Code, attempt.Score,
            attempt.TotalQuestions, attempt.Percentage, attempt.IsPassed, attemptNumber,
            attempt.SubmittedAtUtc!.Value, answers));
    }

    private async Task<Assessment?> LoadAssessmentAsync(int id) => await db.Assessments.AsNoTracking()
        .AsSplitQuery().Include(x => x.Subject).Include(x => x.Lesson).ThenInclude(x => x!.Activity)
        .Include(x => x.AssessmentQuestions).ThenInclude(x => x.Question).ThenInclude(x => x.Choices)
        .SingleOrDefaultAsync(x => x.Id == id && x.Subject.IsPublished &&
            (x.Lesson == null || x.Lesson.IsPublished));

    private async Task<AssessmentAttempt?> LoadAttemptAsync(int attemptId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        return await db.AssessmentAttempts.AsSplitQuery()
            .Include(x => x.Assessment).ThenInclude(x => x.Subject)
            .Include(x => x.Assessment).ThenInclude(x => x.Lesson)
            .Include(x => x.Assessment).ThenInclude(x => x.AssessmentQuestions)
                .ThenInclude(x => x.Question).ThenInclude(x => x.Choices)
            .Include(x => x.Answers)
            .SingleOrDefaultAsync(x => x.Id == attemptId && x.UserId == userId);
    }

    private async Task<int> GetAttemptNumberAsync(AssessmentAttempt attempt) =>
        await db.AssessmentAttempts.CountAsync(x => x.UserId == attempt.UserId &&
            x.AssessmentId == attempt.AssessmentId && x.Id <= attempt.Id &&
            (x.SubmittedAtUtc != null || x.Id == attempt.Id));

    private static AssessmentTakeViewModel BuildTakeModel(AssessmentAttempt attempt, int attemptNumber,
        string? error = null)
    {
        var questions = attempt.Assessment.AssessmentQuestions.OrderBy(x => x.SortOrder)
            .Take(attempt.TotalQuestions).Select((x, index) => new AssessmentQuestionViewModel(
                x.QuestionId, index + 1, x.Question.Prompt, x.Question.Choices
                    .OrderBy(c => c.SortOrder).Select(c => new AssessmentChoiceViewModel(c.Id, c.Text)).ToList()))
            .ToList();
        return new AssessmentTakeViewModel(attempt.Id, attempt.Assessment.Title,
            attempt.Assessment.Subject.Code,
            attempt.Assessment.Type == AssessmentType.FinalExam ? "FINAL EXAM" : "LESSON QUIZ",
            attempt.Assessment.DurationMinutes, attempt.Assessment.PassingPercentage,
            attemptNumber, questions, error);
    }

    private IActionResult ReturnToContent(Assessment assessment) => assessment.LessonId.HasValue
        ? RedirectToAction("Details", "Lessons", new { area = "Student", id = assessment.LessonId.Value })
        : RedirectToAction("Details", "Subjects", new { area = "Student", id = assessment.SubjectId });
}
