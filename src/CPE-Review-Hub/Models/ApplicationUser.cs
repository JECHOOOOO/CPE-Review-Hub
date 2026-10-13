using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace CPE_Review_Hub.Models;

public class ApplicationUser : IdentityUser
{
    [MaxLength(120)] public string FullName { get; set; } = "";
    [MaxLength(16)] public string AccountType { get; set; } = "Student";
    [MaxLength(120)] public string University { get; set; } = "";
    [MaxLength(40)] public string YearLevel { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public ICollection<ActivitySubmission> ActivitySubmissions { get; set; } = [];
    public ICollection<AssessmentAttempt> AssessmentAttempts { get; set; } = [];
}
