using System.ComponentModel.DataAnnotations;

namespace CPE_Review_Hub.Models;

public class LessonImage
{
    public int Id { get; set; }
    public int LessonId { get; set; }
    [MaxLength(500)] public string ImagePath { get; set; } = "";
    [MaxLength(500)] public string AltText { get; set; } = "";
    [MaxLength(1000)] public string Caption { get; set; } = "";
    public int DisplayOrder { get; set; }
    public bool IsPublished { get; set; } = true;
    public Lesson Lesson { get; set; } = null!;
}
