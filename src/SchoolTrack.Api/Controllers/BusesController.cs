using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolTrack.Api.Extensions;
using SchoolTrack.Application.Features.Fleet;
using SchoolTrack.Application.Features.Telemetry;
using SchoolTrack.Shared.Constants;
using SchoolTrack.Shared.DTOs.Devices;
using SchoolTrack.Shared.DTOs.Fleet;
using SchoolTrack.Shared.Models;

namespace SchoolTrack.Api.Controllers;

[ApiController]
[Route(ApiRoutes.Buses.Base)]
[Authorize(Policy = AuthorizationExtensions.RequireSchoolAdmin)]
public sealed class BusesController : ControllerBase
{
    private readonly CreateBusCommandHandler _createBusHandler;
    private readonly AssignDriverCommandHandler _assignDriverHandler;
    private readonly GetBusesQueryHandler _getBusesHandler;
    private readonly GetLatestBusLocationQueryHandler _getBusLocationHandler;

    public BusesController(
        CreateBusCommandHandler createBusHandler,
        AssignDriverCommandHandler assignDriverHandler,
        GetBusesQueryHandler getBusesHandler,
        GetLatestBusLocationQueryHandler getBusLocationHandler)
    {
        _createBusHandler = createBusHandler;
        _assignDriverHandler = assignDriverHandler;
        _getBusesHandler = getBusesHandler;
        _getBusLocationHandler = getBusLocationHandler;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<BusDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<BusDto>>> CreateBus(
        [FromBody] CreateBusRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateBusCommand(
            request.BusNumber,
            request.RegistrationNumber,
            request.Model,
            request.Capacity,
            request.InsuranceExpiry,
            request.FitnessExpiry);

        var result = await _createBusHandler.HandleAsync(command, cancellationToken);
        return CreatedAtAction(
            nameof(GetBuses),
            new { id = result.Id },
            ApiResponse<BusDto>.Ok(result, "Bus registered successfully.", HttpContext.TraceIdentifier));
    }

    [HttpPost(ApiRoutes.Buses.AssignDriver)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<string>>> AssignDriver(
        [FromRoute] Guid id,
        [FromBody] AssignDriverRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AssignDriverCommand(id, request.DriverId);
        await _assignDriverHandler.HandleAsync(command, cancellationToken);
        return Ok(ApiResponse<string>.Ok("Driver assigned to bus.", "Success", HttpContext.TraceIdentifier));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<BusDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<BusDto>>>> GetBuses(CancellationToken cancellationToken)
    {
        var result = await _getBusesHandler.HandleAsync(new GetBusesQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<BusDto>>.Ok(result, "Buses retrieved.", HttpContext.TraceIdentifier));
    }

    [HttpGet(ApiRoutes.Buses.Location)]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<LatestBusLocationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<LatestBusLocationDto>>> GetBusLocation(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _getBusLocationHandler.HandleAsync(new GetLatestBusLocationQuery(id), cancellationToken);
        return Ok(ApiResponse<LatestBusLocationDto>.Ok(result, "Latest bus location retrieved.", HttpContext.TraceIdentifier));
    }
}

