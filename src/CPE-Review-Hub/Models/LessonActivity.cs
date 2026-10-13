using System.ComponentModel.DataAnnotations;

namespace CPE_Review_Hub.Models;

public class LessonActivity
{
    public int Id { get; set; }
    public int LessonId { get; set; }
    [MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(6000)] public string Instructions { get; set; } = "";
    [MaxLength(2000)] public string ExpectedOutput { get; set; } = "";
    public ActivityResponseType ResponseType { get; set; } = ActivityResponseType.ShortAnswer;
    [MaxLength(4000)] public string Options { get; set; } = "";
    public bool IsPublished { get; set; }
    public Lesson Lesson { get; set; } = null!;
    public ICollection<ActivitySubmission> Submissions { get; set; } = [];
}

public enum ActivityResponseType
{
    ShortAnswer = 1,
    Calculation = 2,
    Identification = 3,
    Coding = 4,
    MultipleChoice = 5,
    Matching = 6
}
