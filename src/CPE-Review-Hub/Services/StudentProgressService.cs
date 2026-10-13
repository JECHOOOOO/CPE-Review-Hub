using CPE_Review_Hub.Data;
using CPE_Review_Hub.Models;
using Microsoft.EntityFrameworkCore;

namespace CPE_Review_Hub.Services;

public sealed record LessonProgressState(
    int LessonId,
    int SortOrder,
    int? ActivityId,
    int? QuizId,
    int PassingPercentage,
    bool ActivityCompleted,
    bool QuizPassed,
    bool IsCompleted,
    bool IsUnlocked,
    decimal? BestQuizPercentage,
    int AttemptCount,
    int? ActivitySubmissionId);

public sealed record CourseProgressState(
    int SubjectId,
    IReadOnlyList<LessonProgressState> Lessons,
    bool FinalExamUnlocked,
    int CompletionPercentage);

public sealed class StudentProgressService(ApplicationDbContext db)
{
    public async Task<CourseProgressState?> GetCourseAsync(int subjectId, string userId)
    {
        var lessons = await db.Lessons.AsNoTracking()
            .Where(x => x.SubjectId == subjectId && x.IsPublished && x.Subject.IsPublished)
            .OrderBy(x => x.SortOrder).ThenBy(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.SortOrder,
                ActivityId = x.Activity != null && x.Activity.IsPublished ? (int?)x.Activity.Id : null,
                Quiz = x.Assessments.Where(a => a.Type == AssessmentType.LessonQuiz && a.IsPublished)
                    .Select(a => new { a.Id, a.PassingPercentage }).FirstOrDefault()
            }).ToListAsync();
        if (lessons.Count == 0) return await db.Subjects.AnyAsync(x => x.Id == subjectId && x.IsPublished)
            ? new CourseProgressState(subjectId, [], false, 0)
            : null;

        var activityIds = lessons.Where(x => x.ActivityId.HasValue).Select(x => x.ActivityId!.Value).ToList();
        var quizIds = lessons.Where(x => x.Quiz != null).Select(x => x.Quiz!.Id).ToList();
        var submissions = await db.ActivitySubmissions.AsNoTracking()
            .Where(x => x.UserId == userId && activityIds.Contains(x.LessonActivityId))
            .Select(x => new { x.LessonActivityId, x.Id }).ToDictionaryAsync(x => x.LessonActivityId, x => x.Id);
        var attempts = await db.AssessmentAttempts.AsNoTracking()
            .Where(x => x.UserId == userId && x.SubmittedAtUtc != null && quizIds.Contains(x.AssessmentId))
            .Select(x => new { x.AssessmentId, x.Percentage, x.IsPassed }).ToListAsync();

        var states = new List<LessonProgressState>(lessons.Count);
        var previousCompleted = true;
        foreach (var lesson in lessons)
        {
            var quizAttempts = attempts.Where(x => lesson.Quiz != null &&
                x.AssessmentId == lesson.Quiz.Id).ToList();
            var activityCompleted = lesson.ActivityId is null || submissions.ContainsKey(lesson.ActivityId.Value);
            var quizPassed = quizAttempts.Any(x => x.IsPassed);
            // The activity unlocks the quiz; only the automatically scored quiz completes the lesson.
            var completed = lesson.Quiz != null && quizPassed;
            states.Add(new LessonProgressState(
                lesson.Id, lesson.SortOrder, lesson.ActivityId, lesson.Quiz?.Id,
                lesson.Quiz?.PassingPercentage ?? 70, activityCompleted, quizPassed,
                completed, previousCompleted,
                quizAttempts.Count == 0 ? null : quizAttempts.Max(x => x.Percentage),
                quizAttempts.Count,
                lesson.ActivityId.HasValue && submissions.TryGetValue(lesson.ActivityId.Value, out var submissionId)
                    ? submissionId : null));
            previousCompleted = previousCompleted && completed;
        }

        var completedCount = states.Count(x => x.IsCompleted);
        return new CourseProgressState(subjectId, states,
            states.Count == 6 && states.All(x => x.IsCompleted),
            states.Count == 0 ? 0 : (int)Math.Round(100m * completedCount / states.Count));
    }

    public async Task<bool> IsLessonUnlockedAsync(int lessonId, string userId)
    {
        var target = await db.Lessons.AsNoTracking().Where(x => x.Id == lessonId && x.IsPublished)
            .Select(x => new { x.Id, x.SubjectId, x.SortOrder }).SingleOrDefaultAsync();
        if (target is null) return false;

        return await IsLessonUnlockedAsync(target.Id, target.SubjectId, target.SortOrder, userId);
    }

    public async Task<bool> IsLessonUnlockedAsync(
        int lessonId, int subjectId, int sortOrder, string userId)
    {

        // Opening one lesson used to load the entire course, every submission,
        // and every quiz attempt through four additional Aiven round trips.
        // A lesson is unlocked exactly when no earlier published lesson lacks a
        // passed automatically-scored quiz, so let MySQL answer that directly.
        var hasIncompletePreviousLesson = await db.Lessons.AsNoTracking()
            .Where(x => x.SubjectId == subjectId && x.IsPublished &&
                (x.SortOrder < sortOrder ||
                 (x.SortOrder == sortOrder && x.Id < lessonId)))
            .AnyAsync(previous => !db.AssessmentAttempts.Any(attempt =>
                attempt.UserId == userId && attempt.SubmittedAtUtc != null && attempt.IsPassed &&
                attempt.Assessment.LessonId == previous.Id &&
                attempt.Assessment.Type == AssessmentType.LessonQuiz && attempt.Assessment.IsPublished));
        return !hasIncompletePreviousLesson;
    }

    public async Task<bool> IsAssessmentUnlockedAsync(Assessment assessment, string userId)
    {
        var course = await GetCourseAsync(assessment.SubjectId, userId);
        if (course is null) return false;
        if (assessment.Type == AssessmentType.FinalExam) return course.FinalExamUnlocked;
        var lesson = course.Lessons.SingleOrDefault(x => x.LessonId == assessment.LessonId);
        return lesson is { IsUnlocked: true, ActivityCompleted: true };
    }
}
