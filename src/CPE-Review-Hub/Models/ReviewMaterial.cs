using System.ComponentModel.DataAnnotations;

namespace CPE_Review_Hub.Models;

public class ReviewMaterial
{
    public int Id { get; set; }
    public int LessonId { get; set; }
    [MaxLength(200)] public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    [MaxLength(2048)] public string ResourceUrl { get; set; } = "";
    public int SortOrder { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public Lesson Lesson { get; set; } = null!;
}
