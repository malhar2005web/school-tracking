using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SchoolTrack.Application.Common.Interfaces;
using SchoolTrack.Application.Common.Services;
using SchoolTrack.Domain.Constants;
using SchoolTrack.Domain.Entities;
using SchoolTrack.Domain.Interfaces;
using SchoolTrack.Shared.Constants;
using SchoolTrack.Shared.DTOs.RealTime;
using SchoolTrack.Shared.DTOs.Trips;

namespace SchoolTrack.Application.Features.Trips;

public sealed record CreateTripCommand(
    Guid RouteId,
    Guid BusId,
    Guid DriverId,
    DateOnly TripDate,
    string Shift,
    DateTimeOffset? ScheduledStartAt = null);

public sealed class CreateTripCommandValidator : AbstractValidator<CreateTripCommand>
{
    public CreateTripCommandValidator()
    {
        RuleFor(x => x.RouteId).NotEmpty();
        RuleFor(x => x.BusId).NotEmpty();
        RuleFor(x => x.DriverId).NotEmpty();
        RuleFor(x => x.Shift).Must(s => TripShifts.All.Contains(s))
            .WithMessage($"Shift must be one of: {string.Join(", ", TripShifts.All)}");
    }
}

public sealed class CreateTripCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateTripCommandHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<TripDto> HandleAsync(CreateTripCommand command, CancellationToken cancellationToken = default)
    {
        var schoolId = _tenantContext.SchoolId
            ?? throw new UnauthorizedAccessException("Active school tenant context is required.");

        var route = await _context.Routes
            .Include(r => r.Stops.OrderBy(s => s.SequenceOrder))
            .FirstOrDefaultAsync(r => r.Id == command.RouteId && r.SchoolId == schoolId, cancellationToken);
        if (route == null) throw new KeyNotFoundException($"Route with ID '{command.RouteId}' was not found.");

        var bus = await _context.Buses
            .FirstOrDefaultAsync(b => b.Id == command.BusId && b.SchoolId == schoolId, cancellationToken);
        if (bus == null) throw new KeyNotFoundException($"Bus with ID '{command.BusId}' was not found.");

        var driver = await _context.Drivers
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == command.DriverId && d.SchoolId == schoolId, cancellationToken);
        if (driver == null) throw new KeyNotFoundException($"Driver with ID '{command.DriverId}' was not found.");

        var tripId = Guid.CreateVersion7();
        var trip = new Trip
        {
            Id = tripId,
            SchoolId = schoolId,
            RouteId = command.RouteId,
            BusId = command.BusId,
            DriverId = command.DriverId,
            TripDate = command.TripDate,
            Shift = command.Shift,
            Status = TripStatuses.Scheduled,
            ScheduledStartAt = command.ScheduledStartAt
        };

        // Instantiate trip stops from route stops
        foreach (var routeStop in route.Stops.OrderBy(s => s.SequenceOrder))
        {
            var estimatedArrival = command.ScheduledStartAt?.AddMinutes(routeStop.ScheduledOffsetMinutes);

            var tripStop = new TripStop
            {
                Id = Guid.CreateVersion7(),
                SchoolId = schoolId,
                TripId = tripId,
                RouteStopId = routeStop.Id,
                SequenceOrder = routeStop.SequenceOrder,
                Status = TripStopStatuses.Pending,
                EstimatedArrivalAt = estimatedArrival
            };

            trip.Stops.Add(tripStop);
        }

        var userId = _currentUser.UserId ?? Guid.Empty;
        var initialHistory = new TripStatusHistory
        {
            Id = Guid.CreateVersion7(),
            SchoolId = schoolId,
            TripId = tripId,
            FromStatus = string.Empty,
            ToStatus = TripStatuses.Scheduled,
            ChangedByUserId = userId,
            Notes = "Trip scheduled.",
            ChangedAt = _dateTimeProvider.UtcNow
        };
        trip.StatusHistory.Add(initialHistory);

        _context.Trips.Add(trip);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(trip, route.Name, bus.BusNumber, driver.User?.FullName ?? "Unknown");
    }

    private static TripDto MapToDto(Trip trip, string routeName, string busNumber, string driverName)
    {
        return new TripDto(
            trip.Id,
            trip.SchoolId,
            trip.RouteId,
            routeName,
            trip.BusId,
            busNumber,
            trip.DriverId,
            driverName,
            trip.TripDate,
            trip.Shift,
            trip.Status,
            trip.ScheduledStartAt,
            trip.ActualStartAt,
            trip.ActualEndAt,
            trip.CancellationReason,
            trip.Stops.OrderBy(s => s.SequenceOrder).Select(s => new TripStopDto(
                s.Id,
                s.TripId,
                s.RouteStopId,
                s.RouteStop?.StopName ?? $"Stop #{s.SequenceOrder}",
                s.SequenceOrder,
                s.RouteStop?.Latitude ?? 0m,
                s.RouteStop?.Longitude ?? 0m,
                s.Status,
                s.EstimatedArrivalAt,
                s.ActualArrivalAt,
                s.ActualDepartureAt)).ToList());
    }
}

