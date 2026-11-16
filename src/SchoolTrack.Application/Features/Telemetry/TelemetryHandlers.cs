using Microsoft.EntityFrameworkCore;
using SchoolTrack.Application.Common.Interfaces;
using SchoolTrack.Domain.Interfaces;
using SchoolTrack.Shared.DTOs.Devices;

namespace SchoolTrack.Application.Features.Telemetry;

public sealed record GetLatestBusLocationQuery(Guid BusId);

public sealed class GetLatestBusLocationQueryHandler
{
    private static readonly TimeSpan OnlineHeartbeatThreshold = TimeSpan.FromSeconds(90);

    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetLatestBusLocationQueryHandler(IApplicationDbContext context, IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<LatestBusLocationDto> HandleAsync(GetLatestBusLocationQuery query, CancellationToken cancellationToken = default)
    {
        var bus = await _context.Buses
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == query.BusId, cancellationToken);

        if (bus == null)
        {
            throw new KeyNotFoundException($"Bus with ID '{query.BusId}' was not found.");
        }

        var latest = await _context.LatestBusLocations
            .Include(l => l.Trip)
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.BusId == query.BusId, cancellationToken);

        if (latest == null)
        {
            throw new KeyNotFoundException($"No GPS telemetry has been recorded yet for Bus '{bus.BusNumber}'.");
        }

        var utcNow = _dateTimeProvider.UtcNow;
        var isOnline = latest.IsOnline && (utcNow - latest.RecordedAt <= OnlineHeartbeatThreshold);

        return new LatestBusLocationDto(
            latest.BusId,
            bus.BusNumber,
            latest.SchoolId,
            latest.TripId,
            latest.Trip?.Status,
            latest.Latitude,
            latest.Longitude,
            latest.SpeedKmh,
            latest.Heading,
            latest.Altitude,
            latest.AccuracyMeters,
            latest.IgnitionOn,
            isOnline,
            latest.RecordedAt,
            latest.UpdatedAt);
    }
}

public sealed record GetTripTrailQuery(Guid TripId);

public sealed class GetTripTrailQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetTripTrailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<LocationLogDto>> HandleAsync(GetTripTrailQuery query, CancellationToken cancellationToken = default)
    {
        var tripExists = await _context.Trips
            .AsNoTracking()
            .AnyAsync(t => t.Id == query.TripId, cancellationToken);

        if (!tripExists)
        {
            throw new KeyNotFoundException($"Trip with ID '{query.TripId}' was not found.");
        }

        var logs = await _context.LocationLogs
            .Where(l => l.TripId == query.TripId)
            .OrderBy(l => l.RecordedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return logs.Select(l => new LocationLogDto(
            l.Id,
            l.BusId,
            l.TripId,
            l.Latitude,
            l.Longitude,
            l.SpeedKmh,
            l.Heading,
            l.Altitude,
            l.AccuracyMeters,
            l.IgnitionOn,
            l.RecordedAt,
            l.ReceivedAt)).ToList();
    }
}

