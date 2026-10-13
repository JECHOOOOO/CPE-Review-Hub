namespace CPE_Review_Hub.Models;

public class AttemptAnswer
{
    public int AssessmentAttemptId { get; set; }
    public int QuestionId { get; set; }
    public int ChoiceId { get; set; }
    public bool IsCorrect { get; set; }
    public AssessmentAttempt AssessmentAttempt { get; set; } = null!;
    public Question Question { get; set; } = null!;
    public Choice Choice { get; set; } = null!;
}
