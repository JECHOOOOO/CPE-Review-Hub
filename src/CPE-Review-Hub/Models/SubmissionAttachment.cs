using System.ComponentModel.DataAnnotations;

namespace CPE_Review_Hub.Models;

public class SubmissionAttachment
{
    public int Id { get; set; }
    public int ActivitySubmissionId { get; set; }
    [MaxLength(255)] public string FileName { get; set; } = "";
    [MaxLength(100)] public string ContentType { get; set; } = "application/octet-stream";
    public long FileSize { get; set; }
    public byte[] Data { get; set; } = [];
    public DateTime UploadedAtUtc { get; set; } = DateTime.UtcNow;
    public ActivitySubmission ActivitySubmission { get; set; } = null!;
}
