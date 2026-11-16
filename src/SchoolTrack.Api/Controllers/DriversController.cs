using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolTrack.Api.Extensions;
using SchoolTrack.Application.Features.Drivers;
using SchoolTrack.Shared.Constants;
using SchoolTrack.Shared.DTOs.Drivers;
using SchoolTrack.Shared.Models;

namespace SchoolTrack.Api.Controllers;

[ApiController]
[Route(ApiRoutes.Drivers.Base)]
[Authorize(Policy = AuthorizationExtensions.RequireSchoolAdmin)]
public sealed class DriversController : ControllerBase
{
    private readonly CreateDriverCommandHandler _createDriverHandler;
    private readonly GetDriversQueryHandler _getDriversHandler;

    public DriversController(
        CreateDriverCommandHandler createDriverHandler,
        GetDriversQueryHandler getDriversHandler)
    {
        _createDriverHandler = createDriverHandler;
        _getDriversHandler = getDriversHandler;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<DriverDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<DriverDto>>> CreateDriver(
        [FromBody] CreateDriverRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateDriverCommand(
            request.FullName,
            request.Email,
            request.Password,
            request.LicenseNumber,
            request.LicenseExpiry,
            request.Phone,
            request.YearsExperience);

        var result = await _createDriverHandler.HandleAsync(command, cancellationToken);
        return CreatedAtAction(
            nameof(GetDrivers),
            new { id = result.Id },
            ApiResponse<DriverDto>.Ok(result, "Driver created successfully.", HttpContext.TraceIdentifier));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<DriverDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DriverDto>>>> GetDrivers(CancellationToken cancellationToken)
    {
        var result = await _getDriversHandler.HandleAsync(new GetDriversQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<DriverDto>>.Ok(result, "Drivers retrieved.", HttpContext.TraceIdentifier));
    }
}

