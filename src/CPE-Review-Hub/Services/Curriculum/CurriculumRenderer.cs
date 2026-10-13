using System.Text;

namespace CPE_Review_Hub.Services.Curriculum;

public static class CurriculumRenderer
{
    public static string RenderLesson(string courseCode, string courseTitle, int lessonNumber,
        CurriculumLesson lesson)
    {
        var steps = lesson.Method.Split('>', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var text = new StringBuilder();
        text.AppendLine($"{courseCode} — {courseTitle}");
        text.AppendLine($"LESSON {lessonNumber}: {lesson.Title}");
        text.AppendLine();
        text.AppendLine("LEARNING OBJECTIVES");
        foreach (var objective in lesson.LearningObjectives)
            text.AppendLine($"• {objective}");
        text.AppendLine();
        text.AppendLine("INTRODUCTION");
        text.AppendLine($"This lesson studies {lesson.Focus}. It is part of {courseTitle} because the concepts help Computer Engineering students explain a system, justify a method, and evaluate the consequences of a result. Read the topic together with its assumptions and examples: technical procedures are reliable only when the problem is stated clearly and the limits of the method are understood.");
        text.AppendLine();
        text.AppendLine("LESSON DISCUSSION");
        foreach (var section in lesson.StudySections)
        {
            text.AppendLine(section.Heading.ToUpperInvariant());
            text.AppendLine(section.Content);
            text.AppendLine();
        }
        if (!string.IsNullOrWhiteSpace(lesson.CodeExample))
        {
            text.AppendLine("C++ WORKED EXAMPLE");
            text.AppendLine(lesson.CodeExample);
            text.AppendLine($"Expected result: {lesson.CodeExpectedResult}");
            text.AppendLine("Trace the values and control flow before compiling. This example illustrates the stated lesson point; it is not a substitute for validating user input or testing boundary cases in a complete program.");
            text.AppendLine();
        }
        text.AppendLine("IMPORTANT TERMS");
        foreach (var concept in lesson.Concepts)
            text.AppendLine($"• {concept.Term}: {concept.Definition}");
        text.AppendLine();
        text.AppendLine("KEY RELATIONSHIP OR DESIGN RULE");
        text.AppendLine(lesson.KeyRule);
        text.AppendLine("State all assumptions before using this rule. Keep symbols, units, logic levels, data types, or evaluation criteria consistent with the problem. A result is credible only when it agrees with the stated conditions and passes a reasonableness check.");
        text.AppendLine();
        text.AppendLine("STEP-BY-STEP METHOD");
        for (var i = 0; i < steps.Length; i++) text.AppendLine($"{i + 1}. {steps[i]}");
        text.AppendLine();
        text.AppendLine("WORKED OR PRACTICAL EXAMPLE");
        text.AppendLine(lesson.WorkedExample);
        text.AppendLine($"Result and interpretation: {lesson.ExampleResult}");
        text.AppendLine("Check the result against the key relationship above. If an assumption, input, or constraint changes, repeat the method instead of reusing the old conclusion without analysis.");
        text.AppendLine();
        text.AppendLine("COMMON MISTAKES TO AVOID");
        text.AppendLine(CommonMistakes(courseCode, lesson));
        text.AppendLine();
        text.AppendLine("KEY TAKEAWAYS");
        text.AppendLine($"• The lesson centers on {lesson.Focus}.");
        text.AppendLine($"• Remember and apply: {lesson.KeyRule}");
        text.AppendLine($"• The terms {string.Join(", ", lesson.Concepts.Select(x => x.Term))} describe different parts of the same problem and should not be treated as interchangeable.");
        text.AppendLine("• Good engineering work shows the method, validates the result, and explains what the result means.");
        text.AppendLine();
        text.AppendLine("REFERENCES / FURTHER READING");
        foreach (var reference in lesson.References)
            text.AppendLine($"• {reference.SourceTitle}. {reference.Institution}. {reference.SourceUrl} (verified {reference.VerifiedDate}; supports: {reference.TopicsSupported})");
        text.AppendLine();
        text.AppendLine("Complete the practice activity before opening the 10-item quiz.");
        return text.ToString();
    }

    public static string RenderActivity(int lessonNumber, CurriculumLesson lesson) =>
        string.IsNullOrWhiteSpace(lesson.ActivityPurpose) ? lesson.Activity :
        $"Purpose: {lesson.ActivityPurpose}\n\n{lesson.Activity}";

    public static Models.ActivityResponseType ActivityType(int lessonNumber, CurriculumLesson lesson)
    {
        if (lesson.PracticeType.HasValue) return lesson.PracticeType.Value;
        var text = $"{lesson.Activity} {lesson.Focus}".ToLowerInvariant();
        if (new[] { "implement", "code", "program", "algorithm" }.Any(text.Contains)) return Models.ActivityResponseType.Coding;
        if (new[] { "calculate", "compute", "solve", "equation", "current", "voltage", "cost" }.Any(text.Contains)) return Models.ActivityResponseType.Calculation;
        return lessonNumber == 1 ? Models.ActivityResponseType.Identification : Models.ActivityResponseType.ShortAnswer;
    }

    public static string ActivityOptions(int lessonNumber, CurriculumLesson lesson) => lesson.ActivityOptions;

    public static string ActivityExpectedOutput(Models.ActivityResponseType type, CurriculumLesson? lesson = null) =>
        string.IsNullOrWhiteSpace(lesson?.ActivityExpectedOutput) ? type switch
    {
        Models.ActivityResponseType.Matching => "Complete every match.",
        Models.ActivityResponseType.MultipleChoice => "Select one answer.",
        Models.ActivityResponseType.Calculation => "Enter your calculation and final answer.",
        Models.ActivityResponseType.Coding => "Enter your code or attach the source file.",
        Models.ActivityResponseType.Identification => "Enter the requested term or identification.",
        _ => "Enter a concise response to the task."
    } : lesson.ActivityExpectedOutput;

    private static string CommonMistakes(string courseCode, CurriculumLesson lesson) => courseCode switch
    {
        "CPE 102" or "CPE 103" => "Do not assume that successful compilation proves the algorithm is correct. Trace a small input, check types and boundary conditions, and use the C++ language rules taught in this lesson. Avoid silently accepting invalid input or relying on undefined behavior.",
        "CPE 104" => "Do not substitute examples for proof, omit the domain or universe, or use a formula before checking its assumptions. Keep notation consistent and verify a result against the definitions.",
        _ => $"Do not confuse related components or concepts in {lesson.Title}. State assumptions, distinguish observed evidence from interpretation, and verify the conclusion using the conditions in the problem."
    };
}
