using CPE_Review_Hub.Data;
using CPE_Review_Hub.Models;
using CPE_Review_Hub.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace CPE_Review_Hub.Services;

/// <summary>Builds a read-only learning overview from the student's existing source-of-truth records.</summary>
public sealed class StudentProfileService(ApplicationDbContext db)
{
    public async Task<StudentProfileLearningViewModel> GetLearningAsync(string userId,
        CancellationToken cancellationToken = default)
    {
        var subjects = await db.Subjects.AsNoTracking()
            .Where(x => x.IsPublished)
            .OrderBy(x => x.AcademicLevelId).ThenBy(x => x.Code)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Title,
                Final = x.Assessments.Where(a => a.Type == AssessmentType.FinalExam &&
                        a.LessonId == null && a.IsPublished)
                    .Select(a => new { a.Id, a.Title }).FirstOrDefault()
            }).ToListAsync(cancellationToken);

        var lessons = await db.Lessons.AsNoTracking()
            .Where(x => x.IsPublished && x.Subject.IsPublished)
            .OrderBy(x => x.Subject.AcademicLevelId).ThenBy(x => x.Subject.Code)
            .ThenBy(x => x.SortOrder).ThenBy(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.SubjectId,
                x.Title,
                ActivityId = x.Activity != null && x.Activity.IsPublished ? (int?)x.Activity.Id : null,
                QuizId = x.Assessments.Where(a => a.Type == AssessmentType.LessonQuiz && a.IsPublished)
                    .Select(a => (int?)a.Id).FirstOrDefault()
            }).ToListAsync(cancellationToken);

        var attempts = await db.AssessmentAttempts.AsNoTracking()
            .Where(x => x.UserId == userId && x.SubmittedAtUtc != null && x.Assessment.IsPublished &&
                        x.Assessment.Subject.IsPublished &&
                        (x.Assessment.Type == AssessmentType.FinalExam
                            ? x.Assessment.LessonId == null
                            : x.Assessment.Lesson != null && x.Assessment.Lesson.IsPublished))
            .OrderByDescending(x => x.SubmittedAtUtc)
            .Select(x => new
            {
                x.AssessmentId,
                x.Assessment.SubjectId,
                CourseCode = x.Assessment.Subject.Code,
                AssessmentTitle = x.Assessment.Title,
                LessonTitle = x.Assessment.Lesson == null ? "" : x.Assessment.Lesson.Title,
                x.Assessment.Type,
                x.Score,
                x.TotalQuestions,
                x.IsPassed,
                SubmittedAtUtc = x.SubmittedAtUtc!.Value
            }).ToListAsync(cancellationToken);

        var submissions = await db.ActivitySubmissions.AsNoTracking()
            .Where(x => x.UserId == userId && x.LessonActivity.IsPublished &&
                        x.LessonActivity.Lesson.IsPublished && x.LessonActivity.Lesson.Subject.IsPublished)
            .OrderByDescending(x => x.CompletedAtUtc)
            .Select(x => new
            {
                x.LessonActivityId,
                x.LessonActivity.LessonId,
                x.LessonActivity.Title,
                LessonTitle = x.LessonActivity.Lesson.Title,
                CourseId = x.LessonActivity.Lesson.SubjectId,
                CourseCode = x.LessonActivity.Lesson.Subject.Code,
                x.CompletedAtUtc
            }).ToListAsync(cancellationToken);

        var attemptLookup = attempts.GroupBy(x => x.AssessmentId)
            .ToDictionary(x => x.Key, x => x.ToList());
        var submittedActivityIds = submissions.Select(x => x.LessonActivityId).ToHashSet();
        var courseStates = subjects.Select(subject =>
        {
            var courseLessons = lessons.Where(x => x.SubjectId == subject.Id).ToList();
            var completedCount = courseLessons.Count(lesson => lesson.QuizId is int quizId &&
                attemptLookup.TryGetValue(quizId, out var quizAttempts) && quizAttempts.Any(a => a.IsPassed));
            var subjectAttempts = attempts.Where(x => x.SubjectId == subject.Id).ToList();
            var subjectSubmissions = submissions.Where(x => x.CourseId == subject.Id).ToList();
            var isStarted = subjectAttempts.Count > 0 || subjectSubmissions.Count > 0;
            var finalPassed = subject.Final is not null && attemptLookup.TryGetValue(subject.Final.Id, out var finalAttempts) &&
                finalAttempts.Any(x => x.IsPassed);
            var finalIsUnlockedByExistingRules = courseLessons.Count == 6 && completedCount == 6;
            var isComplete = courseLessons.Count > 0 && completedCount == courseLessons.Count &&
                (subject.Final is null || !finalIsUnlockedByExistingRules || finalPassed);
            var progress = courseLessons.Count == 0 ? 0 :
                (int)Math.Round(100m * completedCount / courseLessons.Count);
            var latestActivity = subjectAttempts.Select(x => x.SubmittedAtUtc)
                .Concat(subjectSubmissions.Select(x => x.CompletedAtUtc))
                .DefaultIfEmpty(DateTime.MinValue).Max();
            return new { Subject = subject, Lessons = courseLessons, Completed = completedCount,
                IsStarted = isStarted, IsComplete = isComplete, Progress = progress,
                LatestActivity = latestActivity };
        }).Where(x => x.Lessons.Count > 0).ToList();

        var inProgressCount = courseStates.Count(x => x.IsStarted && !x.IsComplete);
        var totalLessons = courseStates.Sum(x => x.Lessons.Count);
        var lessonsCompleted = courseStates.Sum(x => x.Completed);
        var overall = totalLessons == 0 ? 0m : Math.Round(100m * lessonsCompleted / totalLessons, 1);

        var courseProgress = courseStates.Where(x => x.IsStarted).Select(x =>
        {
            var nextLesson = x.Lessons.FirstOrDefault(lesson =>
                lesson.QuizId is not int quizId || !attemptLookup.TryGetValue(quizId, out var quizAttempts) ||
                !quizAttempts.Any(a => a.IsPassed));
            var nextUrl = nextLesson is not null
                ? $"/student/lessons/{nextLesson.Id}"
                : $"/student/subjects/{x.Subject.Id}";
            return new StudentProfileCourseViewModel(x.Subject.Id, x.Subject.Code, x.Subject.Title,
                x.Completed, x.Lessons.Count, x.Progress, nextUrl);
        }).ToList();

        var continueCandidate = courseStates.Where(x => !x.IsComplete && x.IsStarted)
            .OrderByDescending(x => x.LatestActivity).FirstOrDefault()
            ?? courseStates.FirstOrDefault(x => !x.IsComplete);
        StudentProfileContinueViewModel? continueLearning = null;
        if (continueCandidate is not null)
        {
            var nextLesson = continueCandidate.Lessons.FirstOrDefault(lesson =>
                lesson.QuizId is not int quizId || !attemptLookup.TryGetValue(quizId, out var quizAttempts) ||
                !quizAttempts.Any(a => a.IsPassed));
            if (nextLesson is not null)
            {
                var activityDone = nextLesson.ActivityId is null || submittedActivityIds.Contains(nextLesson.ActivityId.Value);
                var quizAttempts = nextLesson.QuizId is int id && attemptLookup.TryGetValue(id, out var found)
                    ? found : [];
                var description = !activityDone
                    ? "Continue the lesson and submit its practice activity before the quiz."
                    : nextLesson.QuizId is null
                        ? "Continue reviewing the next available lesson."
                        : quizAttempts.Count > 0
                            ? "Your lesson is available for review and quiz retry."
                            : "Your activity is complete. Continue to the lesson quiz.";
                var label = !activityDone ? "Continue lesson" : nextLesson.QuizId is null
                    ? "Review lesson" : quizAttempts.Count > 0 ? "Retry quiz" : "Continue to quiz";
                var number = continueCandidate.Lessons.IndexOf(nextLesson) + 1;
                continueLearning = new StudentProfileContinueViewModel(continueCandidate.Subject.Code,
                    continueCandidate.Subject.Title, $"Lesson {number} · {nextLesson.Title}", description,
                    $"/student/lessons/{nextLesson.Id}", label, continueCandidate.Completed,
                    continueCandidate.Lessons.Count);
            }
            else if (continueCandidate.Subject.Final is not null && continueCandidate.Lessons.Count == 6 &&
                     continueCandidate.Completed == 6)
            {
                var finalAttempts = attemptLookup.TryGetValue(continueCandidate.Subject.Final.Id, out var found)
                    ? found : [];
                continueLearning = new StudentProfileContinueViewModel(continueCandidate.Subject.Code,
                    continueCandidate.Subject.Title, "Final examination",
                    finalAttempts.Count > 0 ? "All lessons are complete. Review the course to retry the final examination."
                        : "All lessons are complete. The final examination is ready from the course page.",
                    $"/student/subjects/{continueCandidate.Subject.Id}",
                    finalAttempts.Count > 0 ? "Review final exam" : "Continue to final exam",
                    continueCandidate.Completed, continueCandidate.Lessons.Count);
            }
        }

        var recent = submissions.Select(x => new StudentProfileActivityViewModel(
                "ACTIVITY COMPLETED", x.Title, x.CourseCode, x.LessonTitle, x.CompletedAtUtc))
            .Concat(attempts.Select(x => new StudentProfileActivityViewModel(
                x.Type == AssessmentType.FinalExam ? "FINAL EXAM COMPLETED" :
                    x.IsPassed ? "LESSON COMPLETED" : "QUIZ ATTEMPT",
                x.Type == AssessmentType.FinalExam ? x.AssessmentTitle :
                    x.IsPassed && !string.IsNullOrWhiteSpace(x.LessonTitle) ? x.LessonTitle : x.AssessmentTitle,
                x.CourseCode, $"{(x.IsPassed ? "Passed" : "Submitted")} · {x.Score} / {x.TotalQuestions}",
                x.SubmittedAtUtc)))
            .OrderByDescending(x => x.OccurredAtUtc).Take(5).ToList();

        var quizAssessmentIds = lessons.Where(x => x.QuizId.HasValue).Select(x => x.QuizId!.Value).ToHashSet();
        var finalAssessmentIds = subjects.Where(x => x.Final is not null).Select(x => x.Final!.Id).ToHashSet();
        var submittedAttempts = attempts.ToList();
        var quizzesTaken = submittedAttempts.Count(x => quizAssessmentIds.Contains(x.AssessmentId));
        var quizzesPassed = submittedAttempts.Where(x => quizAssessmentIds.Contains(x.AssessmentId) && x.IsPassed)
            .Select(x => x.AssessmentId).Distinct().Count();
        var examsCompleted = submittedAttempts.Where(x => finalAssessmentIds.Contains(x.AssessmentId))
            .Select(x => x.AssessmentId).Distinct().Count();

        return new StudentProfileLearningViewModel(inProgressCount, lessonsCompleted, totalLessons, overall,
            submissions.Select(x => x.LessonActivityId).Distinct().Count(), quizzesTaken, quizzesPassed,
            examsCompleted, courseProgress, continueLearning, recent);
    }
}
