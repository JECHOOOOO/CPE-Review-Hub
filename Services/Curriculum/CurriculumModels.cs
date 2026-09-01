namespace CPE_Review_Hub.Services.Curriculum;

public sealed record CurriculumCourse(string Code, IReadOnlyList<CurriculumLesson> Lessons);

public sealed record CurriculumLesson(
    string Title,
    string Focus,
    string KeyRule,
    string Method,
    string WorkedExample,
    string ExampleResult,
    string Activity,
    IReadOnlyList<CurriculumConcept> Concepts);

public sealed record CurriculumConcept(string Term, string Definition);

public static class CurriculumSeedFactory
{
    public static CurriculumLesson Lesson(string title, string focus, string keyRule,
        string method, string workedExample, string exampleResult, string activity,
        params string[] conceptPairs)
    {
        if (conceptPairs.Length < 5 || conceptPairs.Any(x => !x.Contains('|')))
            throw new InvalidOperationException($"Curriculum lesson '{title}' needs at least five term|definition pairs.");
        return new CurriculumLesson(title, focus, keyRule, method, workedExample, exampleResult,
            activity, conceptPairs.Select(pair =>
            {
                var parts = pair.Split('|', 2, StringSplitOptions.TrimEntries);
                return new CurriculumConcept(parts[0], parts[1]);
            }).ToList());
    }
}
