using CPE_Review_Hub.ViewModels;
using Xunit;

namespace CPE_Review_Hub.Tests;

public sealed class AssessmentTakeModelSecurityTests
{
    [Fact]
    public void Assessment_take_view_models_do_not_expose_correct_answers_or_scores()
    {
        var takeModelProperties = typeof(AssessmentTakeViewModel).GetProperties();
        var questionProperties = typeof(AssessmentQuestionViewModel).GetProperties();
        var choiceProperties = typeof(AssessmentChoiceViewModel).GetProperties();

        Assert.DoesNotContain(takeModelProperties.Concat(questionProperties).Concat(choiceProperties), property =>
            property.Name.Contains("correct", StringComparison.OrdinalIgnoreCase) ||
            property.Name.Contains("answerkey", StringComparison.OrdinalIgnoreCase) ||
            property.Name.Contains("score", StringComparison.OrdinalIgnoreCase) ||
            property.Name.Contains("passed", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(choiceProperties, property => property.Name == nameof(AssessmentChoiceViewModel.Id));
        Assert.Contains(choiceProperties, property => property.Name == nameof(AssessmentChoiceViewModel.Text));
    }
}
