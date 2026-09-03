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
    public decimal? Grade { get; set; }
    [MaxLength(4000)] public string Feedback { get; set; } = "";
    public DateTime? GradedAtUtc { get; set; }
    [MaxLength(128)] public string? GradedById { get; set; }
    public ApplicationUser? GradedBy { get; set; }
    public int Revision { get; set; }

    public void MarkUpdated()
    {
        UpdatedAtUtc = DateTime.UtcNow;
        Grade = null;
        Feedback = "";
        GradedAtUtc = null;
        GradedById = null;
        Revision++;
    }
    public LessonActivity LessonActivity { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
    public ICollection<SubmissionAttachment> Attachments { get; set; } = [];
}
