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
    IReadOnlyList<CurriculumConcept> Concepts)
{
    public IReadOnlyList<string> LearningObjectives { get; init; } = [];
    public IReadOnlyList<CurriculumSection> StudySections { get; init; } = [];
    public IReadOnlyList<CurriculumReference> References { get; init; } = [];
    public Models.ActivityResponseType? PracticeType { get; init; }
    public string ActivityPurpose { get; init; } = "";
    public string ActivityTask { get; init; } = "";
    public string ActivityExpectedOutput { get; init; } = "";
    public string ActivityOptions { get; init; } = "";
    public string CodeExample { get; init; } = "";
    public string CodeExpectedResult { get; init; } = "";
}

public sealed record CurriculumConcept(string Term, string Definition);
public sealed record CurriculumSection(string Heading, string Content);
public sealed record CurriculumReference(string SourceTitle, string Institution, string SourceUrl,
    string TopicsSupported, string VerifiedDate = "2026-09-30");

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
