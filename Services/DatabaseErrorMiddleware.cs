using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace CPE_Review_Hub.Services;

public sealed class DatabaseSetupException : Exception;

public class DatabaseErrorMiddleware(RequestDelegate next, ILogger<DatabaseErrorMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception exception) when (exception is DatabaseSetupException or DbException or DbUpdateException)
        {
            // Do not display database exceptions or connection strings in the browser.
            logger.LogWarning("Database request unavailable ({ErrorType}).", exception.GetType().Name);
            if (context.Response.HasStarted) throw;
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.Headers.CacheControl = "no-store";
            await context.Response.WriteAsync("""
                <!doctype html><html lang="en"><head><meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <title>Database unavailable | COLMS: CentOs Learning Management System</title><link rel="stylesheet" href="/css/hub.css"></head>
                <body><main class="auth-card standalone"><span class="eyebrow">COLMS: CentOs Learning Management System</span>
                <h1>Database connection needed</h1><p>Accounts are temporarily unavailable. Ask the system administrator
                to check MySQL, connection settings, and database migrations.</p>
                <p>For local setup, run <strong>Setup CPE Review Hub.cmd</strong> in the project folder.</p>
                <a class="button" href="/">Back to home</a></main></body></html>
                """);
        }
    }
}
