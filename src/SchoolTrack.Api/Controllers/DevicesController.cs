using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolTrack.Api.Extensions;
using SchoolTrack.Application.Features.Devices;
using SchoolTrack.Shared.Constants;
using SchoolTrack.Shared.DTOs.Devices;
using SchoolTrack.Shared.Models;

namespace SchoolTrack.Api.Controllers;

[ApiController]
[Route(ApiRoutes.Devices.Base)]
[Authorize(Policy = AuthorizationExtensions.RequireSchoolAdmin)]
public sealed class DevicesController : ControllerBase
{
    private readonly CreateRfidCardCommandHandler _createRfidCardHandler;
    private readonly AssignRfidCardCommandHandler _assignRfidCardHandler;
    private readonly RegisterGpsDeviceCommandHandler _registerGpsDeviceHandler;
    private readonly RegisterRfidDeviceCommandHandler _registerRfidDeviceHandler;
    private readonly GetRfidCardsQueryHandler _getRfidCardsHandler;
    private readonly GetGpsDevicesQueryHandler _getGpsDevicesHandler;
    private readonly GetRfidDevicesQueryHandler _getRfidDevicesHandler;

    public DevicesController(
        CreateRfidCardCommandHandler createRfidCardHandler,
        AssignRfidCardCommandHandler assignRfidCardHandler,
        RegisterGpsDeviceCommandHandler registerGpsDeviceHandler,
        RegisterRfidDeviceCommandHandler registerRfidDeviceHandler,
        GetRfidCardsQueryHandler getRfidCardsHandler,
        GetGpsDevicesQueryHandler getGpsDevicesHandler,
        GetRfidDevicesQueryHandler getRfidDevicesHandler)
    {
        _createRfidCardHandler = createRfidCardHandler;
        _assignRfidCardHandler = assignRfidCardHandler;
        _registerGpsDeviceHandler = registerGpsDeviceHandler;
        _registerRfidDeviceHandler = registerRfidDeviceHandler;
        _getRfidCardsHandler = getRfidCardsHandler;
        _getGpsDevicesHandler = getGpsDevicesHandler;
        _getRfidDevicesHandler = getRfidDevicesHandler;
    }

    [HttpPost(ApiRoutes.Devices.RfidCards)]
    [ProducesResponseType(typeof(ApiResponse<RfidCardDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<RfidCardDto>>> CreateRfidCard(
        [FromBody] CreateRfidCardRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreateRfidCardCommand(request.CardUid, request.CardType, request.StudentId);
        var result = await _createRfidCardHandler.HandleAsync(command, cancellationToken);
        return Ok(ApiResponse<RfidCardDto>.Ok(result, "RFID card registered.", HttpContext.TraceIdentifier));
    }

    [HttpPost(ApiRoutes.Devices.RfidCardAssign)]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<string>>> AssignRfidCard(
        [FromRoute] Guid id,
        [FromBody] AssignRfidCardRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AssignRfidCardCommand(id, request.StudentId);
        await _assignRfidCardHandler.HandleAsync(command, cancellationToken);
        return Ok(ApiResponse<string>.Ok("RFID card assigned to student.", "Success", HttpContext.TraceIdentifier));
    }

    [HttpGet(ApiRoutes.Devices.RfidCards)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RfidCardDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RfidCardDto>>>> GetRfidCards(CancellationToken cancellationToken)
    {
        var result = await _getRfidCardsHandler.HandleAsync(new GetRfidCardsQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<RfidCardDto>>.Ok(result, "RFID cards retrieved.", HttpContext.TraceIdentifier));
    }

    [HttpPost(ApiRoutes.Devices.GpsDevices)]
    [ProducesResponseType(typeof(ApiResponse<GpsDeviceDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<GpsDeviceDto>>> RegisterGpsDevice(
        [FromBody] RegisterGpsDeviceRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterGpsDeviceCommand(
            request.DeviceCode,
            request.BusId,
            request.Imei,
            request.Model,
            request.SimCardNumber,
            request.ProtocolType);

        var result = await _registerGpsDeviceHandler.HandleAsync(command, cancellationToken);
        return Ok(ApiResponse<GpsDeviceDto>.Ok(result, "GPS device registered.", HttpContext.TraceIdentifier));
    }

    [HttpGet(ApiRoutes.Devices.GpsDevices)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<GpsDeviceDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<GpsDeviceDto>>>> GetGpsDevices(CancellationToken cancellationToken)
    {
        var result = await _getGpsDevicesHandler.HandleAsync(new GetGpsDevicesQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<GpsDeviceDto>>.Ok(result, "GPS devices retrieved.", HttpContext.TraceIdentifier));
    }

    [HttpPost(ApiRoutes.Devices.RfidDevices)]
    [ProducesResponseType(typeof(ApiResponse<RfidDeviceDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<RfidDeviceDto>>> RegisterRfidDevice(
        [FromBody] RegisterRfidDeviceRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RegisterRfidDeviceCommand(
            request.DeviceCode,
            request.BusId,
            request.GateLocation,
            request.Model,
            request.IpAddress,
            request.Port,
            request.ProtocolType);

        var result = await _registerRfidDeviceHandler.HandleAsync(command, cancellationToken);
        return Ok(ApiResponse<RfidDeviceDto>.Ok(result, "RFID reader registered.", HttpContext.TraceIdentifier));
    }

    [HttpGet(ApiRoutes.Devices.RfidDevices)]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<RfidDeviceDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<RfidDeviceDto>>>> GetRfidDevices(CancellationToken cancellationToken)
    {
        var result = await _getRfidDevicesHandler.HandleAsync(new GetRfidDevicesQuery(), cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<RfidDeviceDto>>.Ok(result, "RFID readers retrieved.", HttpContext.TraceIdentifier));
    }
}

