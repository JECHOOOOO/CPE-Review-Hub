using System.ComponentModel.DataAnnotations;

namespace CPE_Review_Hub.Models;

public class Lesson
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    [MaxLength(200)] public string Title { get; set; } = "";
    [MaxLength(1000)] public string Summary { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public Subject Subject { get; set; } = null!;
    public ICollection<ReviewMaterial> ReviewMaterials { get; set; } = [];
    public ICollection<LessonImage> Images { get; set; } = [];
    public LessonActivity? Activity { get; set; }
    public ICollection<Assessment> Assessments { get; set; } = [];
    public ICollection<Question> Questions { get; set; } = [];
}
