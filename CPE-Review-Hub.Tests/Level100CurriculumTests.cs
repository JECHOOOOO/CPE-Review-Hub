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
                Assert.InRange(question.CorrectChoiceIndex, 0, 3);
                Assert.False(string.IsNullOrWhiteSpace(question.Explanation));
            });
        }
    }
}
