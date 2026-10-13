using System.ComponentModel.DataAnnotations;

namespace CPE_Review_Hub.Models;

public class Choice
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
    [MaxLength(1000)] public string Text { get; set; } = "";
    public bool IsCorrect { get; set; }
    public int SortOrder { get; set; }
    public Question Question { get; set; } = null!;
    public ICollection<AttemptAnswer> AttemptAnswers { get; set; } = [];
}
