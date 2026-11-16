using Microsoft.EntityFrameworkCore;
using SchoolTrack.Application.Common.Interfaces;
using SchoolTrack.DeviceGateway.Abstractions;
using SchoolTrack.DeviceGateway.Services;
using SchoolTrack.Domain.Entities;
using SchoolTrack.Domain.Interfaces;
using SchoolTrack.Shared.DTOs.Devices;

namespace SchoolTrack.DeviceGateway.Simulator;

public interface IRfidSimulatorService
{
    Task<RfidScanResultDto> SimulateTapAsync(SimulateRfidTapRequest request, CancellationToken cancellationToken = default);
}

public sealed class RfidSimulatorService : IRfidSimulatorService
{
    private readonly IApplicationDbContext _context;
    private readonly IRfidIngestionService _ingestionService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RfidSimulatorService(
        IApplicationDbContext context,
        IRfidIngestionService ingestionService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _ingestionService = ingestionService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<RfidScanResultDto> SimulateTapAsync(SimulateRfidTapRequest request, CancellationToken cancellationToken = default)
    {
        var trip = await _context.Trips
            .IgnoreQueryFilters()
            .Include(t => t.Bus)
                .ThenInclude(b => b!.RfidDevice)
            .Include(t => t.Stops)
                .ThenInclude(ts => ts.RouteStop)
            .FirstOrDefaultAsync(t => t.Id == request.TripId, cancellationToken);

        if (trip == null)
        {
            throw new KeyNotFoundException($"Trip with ID '{request.TripId}' was not found.");
        }

        if (trip.Bus == null || trip.Bus.RfidDevice == null)
        {
            throw new InvalidOperationException($"Bus assigned to trip '{request.TripId}' has no registered RFID reader device.");
        }

        var rfidCard = await _context.RfidCards
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.StudentId == request.StudentId && c.Status == "Active", cancellationToken);

        if (rfidCard == null)
        {
            throw new InvalidOperationException($"Student with ID '{request.StudentId}' has no active registered RFID card.");
        }

        decimal? lat = null;
        decimal? lon = null;

        if (request.StopId.HasValue)
        {
            var routeStop = trip.Stops
                .FirstOrDefault(s => s.RouteStopId == request.StopId.Value || s.Id == request.StopId.Value)?.RouteStop;

            if (routeStop != null)
            {
                lat = routeStop.Latitude;
                lon = routeStop.Longitude;
            }
        }

        var packet = new RfidScanPacket(
            trip.Bus.RfidDevice.DeviceCode,
            trip.Bus.RfidDevice.ApiKey,
            rfidCard.CardUid,
            lat,
            lon,
            _dateTimeProvider.UtcNow,
            "{\"simulated\":true}");

        return await _ingestionService.IngestTapAsync(packet, cancellationToken);
    }
}

