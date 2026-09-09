using System.ComponentModel.DataAnnotations;

namespace CPE_Review_Hub.Models;

public class LessonImage
{
    public int Id { get; set; }
    // Uploaded assets persist in MySQL; bundled diagrams keep their existing paths.
    public byte[]? ImageData { get; set; }
    [MaxLength(50)] public string? ContentType { get; set; }
    public int LessonId { get; set; }
    [MaxLength(500)] public string ImagePath { get; set; } = "";
    [MaxLength(500)] public string AltText { get; set; } = "";
    [MaxLength(1000)] public string Caption { get; set; } = "";
    public int DisplayOrder { get; set; }
    public bool IsPublished { get; set; } = true;
    public Lesson Lesson { get; set; } = null!;
}
