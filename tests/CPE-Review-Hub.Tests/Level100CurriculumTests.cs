using CPE_Review_Hub.Services.Curriculum;
using Xunit;

namespace CPE_Review_Hub.Tests;

public sealed class Level100CurriculumTests
{
    [Theory]
    [InlineData("CPE 101", "Computing Systems and Computational Thinking")]
    [InlineData("CPE 102", "Algorithms, Pseudocode, and Your First C++ Program")]
    [InlineData("CPE 103", "Multidimensional Arrays and Text Data")]
    [InlineData("CPE 104", "Propositions, Logical Connectives, and Truth Tables")]
    public void First_year_courses_have_six_course_specific_lessons(string code, string firstLesson)
    {
        var course = CurriculumCatalog.Get(code);

        Assert.Equal(6, course.Lessons.Count);
        Assert.Equal(firstLesson, course.Lessons[0].Title);
        Assert.Equal(6, course.Lessons.Select(x => x.Title).Distinct(StringComparer.Ordinal).Count());
        Assert.All(course.Lessons, lesson =>
        {
            Assert.Equal(5, lesson.Concepts.Count);
            Assert.All(lesson.Concepts, concept =>
            {
                Assert.False(string.IsNullOrWhiteSpace(concept.Term));
                Assert.False(string.IsNullOrWhiteSpace(concept.Definition));
            });
            Assert.False(string.IsNullOrWhiteSpace(lesson.Activity));
            Assert.False(string.IsNullOrWhiteSpace(lesson.WorkedExample));
            Assert.False(string.IsNullOrWhiteSpace(lesson.ExampleResult));
            Assert.InRange(lesson.LearningObjectives.Count, 4, 7);
            Assert.True(lesson.StudySections.Count >= 4);
            Assert.NotEmpty(lesson.References);
            Assert.NotNull(lesson.PracticeType);
            Assert.False(string.IsNullOrWhiteSpace(lesson.ActivityPurpose));
            Assert.False(string.IsNullOrWhiteSpace(lesson.ActivityTask));
            var rendered = CurriculumRenderer.RenderLesson(course.Code, course.Code, course.Lessons.ToList().IndexOf(lesson) + 1, lesson);
            Assert.Contains("REFERENCES / FURTHER READING", rendered);
            Assert.Contains("COMMON MISTAKES TO AVOID", rendered);
            var wordCount = rendered.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            Assert.True(wordCount >= 750,
                $"{course.Code}: {lesson.Title} has {wordCount} words; minimum draft length is 750.");
        });
    }

    [Fact]
    public void Every_first_year_lesson_builds_a_complete_ten_question_quiz()
    {
        var firstYear = new[] { "CPE 101", "CPE 102", "CPE 103", "CPE 104" };

        foreach (var lesson in firstYear.SelectMany(code => CurriculumCatalog.Get(code).Lessons))
        {
            var questions = CurriculumQuizBuilder.Build(lesson);

            Assert.Equal(10, questions.Count);
            Assert.All(questions, question =>
            {
                Assert.Equal(4, question.Choices.Count);
                Assert.Equal(4, question.Choices.Distinct(StringComparer.Ordinal).Count());
                Assert.InRange(question.CorrectChoiceIndex, 0, 3);
                Assert.False(string.IsNullOrWhiteSpace(question.Explanation));
            });
        }
    }

    [Fact]
    public void Programming_lessons_include_traceable_cpp_examples_and_code_quiz_items()
    {
        foreach (var code in new[] { "CPE 102", "CPE 103" })
        {
            foreach (var lesson in CurriculumCatalog.Get(code).Lessons)
            {
                Assert.Contains("int main()", lesson.CodeExample);
                Assert.False(string.IsNullOrWhiteSpace(lesson.CodeExpectedResult));
                var codeQuestion = CurriculumQuizBuilder.Build(lesson)[7];
                Assert.Contains("C++ example", codeQuestion.Prompt);
                Assert.Contains(lesson.CodeExample, codeQuestion.Prompt);
                Assert.Contains(lesson.CodeExpectedResult, codeQuestion.Explanation);
                Assert.Equal(1, codeQuestion.Choices.Count(x => x == lesson.CodeExpectedResult));
            }
        }
    }

    [Fact]
    public void Freshman_online_visuals_use_distinct_https_media_and_attributed_source_pages()
    {
        var freshman = OnlineLessonVisualManifest.All
            .Where(x => new[] { "CPE 101", "CPE 102", "CPE 103", "CPE 104" }.Contains(x.CourseCode))
            .ToList();

        Assert.Equal(8, freshman.Count);
        Assert.Equal(8, freshman.Select(x => x.Visual.ImagePath).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(freshman, item =>
        {
            Assert.StartsWith("https://", item.Visual.ImagePath, StringComparison.OrdinalIgnoreCase);
            Assert.StartsWith("https://", item.Visual.SourceUrl, StringComparison.OrdinalIgnoreCase);
            Assert.False(string.IsNullOrWhiteSpace(item.Visual.License));
            Assert.False(string.IsNullOrWhiteSpace(item.Visual.AltText));
            Assert.False(string.IsNullOrWhiteSpace(item.Visual.Caption));
        });
    }
}
