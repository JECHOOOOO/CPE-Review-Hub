namespace CPE_Review_Hub.Models;

public class AssessmentQuestion
{
    public int AssessmentId { get; set; }
    public int QuestionId { get; set; }
    public int SortOrder { get; set; }
    public Assessment Assessment { get; set; } = null!;
    public Question Question { get; set; } = null!;
}
