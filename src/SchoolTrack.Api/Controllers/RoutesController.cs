using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolTrack.Api.Extensions;
using SchoolTrack.Application.Features.Routes;
using SchoolTrack.Shared.Constants;
using SchoolTrack.Shared.DTOs.Routes;
using SchoolTrack.Shared.Models;

namespace SchoolTrack.Api.Controllers;

[ApiController]
[Route(ApiRoutes.Routes.Base)]
[Authorize(Policy = AuthorizationExtensions.RequireTransportStaff)]
public sealed class RoutesController : ControllerBase
{
    private readonly CreateRouteCommandHandler _createRouteHandler;
    private readonly AddRouteStopCommandHandler _addRouteStopHandler;
    private readonly CreateRouteAssignmentCommandHandler _createAssignmentHandler;
    private readonly GetRoutesQueryHandler _getRoutesHandler;
    private readonly GetRouteByIdQueryHandler _getRouteByIdHandler;

    public RoutesController(
        CreateRouteCommandHandler createRouteHandler,
        AddRouteStopCommandHandler addRouteStopHandler,
        CreateRouteAssignmentCommandHandler createAssignmentHandler,
        GetRoutesQueryHandler getRoutesHandler,
        GetRouteByIdQueryHandler getRouteByIdHandler)
    {
        _createRouteHandler = createRouteHandler;
        _addRouteStopHandler = addRouteStopHandler;
        _createAssignmentHandler = createAssignmentHandler;
        _getRoutesHandler = getRoutesHandler;
        _getRouteByIdHandler = getRouteByIdHandler;
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationExtensions.RequireSchoolAdmin)]
    [ProducesResponseType(typeof(ApiResponse<RouteDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<RouteDto>>> CreateRoute(
        [FromBody] CreateRouteRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateRouteCommand(request.Name, request.Code, request.DirectionType);
        var result = await _createRouteHandler.HandleAsync(command, cancellationToken);

        return CreatedAtAction(
            nameof(GetRouteById),
            new { id = result.Id },
            ApiResponse<RouteDto>.Ok(result, "Route created successfully.", HttpContext.TraceIdentifier));
    }

    [HttpPost(ApiRoutes.Routes.Stops)]
    [Authorize(Policy = AuthorizationExtensions.RequireSchoolAdmin)]
    [ProducesResponseType(typeof(ApiResponse<RouteStopDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<RouteStopDto>>> AddRouteStop(
        [FromRoute] Guid id,
        [FromBody] AddRouteStopRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddRouteStopCommand(
            id,
            request.StopName,
            request.SequenceOrder,
            request.Latitude,
            request.Longitude,
            request.ScheduledOffsetMinutes,
            request.GeofenceRadiusMeters);

        var result = await _addRouteStopHandler.HandleAsync(command, cancellationToken);
        return Ok(ApiResponse<RouteStopDto>.Ok(result, "Route stop added.", HttpContext.TraceIdentifier));
    }

    [HttpPost(ApiRoutes.Routes.Assignments)]
    [Authorize(Policy = AuthorizationExtensions.RequireSchoolAdmin)]
    [ProducesResponseType(typeof(ApiResponse<RouteAssignmentDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<RouteAssignmentDto>>> CreateAssignment(
        [FromBody] CreateRouteAssignmentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateRouteAssignmentCommand(
            request.RouteId,
            request.BusId,
            request.DriverId,
            request.Shift,
            request.EffectiveFrom,
            request.EffectiveTo);

        var result = await _createAssignmentHandler.HandleAsync(command, cancellationToken);
        return Ok(ApiResponse<RouteAssignmentDto>.Ok(result, "Route assignment scheduled.", HttpContext.TraceIdentifier));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RouteDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RouteDto>>>> GetRoutes(CancellationToken cancellationToken)
    {
        var result = await _getRoutesHandler.HandleAsync(new GetRoutesQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<RouteDto>>.Ok(result, "Routes retrieved.", HttpContext.TraceIdentifier));
    }

    [HttpGet(ApiRoutes.Routes.Id)]
    [ProducesResponseType(typeof(ApiResponse<RouteDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<RouteDto>>> GetRouteById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _getRouteByIdHandler.HandleAsync(new GetRouteByIdQuery(id), cancellationToken);
        return Ok(ApiResponse<RouteDto>.Ok(result, "Route details retrieved.", HttpContext.TraceIdentifier));
    }
}

