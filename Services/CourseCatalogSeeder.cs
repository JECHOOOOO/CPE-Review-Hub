using CPE_Review_Hub.Data;
using CPE_Review_Hub.Models;
using CPE_Review_Hub.Services.Curriculum;
using Microsoft.EntityFrameworkCore;

namespace CPE_Review_Hub.Services;

public static class CourseCatalogSeeder
{
    private static readonly CourseDefinition[] Courses =
    [
        new(200, "CPE 211", "Data Structures And Algorithms"),
        new(200, "CPE 212", "Engineering Economics"),
        new(200, "CPE 213", "Fundamentals Of Electrical Circuits"),
        new(200, "CPE 214", "Environmental Science And Engineering"),
        new(200, "CPE 215", "Computer Aided Drafting"),
        new(200, "CPE 216", "Engineering Drawing 1"),
        new(200, "MT 211", "Differential Equations"),
        new(200, "MT 221", "Numerical Method"),
        new(200, "CPE 221", "Software Design"),
        new(200, "CPE 222", "Fundamentals of Electronics Circuits"),
        new(200, "CPE 223", "Engineering Drawing 2"),

        new(300, "CPE 311", "Logic Circuits And Design"),
        new(300, "CPE 312", "Operating System"),
        new(300, "CPE 313", "Data And Digital Communication"),
        new(300, "CPE 314", "Introduction To HDL"),
        new(300, "CPE 315", "Feedback And Control System"),
        new(300, "CPE 316", "Fundamentals Of Mixed Signal And Sensors"),
        new(300, "CPE 317", "Computer Engineering and Drafting Design"),
        new(300, "CPE ELEC 1", "Elective 1"),
        new(300, "CPE 321", "Basic Occupational Health & Safety"),
        new(300, "CPE 322", "Computer Network & Security"),
        new(300, "CPE 323", "Microprocessor"),
        new(300, "CPE 324", "Methods of Research"),
        new(300, "CPE 325", "Technopreneurship"),
        new(300, "CPE 327", "CPE Laws And Professional Practice"),
        new(300, "CPE Elec 2", "System Maintenance and Administration 2"),
        new(300, "CPE 530", "Engineering Ethics"),

        new(400, "CPE 411", "Engineering Management"),
        new(400, "CPE 412", "Embedded Systems"),
        new(400, "CPE 413", "Computer Architecture and Organization"),
        new(400, "CPE 414", "Emerging Technologies in CpE"),
        new(400, "CPE 415", "CpE Practice and Design 1"),
        new(400, "CPE 416", "Digital Signal and Processing"),
        new(400, "CPE ELEC 3", "Software Engineering")
    ];

    public static int CourseCount => Courses.Length;
    public const int LessonsPerCourse = 6;

