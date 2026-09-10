using CPE_Review_Hub.Data;
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
            if (await db.Database.CanConnectAsync(timeout.Token))
                return Ok(new { status = "Healthy" });
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Readiness database check failed ({ErrorType}).", exception.GetType().Name);
        }
        return StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "Unhealthy" });
    }
}