public sealed record UpdateTripStatusCommand(
    Guid TripId,
    string TargetStatus,
    string? Notes = null);

public sealed class UpdateTripStatusCommandValidator : AbstractValidator<UpdateTripStatusCommand>
{
    public UpdateTripStatusCommandValidator()
    {
        RuleFor(x => x.TripId).NotEmpty();
        RuleFor(x => x.TargetStatus).Must(s => TripStatuses.All.Contains(s))
            .WithMessage($"Target status must be one of: {string.Join(", ", TripStatuses.All)}");
    }
}

public sealed class UpdateTripStatusCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly ICurrentUser _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ITrackingBroadcaster _broadcaster;

    public UpdateTripStatusCommandHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        ICurrentUser currentUser,
        IDateTimeProvider dateTimeProvider,
        ITrackingBroadcaster? broadcaster = null)
    {
        _context = context;
        _tenantContext = tenantContext;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
        _broadcaster = broadcaster ?? new NullTrackingBroadcaster();
    }

    public async Task<TripDto> HandleAsync(UpdateTripStatusCommand command, CancellationToken cancellationToken = default)
    {
        var schoolId = _tenantContext.SchoolId
            ?? throw new UnauthorizedAccessException("Active school tenant context is required.");

        var trip = await _context.Trips
            .Include(t => t.Route)
            .Include(t => t.Bus)
            .Include(t => t.Driver).ThenInclude(d => d!.User)
            .Include(t => t.Stops.OrderBy(s => s.SequenceOrder)).ThenInclude(s => s.RouteStop)
            .Include(t => t.StatusHistory)
            .FirstOrDefaultAsync(t => t.Id == command.TripId && t.SchoolId == schoolId, cancellationToken);

        if (trip == null)
        {
            throw new KeyNotFoundException($"Trip with ID '{command.TripId}' was not found.");
        }

        // Driver scoping check: Driver can only transition their own assigned trip
        if (_currentUser.Role == SystemRoles.Driver)
        {
            var driver = await _context.Drivers
                .FirstOrDefaultAsync(d => d.UserId == _currentUser.UserId && d.SchoolId == schoolId, cancellationToken);

            if (driver == null || driver.Id != trip.DriverId)
            {
                throw new UnauthorizedAccessException("Drivers can only update trips assigned to them.");
            }
        }

        var userId = _currentUser.UserId ?? Guid.Empty;
        var previousStatus = trip.Status;
        trip.TransitionTo(command.TargetStatus, userId, command.Notes, _dateTimeProvider.UtcNow);

        await _context.SaveChangesAsync(cancellationToken);

        await _broadcaster.BroadcastTripStatusChangedAsync(
            new TripStatusChangedDto(
                trip.Id,
                trip.SchoolId,
                trip.RouteId,
                trip.Route?.Name ?? "Unknown",
                trip.BusId,
                trip.Bus?.BusNumber ?? "Unknown",
                trip.DriverId,
                trip.Driver?.User?.FullName ?? "Unknown",
                previousStatus,
                trip.Status,
                _dateTimeProvider.UtcNow,
                command.Notes),
            cancellationToken);

        return new TripDto(
            trip.Id,
            trip.SchoolId,
            trip.RouteId,
            trip.Route?.Name ?? "Unknown",
            trip.BusId,
            trip.Bus?.BusNumber ?? "Unknown",
            trip.DriverId,
            trip.Driver?.User?.FullName ?? "Unknown",
            trip.TripDate,
            trip.Shift,
            trip.Status,
            trip.ScheduledStartAt,
            trip.ActualStartAt,
            trip.ActualEndAt,
            trip.CancellationReason,
            trip.Stops.OrderBy(s => s.SequenceOrder).Select(s => new TripStopDto(
                s.Id,
                s.TripId,
                s.RouteStopId,
                s.RouteStop?.StopName ?? $"Stop #{s.SequenceOrder}",
                s.SequenceOrder,
                s.RouteStop?.Latitude ?? 0m,
                s.RouteStop?.Longitude ?? 0m,
                s.Status,
                s.EstimatedArrivalAt,
                s.ActualArrivalAt,
                s.ActualDepartureAt)).ToList());
    }
}

