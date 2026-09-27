using System.ComponentModel.DataAnnotations;

namespace CPE_Review_Hub.Models;

public class Subject
{
    public int Id { get; set; }
    public int AcademicLevelId { get; set; }
    [MaxLength(20)] public string Code { get; set; } = "";
    [MaxLength(160)] public string Title { get; set; } = "";
    [MaxLength(1000)] public string Description { get; set; } = "";
    public decimal? Units { get; set; }
    public int? Semester { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public AcademicLevel AcademicLevel { get; set; } = null!;
    public ICollection<Lesson> Lessons { get; set; } = [];
    public ICollection<Assessment> Assessments { get; set; } = [];
    public ICollection<Question> Questions { get; set; } = [];
}
