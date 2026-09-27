using CPE_Review_Hub.Data;
using Microsoft.EntityFrameworkCore;

namespace CPE_Review_Hub.Services;

/// <summary>
/// Applies the reviewed, additive Freshman catalog migration in production.
/// Only this exact pending migration is allowed; unexpected schema drift is
/// left for manual review. A MySQL advisory lock serializes serverless starts.
/// </summary>
public static class FreshmanCatalogDeployment
{
    private const string MigrationId = "20260927123952_FreshmanLevelAndCourseMetadata";
    private const string LockName = "colms_freshman_catalog_20260927";

    public static async Task ApplyAsync(WebApplication app)
    {
        if (!app.Environment.IsProduction()) return;

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
                    command.CommandText = $"SELECT GET_LOCK('{LockName}', 30)";
                    lockAcquired = Convert.ToInt32(await command.ExecuteScalarAsync()) == 1;
                }
                if (!lockAcquired)
                    throw new InvalidOperationException("The catalog migration lock was unavailable.");

                var pending = (await db.Database.GetPendingMigrationsAsync()).ToArray();
                if (pending.Length == 0) return;
                if (pending.Length != 1 || pending[0] != MigrationId)
                    throw new InvalidOperationException("Unexpected pending database migrations require review.");

                await db.Database.MigrateAsync(MigrationId);
                app.Logger.LogInformation("Applied the reviewed Freshman catalog migration.");
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
            app.Logger.LogError("Freshman catalog migration did not complete ({ErrorType}).",
                exception.GetType().Name);
        }
    }
}
