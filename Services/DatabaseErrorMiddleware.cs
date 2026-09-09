using System.Data.Common;
using Microsoft.EntityFrameworkCore;

namespace CPE_Review_Hub.Services;

public sealed class DatabaseSetupException : Exception;

public class DatabaseErrorMiddleware(RequestDelegate next, ILogger<DatabaseErrorMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception exception) when (IsDatabaseFailure(exception))
        {
            // Do not display database exceptions or connection strings in the browser.
            logger.LogWarning("Database request unavailable ({ErrorType}).", exception.GetType().Name);
            if (context.Response.HasStarted) throw;
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.ContentType = "text/html; charset=utf-8";
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers.RetryAfter = "30";
            await context.Response.WriteAsync("""
                <!doctype html><html lang="en"><head><meta charset="utf-8">
                <meta name="viewport" content="width=device-width, initial-scale=1">
                <title>Database unavailable | COLMS: CentOs Learning Management System</title><link rel="stylesheet" href="/css/hub.css"></head>
                <body><main class="auth-card standalone"><span class="eyebrow">COLMS: CentOs Learning Management System</span>
                <h1>Service temporarily unavailable</h1><p>COLMS cannot reach its database right now.
                Please try again shortly. If this continues, contact the administrator.</p>
                <p>This connection error does not mean your account has been deleted. Do not create a replacement account.</p>
                <a class="button" href="/">Back to home</a></main></body></html>
                """);
        }
    }

    // Data Protection/antiforgery can wrap a database outage in a cryptographic
    // exception while rendering a login form. Preserve the same safe 503 response.
    private static bool IsDatabaseFailure(Exception exception)
    {
        for (Exception? cause = exception; cause is not null; cause = cause.InnerException)
            if (cause is DatabaseSetupException or DbException or DbUpdateException) return true;
        return false;
    }
}
