using CPE_Review_Hub.Services;
using CPE_Review_Hub.Services.Curriculum;
using Xunit;

namespace CPE_Review_Hub.Tests;

public sealed class LessonContentParserTests
{
    [Fact]
    public void Freshman_material_becomes_semantic_sections_and_keeps_reference_links()
    {
        var course = CurriculumCatalog.Get("CPE 101");
        var lesson = course.Lessons[0];
        var content = CurriculumRenderer.RenderLesson(course.Code, course.Code, 1, lesson);

        var sections = LessonContentParser.Parse(content);

        Assert.Contains(sections, x => x.Kind == LessonSectionKind.Objectives && x.Items.Count >= 4);
        Assert.Contains(sections, x => x.Kind == LessonSectionKind.Introduction && x.Paragraphs.Count > 0);
        Assert.Contains(sections, x => x.Kind == LessonSectionKind.Discussion && x.Heading != "Lesson discussion");
        Assert.Contains(sections, x => x.Kind == LessonSectionKind.Terms && x.Items.Count >= 4);
        Assert.Contains(sections, x => x.Kind == LessonSectionKind.Method && x.Items.Count >= 3);
        Assert.Contains(sections, x => x.Kind == LessonSectionKind.Mistakes);
        Assert.Contains(sections, x => x.Kind == LessonSectionKind.Takeaways && x.Items.Count >= 3);
        Assert.Contains(sections, x => x.Kind == LessonSectionKind.References && x.References.Count >= 2);
        Assert.DoesNotContain(sections.SelectMany(x => x.Paragraphs), x => x.Contains("LESSON 1:", StringComparison.Ordinal));
    }

    [Fact]
    public void Legacy_unheaded_material_is_preserved_as_readable_overview()
    {
        const string content = "A short legacy paragraph.\n\nA second paragraph remains intact.";

        var sections = LessonContentParser.Parse(content);

        Assert.Single(sections);
        Assert.Equal("A short legacy paragraph.", sections[0].Paragraphs[0]);
        Assert.Equal("A second paragraph remains intact.", sections[0].Paragraphs[1]);
    }
}
