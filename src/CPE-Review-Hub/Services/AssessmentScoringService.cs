namespace CPE_Review_Hub.Services;

public sealed record AssessmentScore(int Score, int TotalQuestions, decimal Percentage, bool IsPassed);

public sealed class AssessmentScoringService
{
    public AssessmentScore Calculate(int correctAnswers, int totalQuestions, int passingPercentage)
    {
        if (totalQuestions <= 0) throw new ArgumentOutOfRangeException(nameof(totalQuestions));
        if (correctAnswers < 0 || correctAnswers > totalQuestions)
            throw new ArgumentOutOfRangeException(nameof(correctAnswers));
        if (passingPercentage is < 0 or > 100)
            throw new ArgumentOutOfRangeException(nameof(passingPercentage));

        var percentage = Math.Round(100m * correctAnswers / totalQuestions, 2);
        return new AssessmentScore(correctAnswers, totalQuestions, percentage,
            percentage >= passingPercentage);
    }
}
