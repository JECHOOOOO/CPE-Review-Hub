using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.Json;
using CPE_Review_Hub.Data;
using CPE_Review_Hub.Models;
using CPE_Review_Hub.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

namespace CPE_Review_Hub.Services;

// Explicit local operator commands; never exposed as HTTP endpoints.
public static class DatabaseCommands
{
    public static async Task<bool> TryRunAsync(WebApplication app, string[] args)
    {
        var configure = args.Contains("--configure-mysql");
        var migrate = args.Contains("--initialize-database");
        var admin = args.Contains("--create-admin");
        var seedCatalog = args.Contains("--seed-course-catalog");
        if (!configure && !migrate && !admin && !seedCatalog) return false;
        try
        {
            if (configure)
            {
                if (!app.Environment.IsDevelopment())
                    throw new InvalidOperationException("Local credential setup requires the Development environment.");
                await ConfigureAsync(app.Environment.ContentRootPath);
                return true;
            }
            await using var scope = app.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            if (migrate)
            {
                await db.Database.MigrateAsync();
                Console.WriteLine("MySQL schema is up to date. Existing data has not been reset.");
            }
            if (seedCatalog)
            {
                var result = await CourseCatalogSeeder.SeedAsync(db);
                Console.WriteLine($"Course catalog ready: {CourseCatalogSeeder.CourseCount} exact courses, " +
                    $"{CourseCatalogSeeder.LessonsPerCourse} lessons per course. Added {result.CoursesAdded} courses, " +
                    $"{result.LessonsAdded} lessons, {result.MaterialsAdded} study guides, " +
                    $"{result.ActivitiesAdded} activities, {result.AssessmentsAdded} assessments, " +
                    $"{result.QuestionsAdded} questions, {result.ChoicesAdded} choices and " +
                    $"{result.MappingsAdded} assessment-question links.");
            }
            if (admin)
            {
                var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var email = Prompt("Administrator email");
                var name = Prompt("Administrator full name");
                if (email.Length > 256 || !new EmailAddressAttribute().IsValid(email) || name.Length is < 1 or > 120)
                    throw new InvalidOperationException("Enter a valid email and full name (maximum 120 characters).");
                if (await users.FindByEmailAsync(email) is not null)
                    throw new InvalidOperationException("This email already exists. No account was changed or promoted.");
                Console.WriteLine("Use at least 8 characters. A special character is optional.");
                var password = ReadPassword("New admin password");
                if (password.Length is < 8 or > 128)
                    throw new InvalidOperationException("Use a password between 8 and 128 characters.");
                if (password != ReadPassword("Confirm password"))
                    throw new InvalidOperationException("Passwords do not match. No account was created.");
                var result = await users.CreateAsync(new ApplicationUser
                {
                    UserName = email, Email = email, FullName = name,
                    AccountType = PortalSecurity.Admin, LockoutEnabled = true
                }, password);
                if (!result.Succeeded)
                    throw new InvalidOperationException(string.Join(" ", result.Errors.Select(e => e.Description)));
                Console.WriteLine("Administrator created. Sign in at /admin.html. Password was not saved in source code.");
            }
        }
        catch (Exception exception)
        {
            Environment.ExitCode = 1;
            // MySQL messages can contain account/host information. Keep console failures generic.
            Console.Error.WriteLine(exception is InvalidOperationException ? exception.Message :
                $"MySQL setup failed ({exception.GetType().Name}). Check the service, credentials, database permissions and migrations. No reset was performed.");
        }
        return true;
    }

    private static async Task ConfigureAsync(string projectDirectory)
    {
        Console.WriteLine("Use an existing MySQL database and a dedicated database user (not root).");
        Console.WriteLine("Credentials will be saved in your Windows user-secrets store, outside this repository.");
        Console.WriteLine("User-secrets are for local development and are not encrypted. Do not share the secrets file.");
        var host = Prompt("MySQL host", "127.0.0.1");
        if (!uint.TryParse(Prompt("MySQL port", "3306"), out var port) || port is 0 or > 65535)
            throw new InvalidOperationException("Invalid port.");
        var database = Prompt("Database name", "cpe_review_hub");
        var username = Prompt("MySQL username", "cpe_review_hub");
        var password = ReadPassword("MySQL password");
        var connection = new MySqlConnectionStringBuilder
        {
            Server = host, Port = port, Database = database, UserID = username, Password = password,
            SslMode = MySqlSslMode.Required, ConnectionTimeout = 5, PersistSecurityInfo = false
        };
        await using (var testConnection = new MySqlConnection(connection.ConnectionString))
        {
            await testConnection.OpenAsync();
            if (testConnection.ServerVersion.Contains("MariaDB", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("This setup targets MySQL 8+, not the MariaDB server bundled with XAMPP.");
            var major = testConnection.ServerVersion.Split('.')[0];
            if (!int.TryParse(major, out var version) || version < 8)
                throw new InvalidOperationException("MySQL 8 or newer is required.");
        }
        var start = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = projectDirectory, UseShellExecute = false, RedirectStandardInput = true,
            RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true
        };
        foreach (var argument in new[] { "user-secrets", "set", "--project", "CPE-Review-Hub.csproj" })
            start.ArgumentList.Add(argument);
        using var process = Process.Start(start) ?? throw new InvalidOperationException("Could not start user-secrets setup.");
        // Pass credentials over stdin, not shell arguments or command history.
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        await process.StandardInput.WriteAsync(JsonSerializer.Serialize(new Dictionary<string, string>
        {
            ["ConnectionStrings:ReviewHub"] = connection.ConnectionString
        }));
        process.StandardInput.Close();
        await process.WaitForExitAsync();
        await Task.WhenAll(stdout, stderr);
        if (process.ExitCode != 0) throw new InvalidOperationException("Could not save local user secrets. Check your .NET SDK installation.");
        Console.WriteLine("MySQL connection verified and saved. Password was not printed or added to Git.");
    }

    private static string Prompt(string label, string? fallback = null)
    {
        Console.Write($"{label}{(fallback is null ? "" : $" [{fallback}]")}: ");
        var value = Console.ReadLine()?.Trim();
        return string.IsNullOrEmpty(value) ? fallback ?? "" : value;
    }

    private static string ReadPassword(string label)
    {
        Console.Write($"{label}: ");
        if (Console.IsInputRedirected) return Console.ReadLine() ?? "";
        var password = new System.Text.StringBuilder();
        while (true)
        {
            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Enter) break;
            if (key.Key == ConsoleKey.Backspace && password.Length > 0) password.Length--;
            else if (!char.IsControl(key.KeyChar)) password.Append(key.KeyChar);
        }
        Console.WriteLine();
        return password.ToString();
    }
}
