using System.ComponentModel.DataAnnotations;

namespace CPE_Review_Hub.Models;

public class Question
{
    public int Id { get; set; }
    [MaxLength(100)] public string? SourceKey { get; set; }
    public int SubjectId { get; set; }
    public int? LessonId { get; set; }
    [MaxLength(2000)] public string Prompt { get; set; } = "";
    [MaxLength(4000)] public string Explanation { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public Subject Subject { get; set; } = null!;
    public Lesson? Lesson { get; set; }
    public ICollection<Choice> Choices { get; set; } = [];
    public ICollection<AssessmentQuestion> AssessmentQuestions { get; set; } = [];
    public ICollection<AttemptAnswer> AttemptAnswers { get; set; } = [];
}
