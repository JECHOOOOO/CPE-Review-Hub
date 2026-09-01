namespace CPE_Review_Hub.Services.Curriculum;

public sealed record QuizQuestionSeed(string Prompt, string Explanation,
    IReadOnlyList<string> Choices, int CorrectChoiceIndex);

public static class CurriculumQuizBuilder
{
    public static IReadOnlyList<QuizQuestionSeed> Build(CurriculumLesson lesson)
    {
        var questions = new List<QuizQuestionSeed>();
        for (var i = 0; i < 5; i++)
        {
            var correct = lesson.Concepts[i];
            var alternatives = new[]
            {
                lesson.Concepts[(i + 1) % lesson.Concepts.Count],
                lesson.Concepts[(i + 2) % lesson.Concepts.Count],
                lesson.Concepts[(i + 3) % lesson.Concepts.Count]
            };
            var correctIndex = i % 4;
            var ordered = alternatives.ToList();
            ordered.Insert(correctIndex, correct);
            questions.Add(new QuizQuestionSeed(
                $"Which statement correctly defines {correct.Term} in this lesson?",
                $"{correct.Term}: {correct.Definition}", ordered.Select(x => x.Definition).ToList(), correctIndex));
        }

        var steps = lesson.Method.Split('>', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var correctMethod = string.Join(" → ", steps);
        questions.Add(new QuizQuestionSeed(
            $"Which sequence correctly applies the method taught in “{lesson.Title}”?",
            $"The lesson uses this order: {correctMethod}",
            [correctMethod, string.Join(" → ", steps.Reverse()),
                $"Skip the analysis → {steps.Last()} → guess the result",
                $"{steps.Last()} → ignore constraints → {steps.First()}"], 0));

        questions.Add(new QuizQuestionSeed(
            "Which key relationship or design rule is taught in this lesson?",
            lesson.KeyRule,
            ["Use an unrelated average and omit the stated variables.", lesson.KeyRule,
                "The result is valid even when units and assumptions contradict the problem.",
                "No verification is needed when an answer appears plausible."], 1));

        questions.Add(new QuizQuestionSeed(
            $"Based on the worked example — {lesson.WorkedExample} — which conclusion is supported?",
            lesson.ExampleResult,
            [$"The example cannot be analyzed using any concept from {lesson.Title}.",
                "All input values may be discarded without changing the conclusion.", lesson.ExampleResult,
                "The opposite conclusion is guaranteed without checking assumptions."], 2));

        questions.Add(new QuizQuestionSeed(
            "Which approach best satisfies the required lesson activity?",
            $"The activity requires application of the lesson concepts: {lesson.Activity}",
            ["Submit only a copied definition with no application or evidence.",
                "Use a topic that was not discussed and omit the required output.",
                "Skip the procedure and report an unsupported answer.",
                $"Apply the lesson concepts and governing rule to this task: {lesson.Activity}"], 3));

        questions.Add(new QuizQuestionSeed(
            $"Which statement best summarizes the engineering focus of “{lesson.Title}”?",
            $"The lesson focuses on {lesson.Focus} and requires a checked, interpretable result.",
            [$"It focuses on {lesson.Focus} and connects definitions, method, example, and verification.",
                "It teaches that engineering decisions should ignore constraints and evidence.",
                "It requires memorizing an answer while avoiding the stated method.",
                "It concludes that definitions and practical examples are interchangeable."], 0));
        return questions;
    }
}
