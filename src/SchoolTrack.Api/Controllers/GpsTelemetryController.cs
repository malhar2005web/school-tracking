using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolTrack.Api.Extensions;
using SchoolTrack.DeviceGateway.Abstractions;
using SchoolTrack.DeviceGateway.Services;
using SchoolTrack.DeviceGateway.Simulator;
using SchoolTrack.Shared.Constants;
using SchoolTrack.Shared.DTOs.Devices;
using SchoolTrack.Shared.Models;

namespace SchoolTrack.Api.Controllers;

/// <summary>
/// Device telemetry ingestion gateway and route simulation endpoints.
/// </summary>
[ApiController]
[Route(ApiRoutes.Devices.Base)]
public sealed class GpsTelemetryController : ControllerBase
{
    private readonly IGpsIngestionService _ingestionService;
    private readonly IGpsSimulatorService _simulatorService;
    private readonly IAdvancedGpsSimulator _advancedSimulator;

    public GpsTelemetryController(
        IGpsIngestionService ingestionService,
        IGpsSimulatorService simulatorService,
        IAdvancedGpsSimulator advancedSimulator)
    {
        _ingestionService = ingestionService;
        _simulatorService = simulatorService;
        _advancedSimulator = advancedSimulator;
    }

    /// <summary>
    /// Ingests a single real-time GPS telemetry packet from a mounted tracking device or simulator.
    /// </summary>
    [HttpPost(ApiRoutes.Devices.GpsTelemetry)]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<GpsIngestionResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<GpsIngestionResultDto>>> IngestTelemetry(
        [FromBody] GpsTelemetryRequest request,
        CancellationToken cancellationToken)
    {
        var packet = new GpsTelemetryPacket(
            request.DeviceCode,
            request.ApiKey,
            request.Imei,
            request.Latitude,
            request.Longitude,
            request.SpeedKmh,
            request.Heading,
            request.Altitude,
            request.AccuracyMeters,
            request.IgnitionOn,
            request.RecordedAt ?? DateTimeOffset.UtcNow,
            request.RawPayload);

        var result = await _ingestionService.IngestAsync(packet, cancellationToken);
        return Ok(ApiResponse<GpsIngestionResultDto>.Ok(result, "GPS telemetry ingested.", HttpContext.TraceIdentifier));
    }

    /// <summary>
    /// Ingests a batch of queued GPS points recorded during cellular connectivity loss (offline synchronization).
    /// </summary>
    [HttpPost(ApiRoutes.Devices.GpsBatchTelemetry)]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<GpsIngestionResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<GpsIngestionResultDto>>> IngestBatchTelemetry(
        [FromBody] GpsBatchTelemetryRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _ingestionService.IngestBatchAsync(request, cancellationToken);
        return Ok(ApiResponse<GpsIngestionResultDto>.Ok(result, "Batch telemetry synchronized.", HttpContext.TraceIdentifier));
    }

    /// <summary>
    /// Generates and ingests synthetic GPS telemetry along a designated trip route for integration testing and live demonstrations.
    /// </summary>
    [HttpPost(ApiRoutes.Devices.GpsSimulate)]
    [Authorize(Policy = AuthorizationExtensions.RequireTransportStaff)]
    [ProducesResponseType(typeof(ApiResponse<SimulateGpsTripResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SimulateGpsTripResponse>>> SimulateTrip(
        [FromBody] SimulateGpsTripRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _simulatorService.SimulateTripAsync(request, cancellationToken);
        return Ok(ApiResponse<SimulateGpsTripResponse>.Ok(result, "Trip telemetry simulation completed.", HttpContext.TraceIdentifier));
    }

    /// <summary>
    /// Executes advanced kinematic GPS simulation with stop dwell times, acceleration profiles, offline buffering, and degraded accuracy tests.
    /// </summary>
    [HttpPost("gps/advanced-simulate")]
    [Authorize(Policy = AuthorizationExtensions.RequireTransportStaff)]
    [ProducesResponseType(typeof(ApiResponse<AdvancedGpsSimulationResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<AdvancedGpsSimulationResult>>> AdvancedSimulateTrip(
        [FromBody] AdvancedGpsSimulationOptions options,
        CancellationToken cancellationToken)
    {
        var result = await _advancedSimulator.RunSimulationAsync(options, cancellationToken);
        return Ok(ApiResponse<AdvancedGpsSimulationResult>.Ok(result, "Advanced kinematic GPS simulation completed.", HttpContext.TraceIdentifier));
    }
}
