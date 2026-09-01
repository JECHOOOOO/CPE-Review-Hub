using System.ComponentModel.DataAnnotations;

namespace CPE_Review_Hub.Models;

public class ActivitySubmission
{
    public int Id { get; set; }
    public int LessonActivityId { get; set; }
    [MaxLength(128)] public string UserId { get; set; } = "";
    [MaxLength(10000)] public string Response { get; set; } = "";
    public DateTime CompletedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public LessonActivity LessonActivity { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
}