    public static async Task<CatalogSeedResult> SeedAsync(ApplicationDbContext db)
    {
        var validLevels = await db.AcademicLevels.Select(x => x.Id).ToHashSetAsync();
        if (!new[] { 200, 300, 400 }.All(validLevels.Contains))
            throw new InvalidOperationException("Academic levels 200, 300, and 400 must exist before seeding courses.");

        var existing = await db.Subjects
            .AsSplitQuery()
            .Include(x => x.Assessments).ThenInclude(x => x.AssessmentQuestions)
                .ThenInclude(x => x.Question).ThenInclude(x => x.Choices)
            .Include(x => x.Lessons).ThenInclude(x => x.ReviewMaterials)
            .Include(x => x.Lessons).ThenInclude(x => x.Images)
            .Include(x => x.Lessons).ThenInclude(x => x.Activity)
            .Include(x => x.Lessons).ThenInclude(x => x.Assessments)
                .ThenInclude(x => x.AssessmentQuestions).ThenInclude(x => x.Question).ThenInclude(x => x.Choices)
            .Include(x => x.Lessons).ThenInclude(x => x.Questions).ThenInclude(x => x.Choices)
            .ToListAsync();

        var addedCourses = 0;
        var addedLessons = 0;
        var addedMaterials = 0;
        var addedActivities = 0;
        var addedAssessments = 0;
        var addedQuestions = 0;
        var addedChoices = 0;
        var addedMappings = 0;
        var now = DateTime.UtcNow;

        foreach (var definition in Courses)
        {
            var subject = existing.FirstOrDefault(x =>
                x.Code.Equals(definition.Code, StringComparison.OrdinalIgnoreCase));
            if (subject is null)
            {
                subject = new Subject { Code = definition.Code, CreatedAtUtc = now };
                db.Subjects.Add(subject);
                existing.Add(subject);
                addedCourses++;
            }
            subject.Code = definition.Code;
            subject.Title = definition.Title;
            subject.AcademicLevelId = definition.Level;
            subject.Description = $"A structured six-lesson review course for {definition.Title}. Complete each lesson activity before attempting its 10-item quiz, then prepare for the 50-item course examination.";
            subject.IsPublished = true;
            subject.UpdatedAtUtc = now;
            var curriculum = CurriculumCatalog.Get(definition.Code);
            if (curriculum.Lessons.Count != LessonsPerCourse)
                throw new InvalidOperationException($"{definition.Code} must define exactly six curriculum lessons.");
            var courseQuestions = new List<List<Question>>();

            for (var index = 0; index < curriculum.Lessons.Count; index++)
            {
                var number = index + 1;
                var template = curriculum.Lessons[index];
                var lesson = subject.Lessons.FirstOrDefault(x => x.SortOrder == number);
                if (lesson is null)
                {
                    lesson = new Lesson
                    {
                        Subject = subject, SortOrder = number, CreatedAtUtc = now
                    };
                    subject.Lessons.Add(lesson);
                    addedLessons++;
                }
                lesson.Title = template.Title;
                lesson.Summary = $"Study {template.Focus}, apply the governing relationship, complete a practical activity, and verify understanding in a 10-item quiz.";
                lesson.IsPublished = true;
                lesson.UpdatedAtUtc = now;

                var material = lesson.ReviewMaterials.OrderBy(x => x.SortOrder).FirstOrDefault();
                if (material is null)
                {
                    material = new ReviewMaterial
                    {
                        Lesson = lesson,
                        SortOrder = 1,
                        CreatedAtUtc = now
                    };
                    lesson.ReviewMaterials.Add(material);
                    addedMaterials++;
                }
                material.Title = $"Lesson {number} · {template.Title}";
                material.Content = CurriculumRenderer.RenderLesson(definition.Code, definition.Title, number, template);
                material.IsPublished = true;
                material.UpdatedAtUtc = now;

                if (lesson.Activity is null)
                {
                    lesson.Activity = new LessonActivity
                    {
                        Lesson = lesson
                    };
                    addedActivities++;
                }
                var activityType = CurriculumRenderer.ActivityType(number, template);
                lesson.Activity.Title = $"Quick practice: {template.Title}";
                lesson.Activity.Instructions = CurriculumRenderer.RenderActivity(number, template);
                lesson.Activity.ResponseType = activityType;
                lesson.Activity.Options = CurriculumRenderer.ActivityOptions(number, template);
                lesson.Activity.ExpectedOutput = CurriculumRenderer.ActivityExpectedOutput(activityType);
                lesson.Activity.IsPublished = true;

                var imageCode = NormalizeCode(definition.Code).ToLowerInvariant();
                var seededPathPrefix = $"/images/lessons/{imageCode}/lesson-";
                var oldSeededImages = lesson.Images.Where(x =>
                    x.ImagePath.StartsWith(seededPathPrefix, StringComparison.OrdinalIgnoreCase)).ToList();
                if (LessonVisualManifest.TryGet(definition.Code, template.Title, out var visual))
                {
                    var image = oldSeededImages.FirstOrDefault(x =>
                        x.ImagePath.Equals(visual.ImagePath, StringComparison.OrdinalIgnoreCase));
                    if (image is null)
                    {
                        image = new LessonImage { Lesson = lesson, DisplayOrder = 1 };
                        lesson.Images.Add(image);
                    }
                    image.ImagePath = visual.ImagePath;
                    image.AltText = visual.AltText;
                    image.Caption = visual.Caption;
                    image.IsPublished = true;
                    foreach (var stale in oldSeededImages.Where(x => x != image)) db.LessonImages.Remove(stale);
                }
                else
                {
                    foreach (var stale in oldSeededImages) db.LessonImages.Remove(stale);
                }

                var quiz = lesson.Assessments.FirstOrDefault(x => x.Type == AssessmentType.LessonQuiz);
                if (quiz is null)
                {
                    quiz = new Assessment
                    {
                        Lesson = lesson,
                        Subject = subject,
                        Type = AssessmentType.LessonQuiz
                    };
                    lesson.Assessments.Add(quiz);
                    addedAssessments++;
                }
                quiz.Title = $"{definition.Code} · Lesson {number} Quiz";
                quiz.RequiredQuestionCount = 10;
                quiz.DurationMinutes = 15;
                quiz.PassingPercentage = 70;

                var seededQuestions = new List<Question>();
                var questionSeeds = CurriculumQuizBuilder.Build(template);
                for (var questionIndex = 0; questionIndex < questionSeeds.Count; questionIndex++)
                {
                    var sourceKey = $"{NormalizeCode(definition.Code)}-L{number}-Q{questionIndex + 1}";
                    var question = lesson.Questions.FirstOrDefault(x => x.SourceKey == sourceKey);
                    if (question is null)
                    {
                        var seed = questionSeeds[questionIndex];
                        question = new Question
                        {
                            SourceKey = sourceKey, Subject = subject, Lesson = lesson,
                            Prompt = seed.Prompt, Explanation = seed.Explanation,
                            IsActive = true, CreatedAtUtc = now
                        };
                        for (var choiceIndex = 0; choiceIndex < seed.Choices.Count; choiceIndex++)
                        {
                            question.Choices.Add(new Choice
                            {
                                Text = seed.Choices[choiceIndex], SortOrder = choiceIndex + 1,
                                IsCorrect = choiceIndex == seed.CorrectChoiceIndex
                            });
                            addedChoices++;
                        }
                        lesson.Questions.Add(question);
                        addedQuestions++;
                    }
                    seededQuestions.Add(question);
                    if (!quiz.AssessmentQuestions.Any(x => x.Question.SourceKey == sourceKey))
                    {
                        quiz.AssessmentQuestions.Add(new AssessmentQuestion
                        {
                            Assessment = quiz, Question = question, SortOrder = questionIndex + 1
                        });
                        addedMappings++;
                    }
                }
                quiz.IsPublished = seededQuestions.Count == 10 && seededQuestions.All(x => x.Choices.Count == 4);
                courseQuestions.Add(seededQuestions);
            }

            var finalExam = subject.Assessments.FirstOrDefault(x =>
                x.Type == AssessmentType.FinalExam && x.LessonId is null);
            if (finalExam is null)
            {
                finalExam = new Assessment
                {
                    Subject = subject,
                    Type = AssessmentType.FinalExam
                };
                subject.Assessments.Add(finalExam);
                addedAssessments++;
            }
            finalExam.Title = $"{definition.Code} · Final Examination";
            finalExam.RequiredQuestionCount = 50;
            finalExam.DurationMinutes = 90;
            finalExam.PassingPercentage = 70;
            var finalQuestions = courseQuestions.SelectMany(x => x.Take(8))
                .Concat(courseQuestions.Take(2).Select(x => x[8])).ToList();
            for (var i = 0; i < finalQuestions.Count; i++)
            {
                var question = finalQuestions[i];
                if (!finalExam.AssessmentQuestions.Any(x => x.Question.SourceKey == question.SourceKey))
                {
                    finalExam.AssessmentQuestions.Add(new AssessmentQuestion
                    {
                        Assessment = finalExam, Question = question, SortOrder = i + 1
                    });
                    addedMappings++;
                }
            }
            finalExam.IsPublished = finalQuestions.Count == 50;
        }

        await db.SaveChangesAsync();
        return new CatalogSeedResult(addedCourses, addedLessons, addedMaterials, addedActivities,
            addedAssessments, addedQuestions, addedChoices, addedMappings);
    }

    private static string NormalizeCode(string code) => new(code.Where(char.IsLetterOrDigit)
        .Select(char.ToUpperInvariant).ToArray());

    private sealed record CourseDefinition(int Level, string Code, string Title);
}

public record CatalogSeedResult(int CoursesAdded, int LessonsAdded, int MaterialsAdded,
    int ActivitiesAdded, int AssessmentsAdded, int QuestionsAdded, int ChoicesAdded, int MappingsAdded);
