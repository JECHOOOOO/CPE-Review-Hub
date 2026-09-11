using CPE_Review_Hub.Data;
using CPE_Review_Hub.Models;
using CPE_Review_Hub.Services.Curriculum;
using Microsoft.EntityFrameworkCore;
using System.Data;

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
            await RepairAsync(db, app.Logger);
        }
        catch (Exception exception)
        {
            app.Logger.LogError(exception, "Unable to apply the reviewed online lesson-image update.");
        }
    }

    public static async Task RepairAsync(ApplicationDbContext db, ILogger logger)
    {
        await EnsureAdditiveSchemaAsync(db, logger);
        await ReplaceBundledReferencesOnceAsync(db);
    }

    private static async Task EnsureAdditiveSchemaAsync(ApplicationDbContext db, ILogger logger)
    {
        var connection = db.Database.GetDbConnection();
        await db.Database.OpenConnectionAsync();
        try
        {
            // Connectivity is verified independently before any schema statement.
            await using (var ping = connection.CreateCommand())
            {
                ping.CommandText = "SELECT 1";
                await ping.ExecuteScalarAsync();
            }

            var existing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using (var inspect = connection.CreateCommand())
            {
                inspect.CommandText = "SELECT COLUMN_NAME FROM information_schema.COLUMNS " +
                    "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'LessonImages'";
                await using var reader = await inspect.ExecuteReaderAsync();
                while (await reader.ReadAsync()) existing.Add(reader.GetString(0));
            }
            if (existing.Count == 0)
                throw new InvalidOperationException("The existing LessonImages table was not found; no schema change was attempted.");

            var additiveColumns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Author"] = "ALTER TABLE `LessonImages` ADD COLUMN `Author` varchar(200) NOT NULL DEFAULT ''",
                ["License"] = "ALTER TABLE `LessonImages` ADD COLUMN `License` varchar(200) NOT NULL DEFAULT ''",
                ["SourceName"] = "ALTER TABLE `LessonImages` ADD COLUMN `SourceName` varchar(120) NOT NULL DEFAULT ''",
                ["SourceUrl"] = "ALTER TABLE `LessonImages` ADD COLUMN `SourceUrl` varchar(500) NOT NULL DEFAULT ''"
            };
            foreach (var column in additiveColumns.Where(x => !existing.Contains(x.Key)))
            {
                await using var add = connection.CreateCommand();
                add.CommandText = column.Value;
                await add.ExecuteNonQueryAsync();
                logger.LogInformation("Added reviewed LessonImages metadata column {ColumnName}.", column.Key);
            }

            await using var history = connection.CreateCommand();
            history.CommandText = "INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`) " +
                "SELECT @migration, @version WHERE NOT EXISTS " +
                "(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = @migration)";
            var migration = history.CreateParameter();
            migration.ParameterName = "@migration";
            migration.DbType = DbType.String;
            migration.Value = MigrationId;
            history.Parameters.Add(migration);
            var version = history.CreateParameter();
            version.ParameterName = "@version";
            version.DbType = DbType.String;
            version.Value = "10.0.12";
            history.Parameters.Add(version);
            await history.ExecuteNonQueryAsync();
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
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
