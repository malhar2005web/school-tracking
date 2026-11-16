using Microsoft.EntityFrameworkCore;
using SchoolTrack.Application.Common.Interfaces;
using SchoolTrack.Domain.Constants;
using SchoolTrack.Shared.DTOs.Devices;

namespace SchoolTrack.Application.Features.Boarding;

public sealed record GetTripPassengerManifestQuery(Guid TripId);

public sealed class GetTripPassengerManifestQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetTripPassengerManifestQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TripPassengerManifestDto> HandleAsync(GetTripPassengerManifestQuery query, CancellationToken cancellationToken = default)
    {
        var trip = await _context.Trips
            .Include(t => t.Route)
            .Include(t => t.Bus)
            .Include(t => t.Driver).ThenInclude(d => d!.User)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == query.TripId, cancellationToken);

        if (trip == null)
        {
            throw new KeyNotFoundException($"Trip with ID '{query.TripId}' was not found.");
        }

        // Get all boarding events on this trip
        var events = await _context.BoardingEvents
            .Include(b => b.Student).ThenInclude(s => s!.Class)
            .Include(b => b.RfidCard)
            .Include(b => b.RouteStop)
            .Where(b => b.TripId == query.TripId)
            .OrderBy(b => b.ScannedAt)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        // Group events by student
        var studentEvents = events.GroupBy(e => e.StudentId);
        var passengers = new List<PassengerManifestItemDto>();

        int boardedCount = 0;
        int droppedOffCount = 0;

        foreach (var group in studentEvents)
        {
            var student = group.First().Student;
            if (student == null) continue;

            var lastEvent = group.Last();
            var boardingEvent = group.FirstOrDefault(e => e.ScanType == BoardingScanTypes.Boarding);
            var dropOffEvent = group.LastOrDefault(e => e.ScanType == BoardingScanTypes.DropOff);

            string currentStatus;
            if (lastEvent.ScanType == BoardingScanTypes.Boarding)
            {
                currentStatus = "Boarded";
                boardedCount++;
            }
            else if (lastEvent.ScanType == BoardingScanTypes.DropOff)
            {
                currentStatus = "DroppedOff";
                droppedOffCount++;
            }
            else
            {
                currentStatus = "NotBoarded";
            }

            passengers.Add(new PassengerManifestItemDto(
                student.Id,
                student.AdmissionNumber,
                student.FullName,
                student.Class?.Name ?? "Unassigned",
                lastEvent.RfidCard?.CardUid,
                currentStatus,
                boardingEvent?.ScannedAt,
                boardingEvent?.RouteStop?.StopName,
                dropOffEvent?.ScannedAt,
                dropOffEvent?.RouteStop?.StopName));
        }

        var total = passengers.Count;
        var pending = total - (boardedCount + droppedOffCount);

        return new TripPassengerManifestDto(
            trip.Id,
            trip.Route?.Name ?? "Unknown",
            trip.Bus?.BusNumber ?? "Unknown",
            trip.Driver?.User?.FullName ?? "Unknown",
            total,
            boardedCount,
            droppedOffCount,
            Math.Max(0, pending),
            passengers);
    }
}

public sealed record GetUnknownCardTapsQuery(int Take = 50);

public sealed class GetUnknownCardTapsQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetUnknownCardTapsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<UnknownCardTapDto>> HandleAsync(GetUnknownCardTapsQuery query, CancellationToken cancellationToken = default)
    {
        var taps = await _context.UnknownCardTaps
            .OrderByDescending(t => t.ScannedAt)
            .Take(Math.Clamp(query.Take, 1, 200))
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return taps.Select(t => new UnknownCardTapDto(
            t.Id,
            t.SchoolId,
            t.DeviceCode,
            t.CardUid,
            t.Reason,
            t.BusId,
            t.TripId,
            t.ScannedAt)).ToList();
    }
}

