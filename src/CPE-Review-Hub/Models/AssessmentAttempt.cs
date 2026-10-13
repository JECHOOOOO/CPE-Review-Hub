using System.ComponentModel.DataAnnotations;

namespace CPE_Review_Hub.Models;

public class AssessmentAttempt
{
    public int Id { get; set; }
    public int AssessmentId { get; set; }
    [MaxLength(128)] public string UserId { get; set; } = "";
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime? SubmittedAtUtc { get; set; }
    public int Score { get; set; }
    public int TotalQuestions { get; set; }
    public decimal Percentage { get; set; }
    public bool IsPassed { get; set; }
    public Assessment Assessment { get; set; } = null!;
    public ApplicationUser User { get; set; } = null!;
    public ICollection<AttemptAnswer> Answers { get; set; } = [];
}
