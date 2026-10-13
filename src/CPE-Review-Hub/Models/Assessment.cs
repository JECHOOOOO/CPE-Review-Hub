using System.ComponentModel.DataAnnotations;

namespace CPE_Review_Hub.Models;

public enum AssessmentType
{
    LessonQuiz = 1,
    FinalExam = 2
}

public class Assessment
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public int? LessonId { get; set; }
    public AssessmentType Type { get; set; }
    [MaxLength(240)] public string Title { get; set; } = "";
    public int RequiredQuestionCount { get; set; }
    public int DurationMinutes { get; set; }
    public int PassingPercentage { get; set; } = 70;
    public bool IsPublished { get; set; }
    public Subject Subject { get; set; } = null!;
    public Lesson? Lesson { get; set; }
    public ICollection<AssessmentQuestion> AssessmentQuestions { get; set; } = [];
    public ICollection<AssessmentAttempt> Attempts { get; set; } = [];
}
