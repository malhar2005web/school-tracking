using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolTrack.Api.Extensions;
using SchoolTrack.Application.Features.Boarding;
using SchoolTrack.Application.Features.Telemetry;
using SchoolTrack.Application.Features.Trips;
using SchoolTrack.Domain.Constants;
using SchoolTrack.Shared.Constants;
using SchoolTrack.Shared.DTOs.Devices;
using SchoolTrack.Shared.DTOs.Trips;
using SchoolTrack.Shared.Models;

namespace SchoolTrack.Api.Controllers;

[ApiController]
[Route(ApiRoutes.Trips.Base)]
[Authorize]
public sealed class TripsController : ControllerBase
{
    private readonly CreateTripCommandHandler _createTripHandler;
    private readonly UpdateTripStatusCommandHandler _updateStatusHandler;
    private readonly UpdateTripStopStatusCommandHandler _updateStopStatusHandler;
    private readonly GetTripsQueryHandler _getTripsHandler;
    private readonly GetTripByIdQueryHandler _getTripByIdHandler;
    private readonly GetTripTrailQueryHandler _getTripTrailHandler;
    private readonly GetTripPassengerManifestQueryHandler _getManifestHandler;

    public TripsController(
        CreateTripCommandHandler createTripHandler,
        UpdateTripStatusCommandHandler updateStatusHandler,
        UpdateTripStopStatusCommandHandler updateStopStatusHandler,
        GetTripsQueryHandler getTripsHandler,
        GetTripByIdQueryHandler getTripByIdHandler,
        GetTripTrailQueryHandler getTripTrailHandler,
        GetTripPassengerManifestQueryHandler getManifestHandler)
    {
        _createTripHandler = createTripHandler;
        _updateStatusHandler = updateStatusHandler;
        _updateStopStatusHandler = updateStopStatusHandler;
        _getTripsHandler = getTripsHandler;
        _getTripByIdHandler = getTripByIdHandler;
        _getTripTrailHandler = getTripTrailHandler;
        _getManifestHandler = getManifestHandler;
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationExtensions.RequireTransportStaff)]
    [ProducesResponseType(typeof(ApiResponse<TripDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<TripDto>>> CreateTrip(
        [FromBody] CreateTripRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateTripCommand(
            request.RouteId,
            request.BusId,
            request.DriverId,
            request.TripDate,
            request.Shift,
            request.ScheduledStartAt);

        var result = await _createTripHandler.HandleAsync(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetTripById),
            new { id = result.Id },
            ApiResponse<TripDto>.Ok(result, "Trip scheduled successfully.", HttpContext.TraceIdentifier));
    }

    [HttpPut(ApiRoutes.Trips.Status)]
    [ProducesResponseType(typeof(ApiResponse<TripDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TripDto>>> UpdateStatus(
        [FromRoute] Guid id,
        [FromBody] UpdateTripStatusRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateTripStatusCommand(id, request.TargetStatus, request.Notes);
        var result = await _updateStatusHandler.HandleAsync(command, cancellationToken);

        return Ok(ApiResponse<TripDto>.Ok(result, $"Trip status updated to {result.Status}.", HttpContext.TraceIdentifier));
    }

    [HttpPut(ApiRoutes.Trips.StopStatus)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<string>>> UpdateStopStatus(
        [FromRoute] Guid id,
        [FromRoute] Guid stopId,
        [FromBody] UpdateTripStopStatusRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateTripStopStatusCommand(id, stopId, request.Status);
        await _updateStopStatusHandler.HandleAsync(command, cancellationToken);

        return Ok(ApiResponse<string>.Ok("Stop status updated.", "Success", HttpContext.TraceIdentifier));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TripDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TripDto>>>> GetTrips(
        [FromQuery] DateOnly? date,
        [FromQuery] string? shift,
        [FromQuery] string? status,
        [FromQuery] Guid? busId,
        [FromQuery] Guid? driverId,
        CancellationToken cancellationToken)
    {
        var query = new GetTripsQuery(date, shift, status, busId, driverId);
        var result = await _getTripsHandler.HandleAsync(query, cancellationToken);

        return Ok(ApiResponse<IReadOnlyList<TripDto>>.Ok(result, "Trips retrieved.", HttpContext.TraceIdentifier));
    }

    [HttpGet(ApiRoutes.Trips.Active)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TripDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TripDto>>>> GetActiveTrips(CancellationToken cancellationToken)
    {
        var query = new GetTripsQuery(Status: TripStatuses.Started);
        var startedTrips = await _getTripsHandler.HandleAsync(query, cancellationToken);

        var transitQuery = new GetTripsQuery(Status: TripStatuses.InTransit);
        var inTransitTrips = await _getTripsHandler.HandleAsync(transitQuery, cancellationToken);

        var activeTrips = startedTrips.Concat(inTransitTrips).ToList();
        return Ok(ApiResponse<IReadOnlyList<TripDto>>.Ok(activeTrips, "Active trips retrieved.", HttpContext.TraceIdentifier));
    }

    [HttpGet(ApiRoutes.Trips.Id)]
    [ProducesResponseType(typeof(ApiResponse<TripDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TripDto>>> GetTripById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _getTripByIdHandler.HandleAsync(new GetTripByIdQuery(id), cancellationToken);
        return Ok(ApiResponse<TripDto>.Ok(result, "Trip details retrieved.", HttpContext.TraceIdentifier));
    }

    [HttpGet(ApiRoutes.Trips.Trail)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<LocationLogDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<LocationLogDto>>>> GetTripTrail(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _getTripTrailHandler.HandleAsync(new GetTripTrailQuery(id), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<LocationLogDto>>.Ok(result, "Trip location trail retrieved.", HttpContext.TraceIdentifier));
    }

    [HttpGet(ApiRoutes.Trips.Manifest)]
    [ProducesResponseType(typeof(ApiResponse<TripPassengerManifestDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TripPassengerManifestDto>>> GetTripManifest(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _getManifestHandler.HandleAsync(new GetTripPassengerManifestQuery(id), cancellationToken);
        return Ok(ApiResponse<TripPassengerManifestDto>.Ok(result, "Trip passenger manifest retrieved.", HttpContext.TraceIdentifier));
    }
}

