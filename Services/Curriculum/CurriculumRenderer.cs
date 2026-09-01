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
        text.AppendLine($"• Explain {string.Join(", ", lesson.Concepts.Take(3).Select(x => x.Term))} in your own words.");
        text.AppendLine($"• Apply this governing idea correctly: {lesson.KeyRule}");
        text.AppendLine("• Follow the lesson method, show intermediate reasoning, and check the result.");
        text.AppendLine($"• Use the concepts to analyze a Computer Engineering example involving {lesson.Focus.ToLowerInvariant()}.");
        text.AppendLine();
        text.AppendLine("INTRODUCTION");
        text.AppendLine($"This lesson studies {lesson.Focus}. The topic matters in {courseTitle} because engineers must connect a technical model to observable behavior, constraints, and a defensible result. The ideas below are presented as a review sequence: define the system, select the appropriate relationship, work carefully, and interpret the answer rather than stopping at a number or isolated fact.");
        text.AppendLine();
        text.AppendLine("MAIN CONCEPTS AND IMPORTANT DEFINITIONS");
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
        text.AppendLine("KEY TAKEAWAYS");
        text.AppendLine($"• The lesson centers on {lesson.Focus}.");
        text.AppendLine($"• Remember and apply: {lesson.KeyRule}");
        text.AppendLine($"• The terms {string.Join(", ", lesson.Concepts.Select(x => x.Term))} describe different parts of the same problem and should not be treated as interchangeable.");
        text.AppendLine("• Good engineering work shows the method, validates the result, and explains what the result means.");
        text.AppendLine("Complete the activity using your own work before opening the 10-item quiz.");
        return text.ToString();
    }

    public static string RenderActivity(string courseCode, int lessonNumber, CurriculumLesson lesson) => $"""
        Apply Lesson {lessonNumber} of {courseCode}: {lesson.Title}.

        {lesson.Activity}

        Required process:
        1. Restate the task and list the given information, assumptions, and required output.
        2. Use at least three lesson concepts by name and apply this rule correctly: {lesson.KeyRule}
        3. Show the main steps, calculations, diagram, code, table, or decision evidence as appropriate.
        4. Check the result and explain one likely error, limitation, safety issue, or design tradeoff.
        5. End with a short conclusion that directly answers the task.
        """;
}