public sealed record UpdateTripStopStatusCommand(
    Guid TripId,
    Guid StopId,
    string Status);

public sealed class UpdateTripStopStatusCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ITrackingBroadcaster _broadcaster;

    public UpdateTripStopStatusCommandHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        IDateTimeProvider dateTimeProvider,
        ITrackingBroadcaster? broadcaster = null)
    {
        _context = context;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
        _broadcaster = broadcaster ?? new NullTrackingBroadcaster();
    }

    public async Task HandleAsync(UpdateTripStopStatusCommand command, CancellationToken cancellationToken = default)
    {
        var schoolId = _tenantContext.SchoolId
            ?? throw new UnauthorizedAccessException("Active school tenant context is required.");

        var tripStop = await _context.TripStops
            .Include(ts => ts.RouteStop)
            .FirstOrDefaultAsync(ts => ts.Id == command.StopId && ts.TripId == command.TripId && ts.SchoolId == schoolId, cancellationToken);

        if (tripStop == null)
        {
            throw new KeyNotFoundException($"Trip stop with ID '{command.StopId}' was not found on this trip.");
        }

        var utcNow = _dateTimeProvider.UtcNow;
        switch (command.Status)
        {
            case TripStopStatuses.Arrived:
                tripStop.MarkArrived(utcNow);
                break;
            case TripStopStatuses.Departed:
                tripStop.MarkDeparted(utcNow);
                break;
            case TripStopStatuses.Skipped:
                tripStop.MarkSkipped();
                break;
            default:
                throw new ArgumentException($"Invalid trip stop status '{command.Status}'.");
        }

        await _context.SaveChangesAsync(cancellationToken);

        await _broadcaster.BroadcastTripStopStatusChangedAsync(
            new TripStopStatusChangedDto(
                command.TripId,
                schoolId,
                tripStop.Id,
                tripStop.RouteStop?.StopName ?? $"Stop #{tripStop.SequenceOrder}",
                tripStop.SequenceOrder,
                command.Status,
                utcNow),
            cancellationToken);
    }
}

public sealed record GetTripsQuery(
    DateOnly? TripDate = null,
    string? Shift = null,
    string? Status = null,
    Guid? BusId = null,
    Guid? DriverId = null);

