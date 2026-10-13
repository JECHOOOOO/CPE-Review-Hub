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
        var swappedStart = steps.ToArray();
        (swappedStart[0], swappedStart[1]) = (swappedStart[1], swappedStart[0]);
        var swappedMiddle = steps.ToArray();
        var middle = Math.Max(1, steps.Length / 2);
        (swappedMiddle[middle - 1], swappedMiddle[middle]) = (swappedMiddle[middle], swappedMiddle[middle - 1]);
        var swappedEnd = steps.ToArray();
        (swappedEnd[^1], swappedEnd[^2]) = (swappedEnd[^2], swappedEnd[^1]);
        questions.Add(new QuizQuestionSeed(
            $"Which sequence correctly applies the method taught in “{lesson.Title}”?",
            $"The lesson uses this order: {correctMethod}",
            [correctMethod, string.Join(" → ", swappedStart),
                string.Join(" → ", swappedMiddle), string.Join(" → ", swappedEnd)], 0));

        var ruleChoices = lesson.Concepts.Take(3).Select(x => $"{x.Term}: {x.Definition}").ToList();
        ruleChoices.Insert(1, lesson.KeyRule);
        questions.Add(new QuizQuestionSeed(
            "Which statement expresses the governing relationship or design rule for this lesson?",
            $"The lesson's governing rule is: {lesson.KeyRule}", ruleChoices, 1));

        questions.Add(string.IsNullOrWhiteSpace(lesson.CodeExample)
            ? new QuizQuestionSeed(
                $"In the worked example — {lesson.WorkedExample} — what conclusion is explicitly supported?",
                lesson.ExampleResult,
                [$"The example's result is not established because the stated method was not applied.",
                    $"The result should be interpreted as {lesson.Concepts[0].Definition}", lesson.ExampleResult,
                    $"The example reaches the conclusion without using or checking its given information."], 2)
            : new QuizQuestionSeed(
                $"Trace this C++ example. {lesson.CodeExample}\nAssuming the stated inputs are valid, what result is produced?",
                $"The code's result is {lesson.CodeExpectedResult}; trace the declared values and executed statements to confirm it.",
                [lesson.CodeExpectedResult,
                    "The program fails to compile because the shown declarations and operations are incompatible.",
                    "The program enters an unbounded loop or recursive call before producing a result.",
                    "The program fails at runtime before reaching the output statement."], 0));

        questions.Add(new QuizQuestionSeed(
            $"A student is completing the lesson activity: {lesson.Activity} Which submission meets its stated requirements?",
            $"Apply the lesson concepts and provide the evidence requested by the activity: {lesson.Activity}",
            [$"Provide only a final conclusion, leaving out the method and checks requested by the activity.",
                $"Use the required method but omit the assumptions or constraints stated in the activity.",
                $"Provide an answer for a different input case and do not address the requested output.",
                $"Apply the lesson concepts and provide the evidence requested by the activity: {lesson.Activity}"], 3));

        questions.Add(new QuizQuestionSeed(
            $"Which review practice best supports reliable work on “{lesson.Title}”?",
            $"Define the problem about {lesson.Focus}, apply the stated rule, show the method, and check that the result fits the assumptions.",
            ["Treat the first plausible result as correct without checking inputs or assumptions.",
                "Memorize a worked-example answer and reuse it even when the problem conditions change.",
                "Replace the stated method with an unrelated technique and omit the reasoning.",
                $"Define the problem about {lesson.Focus}, apply the stated rule, show the method, and check that the result fits the assumptions."], 3));
        return questions;
    }
}
