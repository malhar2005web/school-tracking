using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolTrack.Api.Extensions;
using SchoolTrack.Application.Features.Boarding;
using SchoolTrack.DeviceGateway.Abstractions;
using SchoolTrack.DeviceGateway.Services;
using SchoolTrack.DeviceGateway.Simulator;
using SchoolTrack.Shared.Constants;
using SchoolTrack.Shared.DTOs.Devices;
using SchoolTrack.Shared.Models;

namespace SchoolTrack.Api.Controllers;

/// <summary>
/// RFID reader device card tap ingestion, batch synchronization, and student boarding simulation.
/// </summary>
[ApiController]
[Route(ApiRoutes.Devices.Base)]
public sealed class RfidIngestionController : ControllerBase
{
    private readonly IRfidIngestionService _ingestionService;
    private readonly IRfidSimulatorService _simulatorService;
    private readonly IAdvancedRfidSimulator _advancedSimulator;
    private readonly GetUnknownCardTapsQueryHandler _getUnknownCardTapsHandler;

    public RfidIngestionController(
        IRfidIngestionService ingestionService,
        IRfidSimulatorService simulatorService,
        IAdvancedRfidSimulator advancedSimulator,
        GetUnknownCardTapsQueryHandler getUnknownCardTapsHandler)
    {
        _ingestionService = ingestionService;
        _simulatorService = simulatorService;
        _advancedSimulator = advancedSimulator;
        _getUnknownCardTapsHandler = getUnknownCardTapsHandler;
    }

    /// <summary>
    /// Ingests a single real-time RFID student card scan event from an on-bus reader.
    /// </summary>
    [HttpPost(ApiRoutes.Devices.RfidTap)]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<RfidScanResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<RfidScanResultDto>>> IngestTap(
        [FromBody] RfidScanRequest request,
        CancellationToken cancellationToken)
    {
        var packet = new RfidScanPacket(
            request.DeviceCode,
            request.ApiKey,
            request.CardUid,
            request.Latitude,
            request.Longitude,
            request.ScannedAt ?? DateTimeOffset.UtcNow);

        var result = await _ingestionService.IngestTapAsync(packet, cancellationToken);
        return Ok(ApiResponse<RfidScanResultDto>.Ok(result, result.Message, HttpContext.TraceIdentifier));
    }

    /// <summary>
    /// Ingests a batch of queued RFID scans collected during offline reader operation.
    /// </summary>
    [HttpPost(ApiRoutes.Devices.RfidBatchTap)]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RfidScanResultDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RfidScanResultDto>>>> IngestBatchTaps(
        [FromBody] RfidBatchScanRequest request,
        CancellationToken cancellationToken)
    {
        var results = await _ingestionService.IngestBatchTapsAsync(request, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<RfidScanResultDto>>.Ok(results, "Batch RFID taps synchronized.", HttpContext.TraceIdentifier));
    }

    /// <summary>
    /// Simulates a student RFID card tap for boarding or deboarding testing.
    /// </summary>
    [HttpPost(ApiRoutes.Devices.RfidSimulateTap)]
    [Authorize(Policy = AuthorizationExtensions.RequireTransportStaff)]
    [ProducesResponseType(typeof(ApiResponse<RfidScanResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RfidScanResultDto>>> SimulateTap(
        [FromBody] SimulateRfidTapRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _simulatorService.SimulateTapAsync(request, cancellationToken);
        return Ok(ApiResponse<RfidScanResultDto>.Ok(result, result.Message, HttpContext.TraceIdentifier));
    }

    /// <summary>
    /// Executes advanced multi-student RFID boarding sweep simulation with rapid double-tap deduplication and rogue card security interception.
    /// </summary>
    [HttpPost("rfid/advanced-simulate")]
    [Authorize(Policy = AuthorizationExtensions.RequireTransportStaff)]
    [ProducesResponseType(typeof(ApiResponse<AdvancedRfidSimulationResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<AdvancedRfidSimulationResult>>> AdvancedSimulateRfid(
        [FromBody] AdvancedRfidSimulationOptions options,
        CancellationToken cancellationToken)
    {
        var result = await _advancedSimulator.RunSimulationAsync(options, cancellationToken);
        return Ok(ApiResponse<AdvancedRfidSimulationResult>.Ok(result, "Advanced RFID simulation completed.", HttpContext.TraceIdentifier));
    }

    /// <summary>
    /// Retrieves security audit records for unknown, unregistered, or unassigned card taps.
    /// </summary>
    [HttpGet(ApiRoutes.Devices.UnknownCardTaps)]
    [Authorize(Policy = AuthorizationExtensions.RequireTransportStaff)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UnknownCardTapDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<UnknownCardTapDto>>>> GetUnknownCardTaps(
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var results = await _getUnknownCardTapsHandler.HandleAsync(new GetUnknownCardTapsQuery(take), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<UnknownCardTapDto>>.Ok(results, "Unknown card taps retrieved.", HttpContext.TraceIdentifier));
    }
}