public sealed class GetTripsQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetTripsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<TripDto>> HandleAsync(GetTripsQuery query, CancellationToken cancellationToken = default)
    {
        var tripsQuery = _context.Trips
            .Include(t => t.Route)
            .Include(t => t.Bus)
            .Include(t => t.Driver).ThenInclude(d => d!.User)
            .Include(t => t.Stops.OrderBy(s => s.SequenceOrder)).ThenInclude(s => s.RouteStop)
            .AsNoTracking();

        if (query.TripDate.HasValue) tripsQuery = tripsQuery.Where(t => t.TripDate == query.TripDate.Value);
        if (!string.IsNullOrWhiteSpace(query.Shift)) tripsQuery = tripsQuery.Where(t => t.Shift == query.Shift);
        if (!string.IsNullOrWhiteSpace(query.Status)) tripsQuery = tripsQuery.Where(t => t.Status == query.Status);
        if (query.BusId.HasValue) tripsQuery = tripsQuery.Where(t => t.BusId == query.BusId.Value);
        if (query.DriverId.HasValue) tripsQuery = tripsQuery.Where(t => t.DriverId == query.DriverId.Value);

        var trips = await tripsQuery
            .OrderByDescending(t => t.TripDate).ThenBy(t => t.ScheduledStartAt)
            .ToListAsync(cancellationToken);

        return trips.Select(t => new TripDto(
            t.Id,
            t.SchoolId,
            t.RouteId,
            t.Route?.Name ?? "Unknown",
            t.BusId,
            t.Bus?.BusNumber ?? "Unknown",
            t.DriverId,
            t.Driver?.User?.FullName ?? "Unknown",
            t.TripDate,
            t.Shift,
            t.Status,
            t.ScheduledStartAt,
            t.ActualStartAt,
            t.ActualEndAt,
            t.CancellationReason,
            t.Stops.OrderBy(s => s.SequenceOrder).Select(s => new TripStopDto(
                s.Id,
                s.TripId,
                s.RouteStopId,
                s.RouteStop?.StopName ?? $"Stop #{s.SequenceOrder}",
                s.SequenceOrder,
                s.RouteStop?.Latitude ?? 0m,
                s.RouteStop?.Longitude ?? 0m,
                s.Status,
                s.EstimatedArrivalAt,
                s.ActualArrivalAt,
                s.ActualDepartureAt)).ToList())).ToList();
    }
}

public sealed record GetTripByIdQuery(Guid TripId);

public sealed class GetTripByIdQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetTripByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TripDto> HandleAsync(GetTripByIdQuery query, CancellationToken cancellationToken = default)
    {
        var trip = await _context.Trips
            .Include(t => t.Route)
            .Include(t => t.Bus)
            .Include(t => t.Driver).ThenInclude(d => d!.User)
            .Include(t => t.Stops.OrderBy(s => s.SequenceOrder)).ThenInclude(s => s.RouteStop)
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == query.TripId, cancellationToken);

        if (trip == null)
        {
            throw new KeyNotFoundException($"Trip with ID '{query.TripId}' was not found.");
        }

        return new TripDto(
            trip.Id,
            trip.SchoolId,
            trip.RouteId,
            trip.Route?.Name ?? "Unknown",
            trip.BusId,
            trip.Bus?.BusNumber ?? "Unknown",
            trip.DriverId,
            trip.Driver?.User?.FullName ?? "Unknown",
            trip.TripDate,
            trip.Shift,
            trip.Status,
            trip.ScheduledStartAt,
            trip.ActualStartAt,
            trip.ActualEndAt,
            trip.CancellationReason,
            trip.Stops.OrderBy(s => s.SequenceOrder).Select(s => new TripStopDto(
                s.Id,
                s.TripId,
                s.RouteStopId,
                s.RouteStop?.StopName ?? $"Stop #{s.SequenceOrder}",
                s.SequenceOrder,
                s.RouteStop?.Latitude ?? 0m,
                s.RouteStop?.Longitude ?? 0m,
                s.Status,
                s.EstimatedArrivalAt,
                s.ActualArrivalAt,
                s.ActualDepartureAt)).ToList());
    }
}

