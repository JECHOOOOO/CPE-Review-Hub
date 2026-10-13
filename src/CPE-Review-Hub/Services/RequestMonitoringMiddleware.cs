using System.Diagnostics;

namespace CPE_Review_Hub.Services;

public sealed class RequestMonitoringMiddleware(RequestDelegate next,
    ILogger<RequestMonitoringMiddleware> logger)
{
    private static readonly TimeSpan SlowRequestThreshold = TimeSpan.FromSeconds(2);

    public async Task InvokeAsync(HttpContext context)
    {
        var started = Stopwatch.GetTimestamp();
        await next(context);
        var elapsed = Stopwatch.GetElapsedTime(started);

        if (context.Request.Path.StartsWithSegments("/Admin") &&
            context.Response.StatusCode is StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden)
            logger.LogWarning("Unauthorized Admin route request was blocked with HTTP {StatusCode}.",
                context.Response.StatusCode);

        if (elapsed >= SlowRequestThreshold)
            logger.LogWarning("Slow {Method} request to {Path} completed with HTTP {StatusCode} in {ElapsedMs} ms.",
                context.Request.Method, context.Request.Path.Value, context.Response.StatusCode,
                Math.Round(elapsed.TotalMilliseconds));
    }
}
