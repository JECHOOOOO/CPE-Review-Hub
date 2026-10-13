using System.ComponentModel.DataAnnotations;
using CPE_Review_Hub.Models;

namespace CPE_Review_Hub.ViewModels;

public class SubjectFormViewModel
{
    public int Id { get; set; }
    [Required, StringLength(20), Display(Name = "Subject code")] public string Code { get; set; } = "";
    [Required, StringLength(160)] public string Title { get; set; } = "";
    [StringLength(1000)] public string Description { get; set; } = "";
    [Range(typeof(decimal), "0.5", "50"), Display(Name = "Units")] public decimal? Units { get; set; }
    [Range(1, 2), Display(Name = "Semester")] public int? Semester { get; set; }
    [Range(1, int.MaxValue), Display(Name = "Academic year")] public int AcademicLevelId { get; set; }
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
    [StringLength(200), Display(Name = "Activity title")] public string ActivityTitle { get; set; } = "";
    [StringLength(6000), Display(Name = "Activity instructions")] public string ActivityInstructions { get; set; } = "";
    [StringLength(2000), Display(Name = "Expected output")] public string ActivityExpectedOutput { get; set; } = "";
    [Display(Name = "Response type")] public ActivityResponseType ActivityResponseType { get; set; } = ActivityResponseType.ShortAnswer;
    [StringLength(4000), Display(Name = "Choices or matches")] public string ActivityOptions { get; set; } = "";
    [Display(Name = "Publish this icebreaker activity")] public bool ActivityIsPublished { get; set; }
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

public class LessonImageFormViewModel
{
    public int Id { get; set; }
    [Range(1, int.MaxValue)] public int LessonId { get; set; }
    [Required, Url, StringLength(500), Display(Name = "Direct online image URL")] public string ImagePath { get; set; } = "";
    [Required, Url, StringLength(500), Display(Name = "Source page URL")] public string SourceUrl { get; set; } = "";
    [StringLength(120), Display(Name = "Source name")] public string SourceName { get; set; } = "Wikimedia Commons";
    [StringLength(200)] public string License { get; set; } = "";
    [StringLength(200)] public string Author { get; set; } = "";
    [Required, StringLength(500), Display(Name = "Alternative text")] public string AltText { get; set; } = "";
    [StringLength(1000)] public string Caption { get; set; } = "";
    [Range(0, 10000), Display(Name = "Display order")] public int DisplayOrder { get; set; }
    [Display(Name = "Visible to students")] public bool IsPublished { get; set; } = true;
    public IReadOnlyList<SelectionOption> Lessons { get; set; } = [];
}

public record SelectionOption(int Id, string Label);
public record SubjectAdminRow(int Id, string Code, string Title, string Level, decimal? Units, int? Semester,
    int LessonCount, bool IsPublished);
public record LessonAdminRow(int Id, string Title, string SubjectCode, int SortOrder, int MaterialCount, bool IsPublished);
public record MaterialAdminRow(int Id, string Title, string LessonTitle, string SubjectCode, int SortOrder, bool IsPublished);
public record LessonImageAdminRow(int Id, string ImagePath, string AltText, string LessonTitle,
    string SubjectCode, int DisplayOrder, bool IsPublished);
public record AdminDashboardViewModel(string FullName, string Email, int StudentCount, int SubjectCount, int LessonCount, int MaterialCount);
public record StudentLevelCard(int Id, string Name, int SubjectCount);
public record StudentDashboardViewModel(string FullName, string Email, IReadOnlyList<StudentLevelCard> Levels);
public record StudentProfilePageViewModel(string FullName, string Email, string University, string YearLevel,
    bool IsActive, DateTime MemberSinceUtc, string Initials, ProfileViewModel EditProfile,
    StudentProfileLearningViewModel Learning);
public record StudentProfileLearningViewModel(int CoursesInProgress, int LessonsCompleted, int TotalLessons,
    decimal OverallProgressPercentage, int ActivitiesCompleted, int QuizzesTaken, int QuizzesPassed,
    int FinalExamsCompleted, IReadOnlyList<StudentProfileCourseViewModel> Courses,
    StudentProfileContinueViewModel? ContinueLearning,
    IReadOnlyList<StudentProfileActivityViewModel> RecentActivity);
public record StudentProfileCourseViewModel(int SubjectId, string Code, string Title,
    int CompletedLessons, int TotalLessons, int ProgressPercentage, string ContinueUrl);
public record StudentProfileContinueViewModel(string CourseCode, string CourseTitle, string StepTitle,
    string Description, string Url, string ActionLabel, int CompletedLessons, int TotalLessons);
public record StudentProfileActivityViewModel(string Kind, string Title, string CourseCode,
    string Details, DateTime OccurredAtUtc);
public record StudentSubjectRow(int Id, string Code, string Title, string Description, string Level, int LessonCount,
    decimal? Units, int? Semester);
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
public record StudentLessonImageViewModel(string ImagePath, string SourceUrl, string SourceName,
    string License, string Author, string AltText, string Caption, int DisplayOrder);
public record StudentActivityViewModel(int Id, string Title, string Instructions, string ExpectedOutput,
    ActivityResponseType ResponseType, IReadOnlyList<string> Options, IReadOnlyList<ActivityMatchViewModel> Matches,
    bool IsCompleted, string Response, DateTime? CompletedAtUtc,
    IReadOnlyList<AttachmentViewModel> Attachments);
public record ActivityMatchViewModel(string Term, string Definition);
public record AttachmentViewModel(int Id, string FileName, string ContentType, long FileSize);
public record StudentLessonDetailsViewModel(int Id, string Title, string Summary, string SubjectCode, int SubjectId,
    IReadOnlyList<StudentMaterialViewModel> Materials, IReadOnlyList<StudentLessonImageViewModel> Images,
    StudentActivityViewModel? Activity,
    StudentAssessmentStatusViewModel? Quiz)
{
    public string SubjectTitle { get; init; } = "";
    public int LessonNumber { get; init; }
    public int LessonCount { get; init; }
}

public class ActivitySubmissionViewModel
{
    [StringLength(10000)] public string Response { get; set; } = "";
    public List<string> MatchingResponses { get; set; } = [];
    [Display(Name = "Attach files or images")] public List<IFormFile> Attachments { get; set; } = [];
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
