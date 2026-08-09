using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QueryPlus.Data.Context;

namespace QueryPlus.Api.Api;

[ApiController]
[Route("api/health")]
public sealed class HealthController(ApplicationDbContext db) : ControllerBase
{
    private static readonly TimeSpan ReadyCheckTimeout = TimeSpan.FromSeconds(5);

    /// <summary>Liveness only - proves the .NET process is up, nothing more.</summary>
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Get() => Ok(new { status = "healthy" });

    /// <summary>Readiness - additionally proves the app can reach the SQL Server catalog database.</summary>
    [AllowAnonymous]
    [HttpGet("ready")]
    public async Task<IActionResult> Ready(CancellationToken cancellationToken)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(ReadyCheckTimeout);

        try
        {
            if (await db.Database.CanConnectAsync(timeoutCts.Token))
            {
                return Ok(new { status = "healthy" });
            }
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            // Falls through to the unhealthy response below.
        }

        return StatusCode(StatusCodes.Status503ServiceUnavailable,
            new { status = "unhealthy", reason = "database unreachable" });
    }
}
