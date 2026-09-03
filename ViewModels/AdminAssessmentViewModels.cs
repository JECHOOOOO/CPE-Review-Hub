using System.ComponentModel.DataAnnotations;
using CPE_Review_Hub.Models;

namespace CPE_Review_Hub.ViewModels;

public record AssessmentAdminRow(int Id, string SubjectCode, string? LessonTitle, string Title,
    AssessmentType Type, int RequiredQuestions, int CurrentQuestions, int PassingPercentage,
    bool IsPublished, int AttemptCount);

public record QuestionAdminRow(int Id, int Number, string Prompt, string CorrectAnswer,
    bool IsActive, int ChoiceCount, int AttemptAnswerCount);

public record AssessmentAdminDetailsViewModel(int Id, string SubjectCode, string? LessonTitle,
    string Title, AssessmentType Type, int RequiredQuestions, int PassingPercentage,
    int DurationMinutes, bool IsPublished, int AttemptCount, IReadOnlyList<QuestionAdminRow> Questions);

public class AssessmentSettingsViewModel
{
    public int Id { get; set; }
    [Required, StringLength(240)] public string Title { get; set; } = "";
    [Range(1, 200), Display(Name = "Required question count")] public int RequiredQuestionCount { get; set; }
    [Range(1, 300), Display(Name = "Suggested duration (minutes)")] public int DurationMinutes { get; set; }
    [Range(1, 100), Display(Name = "Passing percentage")] public int PassingPercentage { get; set; }
    [Display(Name = "Published to students")] public bool IsPublished { get; set; }
    public string ContextLabel { get; set; } = "";
    public int CurrentQuestionCount { get; set; }
}

public class QuestionFormViewModel
{
    public int Id { get; set; }
    [Range(1, int.MaxValue)] public int AssessmentId { get; set; }
    [Display(Name = "Lesson source")] public int? LessonId { get; set; }
    [Required, StringLength(2000), Display(Name = "Question")] public string Prompt { get; set; } = "";
    [Required, StringLength(4000)] public string Explanation { get; set; } = "";
    [Required, StringLength(1000), Display(Name = "Choice A")] public string ChoiceA { get; set; } = "";
    [Required, StringLength(1000), Display(Name = "Choice B")] public string ChoiceB { get; set; } = "";
    [Required, StringLength(1000), Display(Name = "Choice C")] public string ChoiceC { get; set; } = "";
    [Required, StringLength(1000), Display(Name = "Choice D")] public string ChoiceD { get; set; } = "";
    [Range(0, 3), Display(Name = "Correct choice")] public int CorrectChoiceIndex { get; set; }
    [Display(Name = "Active question")] public bool IsActive { get; set; } = true;
    public string AssessmentLabel { get; set; } = "";
    public AssessmentType AssessmentType { get; set; }
    public IReadOnlyList<SelectionOption> Lessons { get; set; } = [];
}

public record AttemptAdminRow(int Id, string StudentName, string StudentEmail, string SubjectCode,
    string AssessmentTitle, AssessmentType Type, string? LessonTitle, int Score, int Total,
    decimal Percentage, bool IsPassed, int AttemptNumber, DateTime SubmittedAtUtc);

public record AttemptAnswerAdminRow(int Number, string Prompt, string SelectedAnswer,
    string CorrectAnswer, bool IsCorrect, string Explanation);

public record AttemptAdminDetailsViewModel(int Id, string StudentName, string StudentEmail,
    string SubjectCode, string AssessmentTitle, AssessmentType Type, string? LessonTitle,
    int Score, int Total, decimal Percentage, bool IsPassed, int AttemptNumber,
    DateTime SubmittedAtUtc, IReadOnlyList<AttemptAnswerAdminRow> Answers);

public record CourseStatisticsRow(int SubjectId, string Code, string Title, int QuizAttempts,
    int ExamAttempts, decimal AverageScore, int CompletedStudents, decimal CompletionRate);

public record AdminStatisticsViewModel(int StudentCount, int QuizAttempts, int ExamAttempts,
    decimal AverageQuizScore, decimal AverageExamScore, int PassedAttempts, int FailedAttempts,
    decimal LessonCompletionRate, IReadOnlyList<CourseStatisticsRow> Courses);

public record AdminStudentLessonProgressRow(int Number, string Title, bool ActivityCompleted,
    bool QuizPassed, bool IsCompleted, bool IsUnlocked, decimal? BestScore, int? ActivitySubmissionId);

public record AdminStudentCourseProgressRow(int SubjectId, string Code, string Title,
    int CompletionPercentage, bool FinalExamUnlocked,
    IReadOnlyList<AdminStudentLessonProgressRow> Lessons);

public record AdminStudentProgressViewModel(string StudentId, string StudentName, string StudentEmail,
    IReadOnlyList<AdminStudentCourseProgressRow> Courses);

public record AdminActivitySubmissionViewModel(int Id, string StudentName, string StudentEmail,
    string SubjectCode, string LessonTitle, string ActivityTitle, string Response,
    DateTime CompletedAtUtc, DateTime UpdatedAtUtc, IReadOnlyList<AttachmentViewModel> Attachments,
    string Instructions, string ExpectedOutput, DateTime? GradedAtUtc, string? GraderName,
    ActivityGradeViewModel Grade);

public class ActivityGradeViewModel
{
    [Required, Range(typeof(decimal), "0", "100"), Display(Name = "Score out of 100")]
    public decimal? Score { get; set; }
    [StringLength(4000), Display(Name = "Feedback for the student")]
    public string? Feedback { get; set; }
    [Required, Range(0, int.MaxValue)] public int? Revision { get; set; }
}

public record ActivitySubmissionAdminRow(int Id, string StudentName, string StudentEmail,
    string SubjectCode, string LessonTitle, DateTime UpdatedAtUtc, int AttachmentCount, decimal? Grade);

public record ActivitySubmissionIndexViewModel(string? Search, string Status, int Page,
    bool HasNextPage, IReadOnlyList<ActivitySubmissionAdminRow> Submissions);
