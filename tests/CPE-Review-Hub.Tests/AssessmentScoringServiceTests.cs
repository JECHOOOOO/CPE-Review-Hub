using CPE_Review_Hub.Services;
using Xunit;

namespace CPE_Review_Hub.Tests;

public sealed class AssessmentScoringServiceTests
{
    private readonly AssessmentScoringService service = new();

    [Theory]
    [InlineData(7, 10, 70, 70, true)]
    [InlineData(6, 10, 70, 60, false)]
    [InlineData(8, 10, 80, 80, true)]
    [InlineData(37, 50, 70, 74, true)]
    public void Calculate_returns_automatic_score(int correct, int total, int passing,
        decimal expectedPercentage, bool expectedPassed)
    {
        var result = service.Calculate(correct, total, passing);

        Assert.Equal(correct, result.Score);
        Assert.Equal(total, result.TotalQuestions);
        Assert.Equal(expectedPercentage, result.Percentage);
        Assert.Equal(expectedPassed, result.IsPassed);
    }

    [Theory]
    [InlineData(-1, 10, 70)]
    [InlineData(11, 10, 70)]
    [InlineData(0, 0, 70)]
    [InlineData(5, 10, 101)]
    public void Calculate_rejects_invalid_values(int correct, int total, int passing) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => service.Calculate(correct, total, passing));
}
