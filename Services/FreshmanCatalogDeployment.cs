using System.Data;
using System.Data.Common;
using CPE_Review_Hub.Data;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

namespace CPE_Review_Hub.Services;

/// <summary>
/// Applies only the reviewed additive Freshman migration. EF's MigrateAsync
/// attempts CREATE TABLE for the existing history table, which the restricted
/// production account cannot do.
/// </summary>
public static class FreshmanCatalogDeployment
{
    private const string MigrationId = "20260927123952_FreshmanLevelAndCourseMetadata";
    private const string LockName = "colms_freshman_catalog_20260927";

    public static async Task ApplyAsync(WebApplication app)
    {
        if (!app.Environment.IsProduction()) return;
        var stage = "connect";

        try
        {
            await using var scope = app.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            await db.Database.OpenConnectionAsync();
            var connection = db.Database.GetDbConnection();
            var lockAcquired = false;
            try
            {
                await using (var command = connection.CreateCommand())
                {
                    stage = "lock";
                    command.CommandText = $"SELECT GET_LOCK('{LockName}', 30)";
                    lockAcquired = Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
                }
                if (!lockAcquired)
                    throw new InvalidOperationException("The catalog migration lock was unavailable.");

                var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                stage = "inspect-subjects";
                await using (var inspect = connection.CreateCommand())
                {
                    inspect.CommandText = "SELECT COLUMN_NAME FROM information_schema.COLUMNS " +
                        "WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'Subjects'";
                    await using var reader = await inspect.ExecuteReaderAsync();
                    while (await reader.ReadAsync()) columns.Add(reader.GetString(0));
                }
                if (!columns.Contains("Id") || !columns.Contains("Code") || !columns.Contains("AcademicLevelId"))
                    throw new InvalidOperationException("Existing Subjects schema requires manual review.");

                if (!columns.Contains("Semester"))
                {
                    stage = "add-semester";
                    await ExecuteAsync(connection, "ALTER TABLE `Subjects` ADD COLUMN `Semester` int NULL");
                }
                if (!columns.Contains("Units"))
                {
                    stage = "add-units";
                    await ExecuteAsync(connection, "ALTER TABLE `Subjects` ADD COLUMN `Units` decimal(4,1) NULL");
                }

                stage = "freshman-level";
                await ExecuteAsync(connection,
                    "INSERT INTO `AcademicLevels` (`Id`, `Name`) VALUES (100, 'Freshman') " +
                    "ON DUPLICATE KEY UPDATE `Name` = 'Freshman'");

                var courses = new (string Code, string Title, decimal Units, int Semester)[]
                {
                    ("CPE 101", "Introduction to Computing", 3.0m, 1),
                    ("CPE 102", "Computer Programming 1", 3.0m, 1),
                    ("CPE 103", "Computer Programming 2", 3.0m, 2),
                    ("CPE 104", "Discrete Mathematics", 3.0m, 2)
                };
                foreach (var course in courses)
                {
                    stage = "course-" + course.Code;
                    await using var insert = connection.CreateCommand();
                    insert.CommandText = "INSERT INTO `Subjects` " +
                        "(`AcademicLevelId`, `Code`, `Title`, `Description`, `Units`, `Semester`, `IsPublished`, `CreatedAtUtc`, `UpdatedAtUtc`) " +
                        "SELECT 100, @code, @title, '', @units, @semester, 1, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6) " +
                        "FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM `Subjects` WHERE `Code` = @code)";
                    AddParameter(insert, "@code", DbType.String, course.Code);
                    AddParameter(insert, "@title", DbType.String, course.Title);
                    AddParameter(insert, "@units", DbType.Decimal, course.Units);
                    AddParameter(insert, "@semester", DbType.Int32, course.Semester);
                    await insert.ExecuteNonQueryAsync();

                    await using var update = connection.CreateCommand();
                    update.CommandText = "UPDATE `Subjects` SET `AcademicLevelId` = 100, `Title` = @title, " +
                        "`Units` = @units, `Semester` = @semester WHERE `Code` = @code";
                    AddParameter(update, "@code", DbType.String, course.Code);
                    AddParameter(update, "@title", DbType.String, course.Title);
                    AddParameter(update, "@units", DbType.Decimal, course.Units);
                    AddParameter(update, "@semester", DbType.Int32, course.Semester);
                    await update.ExecuteNonQueryAsync();
                }

                await using (var history = connection.CreateCommand())
                {
                    stage = "migration-history";
                    history.CommandText = "INSERT INTO `__EFMigrationsHistory` (`MigrationId`, `ProductVersion`) " +
                        "SELECT @migration, @version WHERE NOT EXISTS " +
                        "(SELECT 1 FROM `__EFMigrationsHistory` WHERE `MigrationId` = @migration)";
                    AddParameter(history, "@migration", DbType.String, MigrationId);
                    AddParameter(history, "@version", DbType.String, "10.0.12");
                    await history.ExecuteNonQueryAsync();
                }
                app.Logger.LogInformation("Freshman catalog schema and four course records are up to date.");
            }
            finally
            {
                if (lockAcquired)
                {
                    await using var release = connection.CreateCommand();
                    release.CommandText = $"SELECT RELEASE_LOCK('{LockName}')";
                    await release.ExecuteScalarAsync();
                }
                await db.Database.CloseConnectionAsync();
            }
        }
        catch (Exception exception)
        {
            app.Logger.LogError("Freshman catalog migration did not complete at {Stage} ({ErrorType}, MySqlCode {MySqlCode}).",
                stage, exception.GetType().Name, (exception as MySqlException)?.Number);
        }
    }

    private static async Task ExecuteAsync(DbConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private static void AddParameter(DbCommand command, string name, DbType type, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = type;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
