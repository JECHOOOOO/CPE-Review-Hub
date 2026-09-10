using System.ComponentModel.DataAnnotations;

namespace CPE_Review_Hub.Models;

public class LessonImage
{
    public int Id { get; set; }
    // Legacy uploaded assets remain readable so existing records are not broken.
    public byte[]? ImageData { get; set; }
    [MaxLength(50)] public string? ContentType { get; set; }
    public int LessonId { get; set; }
    [MaxLength(500)] public string ImagePath { get; set; } = "";
    [MaxLength(500)] public string SourceUrl { get; set; } = "";
    [MaxLength(120)] public string SourceName { get; set; } = "";
    [MaxLength(200)] public string License { get; set; } = "";
    [MaxLength(200)] public string Author { get; set; } = "";
    [MaxLength(500)] public string AltText { get; set; } = "";
    [MaxLength(1000)] public string Caption { get; set; } = "";
    public int DisplayOrder { get; set; }
    public bool IsPublished { get; set; } = true;
    public Lesson Lesson { get; set; } = null!;
}
