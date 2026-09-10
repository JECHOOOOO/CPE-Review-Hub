using CPE_Review_Hub.Data;
using CPE_Review_Hub.Models;
using CPE_Review_Hub.Services.Curriculum;
using Microsoft.EntityFrameworkCore;

namespace CPE_Review_Hub.Services;

public static class OnlineLessonImageDeployment
{
    private const string MigrationId = "20260910203500_OnlineLessonImageReferences";

    public static async Task ApplyAsync(WebApplication app)
    {
        if (!app.Environment.IsProduction()) return;
        try
        {
            await using var scope = app.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
            if (pending.Count > 0)
            {
                if (pending.Count != 1 || !pending.Contains(MigrationId, StringComparer.Ordinal))
                {
                    app.Logger.LogCritical("COLMS found unreviewed pending database migrations. Startup migration was not attempted.");
                    return;
                }
                await db.Database.MigrateAsync(MigrationId);
                app.Logger.LogInformation("Applied the reviewed additive lesson-image metadata migration.");
            }

            await ReplaceBundledReferencesOnceAsync(db);
        }
        catch (Exception exception)
        {
            app.Logger.LogError(exception, "Unable to apply the reviewed online lesson-image update.");
        }
    }

    private static async Task ReplaceBundledReferencesOnceAsync(ApplicationDbContext db)
    {
        var courseCodes = OnlineLessonVisualManifest.All.Select(x => x.CourseCode)
            .Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var lessons = await db.Lessons.Include(x => x.Subject).Include(x => x.Images)
            .Where(x => courseCodes.Contains(x.Subject.Code)).ToListAsync();
        var changed = false;

        foreach (var entry in OnlineLessonVisualManifest.All)
        {
            var lesson = lessons.FirstOrDefault(x =>
                x.Subject.Code.Equals(entry.CourseCode, StringComparison.OrdinalIgnoreCase) &&
                x.Title.Equals(entry.LessonTitle, StringComparison.OrdinalIgnoreCase));
            if (lesson is null || lesson.Images.Any(x =>
                    !string.IsNullOrWhiteSpace(x.SourceUrl) &&
                    x.ImagePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))) continue;

            var localPrefix = $"/images/lessons/{NormalizeCode(entry.CourseCode).ToLowerInvariant()}/lesson-";
            var replaceable = lesson.Images.Where(x =>
                x.ImagePath.StartsWith(localPrefix, StringComparison.OrdinalIgnoreCase)).ToList();
            var image = replaceable.FirstOrDefault();
            if (image is null)
            {
                image = new LessonImage { Lesson = lesson, DisplayOrder = 1 };
                lesson.Images.Add(image);
            }
            image.ImagePath = entry.Visual.ImagePath;
            image.SourceUrl = entry.Visual.SourceUrl;
            image.SourceName = entry.Visual.SourceName;
            image.License = entry.Visual.License;
            image.Author = entry.Visual.Author;
            image.AltText = entry.Visual.AltText;
            image.Caption = entry.Visual.Caption;
            image.ImageData = null;
            image.ContentType = null;
            image.IsPublished = true;
            foreach (var duplicate in replaceable.Skip(1)) db.LessonImages.Remove(duplicate);
            changed = true;
        }

        if (changed)
            await db.SaveChangesAsync();
    }

    private static string NormalizeCode(string code) =>
        new(code.Where(char.IsLetterOrDigit).ToArray());
}
