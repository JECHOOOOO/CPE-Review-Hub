using System.Text.RegularExpressions;
using CPE_Review_Hub.ViewModels;

namespace CPE_Review_Hub.Services;

public enum LessonSectionKind
{
    Introduction,
    Objectives,
    Discussion,
    Example,
    Terms,
    Method,
    Mistakes,
    Takeaways,
    References,
    General
}

public sealed record LessonReference(string Institution, string Title, string Url, string Details);

public sealed record LessonContentSection(
    string Heading,
    string Id,
    LessonSectionKind Kind,
    IReadOnlyList<string> Paragraphs,
    IReadOnlyList<string> Items,
    IReadOnlyList<LessonReference> References);

/// <summary>
/// Turns the existing plain-text lesson material into display sections. This is
/// deliberately a presentation parser: it does not change, truncate, or persist
/// lesson content, and it tolerates older material with no formal headings.
/// </summary>
public static partial class LessonContentParser
{
    private static readonly HashSet<string> ImageStopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "and", "for", "with", "from", "that", "this", "into", "used", "showing", "figure",
        "lesson", "basic", "example", "diagram", "illustration", "using", "between", "how", "what"
    };

    private static readonly Dictionary<string, (string Heading, LessonSectionKind Kind)> HeadingMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["INTRODUCTION"] = ("Introduction", LessonSectionKind.Introduction),
            ["LEARNING OBJECTIVES"] = ("Learning objectives", LessonSectionKind.Objectives),
            ["LESSON DISCUSSION"] = ("Lesson discussion", LessonSectionKind.Discussion),
            ["IMPORTANT TERMS"] = ("Important terms", LessonSectionKind.Terms),
            ["KEY TERMS"] = ("Important terms", LessonSectionKind.Terms),
            ["KEY RELATIONSHIP OR DESIGN RULE"] = ("Key relationship or design rule", LessonSectionKind.General),
            ["STEP-BY-STEP METHOD"] = ("Step-by-step method", LessonSectionKind.Method),
            ["WORKED OR PRACTICAL EXAMPLE"] = ("Worked or practical example", LessonSectionKind.Example),
            ["COMMON MISTAKES TO AVOID"] = ("Common mistakes", LessonSectionKind.Mistakes),
            ["COMMON MISTAKES"] = ("Common mistakes", LessonSectionKind.Mistakes),
            ["KEY TAKEAWAYS"] = ("Key takeaways", LessonSectionKind.Takeaways),
            ["REFERENCES / FURTHER READING"] = ("References & further reading", LessonSectionKind.References),
            ["REFERENCES"] = ("References & further reading", LessonSectionKind.References)
        };

    public static IReadOnlyList<LessonContentSection> Parse(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return [];

        var paragraphs = content.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n').Split("\n\n", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var draft = new List<(string Heading, LessonSectionKind Kind, List<string> Body)>();
        string currentHeading = "Overview";
        var currentKind = LessonSectionKind.General;
        var body = new List<string>();

        void Flush()
        {
            if (body.Count > 0)
            {
                draft.Add((currentHeading, currentKind, body));
                body = [];
            }
        }

        foreach (var raw in paragraphs)
        {
            var paragraphLines = new List<string>();
            foreach (var sourceLine in raw.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                var line = sourceLine.Trim();
                if (IsMetadata(line)) continue;

                if (HeadingMap.TryGetValue(line, out var known))
                {
                    AddParagraph();
                    Flush();
                    currentHeading = known.Heading;
                    currentKind = known.Kind;
                    continue;
                }

                if (IsDiscussionHeading(line))
                {
                    AddParagraph();
                    Flush();
                    currentHeading = ToTitleCase(line);
                    currentKind = LessonSectionKind.Discussion;
                    continue;
                }

                paragraphLines.Add(line);
            }
            AddParagraph();

            void AddParagraph()
            {
                if (paragraphLines.Count == 0) return;
                body.Add(string.Join('\n', paragraphLines));
                paragraphLines.Clear();
            }
        }
        Flush();

        if (draft.Count == 0) return [];

        var sections = new List<LessonContentSection>();
        var idCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var section in draft)
        {
            var idBase = Slug(section.Heading);
            idCounts.TryGetValue(idBase, out var count);
            idCounts[idBase] = ++count;
            var id = count == 1 ? idBase : $"{idBase}-{count}";
            var rawItems = section.Body.SelectMany(SplitLines).ToList();

            if (section.Kind == LessonSectionKind.Objectives || section.Kind == LessonSectionKind.Takeaways ||
                section.Kind == LessonSectionKind.Mistakes || section.Kind == LessonSectionKind.Method)
            {
                var items = rawItems.Select(CleanListMarker).Where(x => x.Length > 0).ToList();
                sections.Add(new(section.Heading, id, section.Kind, [], items, []));
            }
            else if (section.Kind == LessonSectionKind.Terms)
            {
                sections.Add(new(section.Heading, id, section.Kind, [], rawItems.Select(CleanListMarker).Where(x => x.Length > 0).ToList(), []));
            }
            else if (section.Kind == LessonSectionKind.References)
            {
                sections.Add(new(section.Heading, id, section.Kind, [], [], section.Body.SelectMany(ParseReferences).ToList()));
            }
            else
            {
                sections.Add(new(section.Heading, id, section.Kind, section.Body, [], []));
            }
        }

        // Older lesson records often have a leading prose block before their first
        // recognizable label. Label it as an overview without changing its text.
        if (sections.Count > 1 && sections[0].Kind == LessonSectionKind.General && sections[0].Heading == "Overview")
            sections[0] = sections[0] with { Heading = "Overview", Id = "overview" };
        return sections;
    }

    public static int MatchImageSection(StudentLessonImageViewModel image,
        IReadOnlyList<LessonContentSection> sections)
    {
        if (sections.Count == 0) return -1;
        var imageWords = Words($"{image.AltText} {image.Caption}");
        var bestIndex = -1;
        var bestScore = 0;
        for (var index = 0; index < sections.Count; index++)
        {
            var section = sections[index];
            var sectionWords = Words($"{section.Heading} {string.Join(' ', section.Paragraphs)} {string.Join(' ', section.Items)}");
            var score = imageWords.Count(sectionWords.Contains);
            if (score > bestScore)
            {
                bestScore = score;
                bestIndex = index;
            }
        }

        if (bestIndex >= 0) return bestIndex;
        var discussion = sections.ToList().FindIndex(x => x.Kind == LessonSectionKind.Discussion);
        return discussion >= 0 ? discussion : 0;
    }

    private static HashSet<string> Words(string value) => Regex.Matches(value.ToLowerInvariant(), "[a-z0-9]{3,}")
        .Select(match => match.Value).Where(word => !ImageStopWords.Contains(word)).ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static IEnumerable<string> SplitLines(string value) => value
        .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    private static string CleanListMarker(string value) => value.Trim().TrimStart('•', '✓', '-', '*').Trim();

    private static bool IsMetadata(string line) =>
        Regex.IsMatch(line, @"^CPE\s+\d{3}\s+—", RegexOptions.IgnoreCase) ||
        Regex.IsMatch(line, @"^LESSON\s+\d+\s*:", RegexOptions.IgnoreCase) ||
        line.StartsWith("Complete the practice activity before", StringComparison.OrdinalIgnoreCase);

    private static bool IsDiscussionHeading(string value) =>
        value.Length is > 2 and < 100 && value == value.ToUpperInvariant() &&
        !value.Contains("HTTP", StringComparison.OrdinalIgnoreCase) &&
        Regex.IsMatch(value, "[A-Z]") && !value.StartsWith("•", StringComparison.Ordinal);

    private static string ToTitleCase(string value) =>
        string.Join(' ', value.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(word => word.Length <= 3 ? word : char.ToUpperInvariant(word[0]) + word[1..].ToLowerInvariant()));

    private static string Slug(string value)
    {
        var slug = Regex.Replace(value.ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? "lesson-section" : slug;
    }

    private static IEnumerable<LessonReference> ParseReferences(string paragraph)
    {
        foreach (var raw in SplitLines(paragraph))
        {
            var line = CleanListMarker(raw);
            var match = ReferenceUrlRegex().Match(line);
            if (!match.Success) continue;

            var url = match.Value.TrimEnd('.', ',', ')', ';');
            var before = line[..match.Index].Trim().TrimEnd('.');
            var details = line[(match.Index + match.Length)..].Trim().TrimStart('(', ' ', ';');
            var verifiedAt = details.IndexOf("(verified", StringComparison.OrdinalIgnoreCase);
            if (verifiedAt >= 0) details = details[..verifiedAt].Trim();
            var parts = before.Split('.', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            var title = parts.Length > 0 ? parts[0] : "Further reading";
            var institution = parts.Length > 1 ? parts[1] : "Academic resource";
            yield return new(institution, title, url, details.TrimEnd(')', '.', ';'));
        }
    }

    [GeneratedRegex("https://[^\\s)]+", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ReferenceUrlRegex();
}
