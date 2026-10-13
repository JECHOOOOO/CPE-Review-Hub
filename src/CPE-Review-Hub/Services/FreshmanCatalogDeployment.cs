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

                // The runtime account has DML permission but MySQL error 1142
                // confirms it cannot ALTER Subjects. Do not repeatedly attempt
                // DDL on every cold start; preserve the pending EF migration for
                // a separate privileged maintenance operation.
                stage = "freshman-level";
                await ExecuteAsync(connection,
                    "INSERT INTO `AcademicLevels` (`Id`, `Name`) VALUES (100, 'Freshman') " +
                    "ON DUPLICATE KEY UPDATE `Name` = 'Freshman'");

                var courses = new (string Code, string Title)[]
                {
                    ("CPE 101", "Introduction to Computing"),
                    ("CPE 102", "Computer Programming 1"),
                    ("CPE 103", "Computer Programming 2"),
                    ("CPE 104", "Discrete Mathematics")
                };
                foreach (var course in courses)
                {
                    stage = "course-" + course.Code;
                    await using var insert = connection.CreateCommand();
                    insert.CommandText = "INSERT INTO `Subjects` " +
                        "(`AcademicLevelId`, `Code`, `Title`, `Description`, `IsPublished`, `CreatedAtUtc`, `UpdatedAtUtc`) " +
                        "SELECT 100, @code, @title, '', 1, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6) " +
                        "FROM DUAL WHERE NOT EXISTS (SELECT 1 FROM `Subjects` WHERE `Code` = @code)";
                    AddParameter(insert, "@code", DbType.String, course.Code);
                    AddParameter(insert, "@title", DbType.String, course.Title);
                    await insert.ExecuteNonQueryAsync();

                    await using var update = connection.CreateCommand();
                    update.CommandText = "UPDATE `Subjects` SET `AcademicLevelId` = 100, `Title` = @title, " +
                        "`UpdatedAtUtc` = UTC_TIMESTAMP(6) WHERE `Code` = @code " +
                        "AND (`AcademicLevelId` <> 100 OR `Title` <> @title)";
                    AddParameter(update, "@code", DbType.String, course.Code);
                    AddParameter(update, "@title", DbType.String, course.Title);
                    await update.ExecuteNonQueryAsync();
                }
                app.Logger.LogInformation("Freshman catalog records are up to date; Subjects metadata migration remains pending.");
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
