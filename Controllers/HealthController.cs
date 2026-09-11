using CPE_Review_Hub.Data;
using CPE_Review_Hub.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CPE_Review_Hub.Controllers;

[ApiController, AllowAnonymous, ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
public sealed class HealthController(ApplicationDbContext db, ILogger<HealthController> logger) : ControllerBase
{
    [HttpGet("/health/live")]
    public IActionResult Live() => Ok(new { status = "Healthy" });

    [HttpGet("/health"), HttpGet("/health/ready")]
    public async Task<IActionResult> Ready(CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(5));
        try
        {
            // The operation is idempotent and additive. It covers a deployment
            // whose initial container startup could not finish the image schema
            // update, without dropping or rewriting learning records.
            await OnlineLessonImageDeployment.RepairAsync(db, logger);
            if (!await db.Database.CanConnectAsync(timeout.Token))
                return StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "Unhealthy" });

            // Verify both the connection and the schema used by lesson pages.
            await db.Database.SqlQueryRaw<int>("SELECT 1 AS Value").SingleAsync(timeout.Token);
            await db.LessonImages.AsNoTracking().Select(x => new
            {
                x.Id, x.Author, x.License, x.SourceName, x.SourceUrl
            }).Take(1).ToListAsync(timeout.Token);
            return Ok(new { status = "Healthy" });
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Readiness database check failed ({ErrorType}).", exception.GetType().Name);
        }
        return StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "Unhealthy" });
    }
}
