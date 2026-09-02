using System.ComponentModel.DataAnnotations;

namespace CPE_Review_Hub.ViewModels;

public class SubjectFormViewModel
{
    public int Id { get; set; }
    [Required, StringLength(20), Display(Name = "Subject code")] public string Code { get; set; } = "";
    [Required, StringLength(160)] public string Title { get; set; } = "";
    [StringLength(1000)] public string Description { get; set; } = "";
    [Range(1, int.MaxValue), Display(Name = "Academic level")] public int AcademicLevelId { get; set; }
    [Display(Name = "Visible to students")] public bool IsPublished { get; set; }
    public IReadOnlyList<SelectionOption> Levels { get; set; } = [];
}

public class LessonFormViewModel
{
    public int Id { get; set; }
    [Range(1, int.MaxValue)] public int SubjectId { get; set; }
    [Required, StringLength(200)] public string Title { get; set; } = "";
    [StringLength(1000)] public string Summary { get; set; } = "";
    [Range(0, 10000), Display(Name = "Display order")] public int SortOrder { get; set; }
    [Display(Name = "Visible to students")] public bool IsPublished { get; set; }
    public IReadOnlyList<SelectionOption> Subjects { get; set; } = [];
}

public class MaterialFormViewModel
{
    public int Id { get; set; }
    [Range(1, int.MaxValue)] public int LessonId { get; set; }
    [Required, StringLength(200)] public string Title { get; set; } = "";
    [Required, StringLength(50000)] public string Content { get; set; } = "";
    [Url, StringLength(2048), Display(Name = "Optional resource URL")] public string? ResourceUrl { get; set; }
    [Range(0, 10000), Display(Name = "Display order")] public int SortOrder { get; set; }
    [Display(Name = "Visible to students")] public bool IsPublished { get; set; }
    public IReadOnlyList<SelectionOption> Lessons { get; set; } = [];
}

public record SelectionOption(int Id, string Label);
public record SubjectAdminRow(int Id, string Code, string Title, string Level, int LessonCount, bool IsPublished);
public record LessonAdminRow(int Id, string Title, string SubjectCode, int SortOrder, int MaterialCount, bool IsPublished);
public record MaterialAdminRow(int Id, string Title, string LessonTitle, string SubjectCode, int SortOrder, bool IsPublished);
public record AdminDashboardViewModel(string FullName, string Email, int StudentCount, int SubjectCount, int LessonCount, int MaterialCount);
public record StudentLevelCard(int Id, string Name, int SubjectCount);
public record StudentDashboardViewModel(string FullName, string Email, IReadOnlyList<StudentLevelCard> Levels);
public record StudentSubjectRow(int Id, string Code, string Title, string Description, string Level, int LessonCount);
public record StudentSubjectIndexViewModel(int? SelectedLevel, IReadOnlyList<SelectionOption> Levels, IReadOnlyList<StudentSubjectRow> Subjects);
public record StudentLessonRow(int Id, string Title, string Summary, int MaterialCount,
    bool ActivityCompleted, decimal? BestQuizPercentage, bool IsUnlocked, bool IsCompleted,
    int PassingPercentage);
public record StudentAssessmentStatusViewModel(int Id, string Title, int RequiredQuestions, int CurrentQuestions,
    bool IsPublished, bool IsUnlocked, decimal? BestPercentage, int AttemptCount)
{
    public bool IsReady => IsPublished && CurrentQuestions >= RequiredQuestions;
}
public record StudentSubjectDetailsViewModel(int Id, string Code, string Title, string Description, string Level,
    IReadOnlyList<StudentLessonRow> Lessons, StudentAssessmentStatusViewModel? FinalExam, int ProgressPercentage);
public record StudentMaterialViewModel(string Title, string Content, string ResourceUrl);
public record StudentActivityViewModel(int Id, string Title, string Instructions, string ExpectedOutput,
    bool IsCompleted, string Response, DateTime? CompletedAtUtc);
public record StudentLessonDetailsViewModel(int Id, string Title, string Summary, string SubjectCode, int SubjectId,
    IReadOnlyList<StudentMaterialViewModel> Materials, StudentActivityViewModel? Activity,
    StudentAssessmentStatusViewModel? Quiz);

public class ActivitySubmissionViewModel
{
    [Required, StringLength(10000, MinimumLength = 50)] public string Response { get; set; } = "";
}

public class AssessmentSubmissionViewModel
{
    public int AttemptId { get; set; }
    public Dictionary<int, int> Answers { get; set; } = [];
}

public record AssessmentChoiceViewModel(int Id, string Text);
public record AssessmentQuestionViewModel(int Id, int Number, string Prompt,
    IReadOnlyList<AssessmentChoiceViewModel> Choices);
public record AssessmentTakeViewModel(int AttemptId, string Title, string CourseCode, string AssessmentLabel,
    int DurationMinutes, int PassingPercentage, int AttemptNumber,
    IReadOnlyList<AssessmentQuestionViewModel> Questions, string? Error);
public record AssessmentAnswerResultViewModel(int Number, string Prompt, string SelectedAnswer,
    string CorrectAnswer, bool IsCorrect, string Explanation);
public record AssessmentResultViewModel(int AttemptId, int AssessmentId, string Title, string CourseCode,
    int Score, int Total, decimal Percentage, bool IsPassed, int AttemptNumber, DateTime SubmittedAtUtc,
    IReadOnlyList<AssessmentAnswerResultViewModel> Answers);
public record CourseProgressRow(int SubjectId, string Code, string Title, int CompletedActivities,
    int CompletedQuizzes, int LessonCount, int ProgressPercentage, decimal? BestFinalPercentage);
public record AssessmentHistoryRow(int AttemptId, string CourseCode, string Title, string Type,
    int Score, int Total, decimal Percentage, bool IsPassed, DateTime SubmittedAtUtc);
public record StudentProgressViewModel(IReadOnlyList<CourseProgressRow> Courses,
    IReadOnlyList<AssessmentHistoryRow> History);
