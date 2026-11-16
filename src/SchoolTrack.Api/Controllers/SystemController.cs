using Microsoft.AspNetCore.Mvc;
using SchoolTrack.Shared.Constants;
using SchoolTrack.Shared.Models;

namespace SchoolTrack.Api.Controllers;

/// <summary>
/// Provides platform metadata and diagnostics for service verification.
/// </summary>
[ApiController]
[Route(ApiRoutes.Version1Prefix + "/system")]
public sealed class SystemController : ControllerBase
{
    private readonly IHostEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of <see cref="SystemController"/>.
    /// </summary>
    public SystemController(IHostEnvironment environment)
    {
        _environment = environment;
    }

    /// <summary>
    /// Returns basic platform service metadata.
    /// </summary>
    [HttpGet("info")]
    [ProducesResponseType(typeof(ApiResponse<SystemInfoResponse>), StatusCodes.Status200OK)]
    public ActionResult<ApiResponse<SystemInfoResponse>> GetInfo()
    {
        var info = new SystemInfoResponse(
            Service: "SchoolTrack.Api",
            ApiVersion: "v1",
            Environment: _environment.EnvironmentName,
            UtcNow: DateTimeOffset.UtcNow);

        return Ok(ApiResponse<SystemInfoResponse>.Ok(
            info,
            message: "SchoolTrack API is operational.",
            traceId: HttpContext.TraceIdentifier));
    }
}

/// <summary>
/// Metadata payload describing the running SchoolTrack API service.
/// </summary>
public sealed record SystemInfoResponse(
    string Service,
    string ApiVersion,
    string Environment,
    DateTimeOffset UtcNow);

