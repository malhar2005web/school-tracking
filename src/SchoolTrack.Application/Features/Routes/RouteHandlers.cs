using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SchoolTrack.Application.Common.Interfaces;
using SchoolTrack.Domain.Constants;
using SchoolTrack.Domain.Entities;
using SchoolTrack.Domain.Interfaces;
using SchoolTrack.Shared.DTOs.Routes;

namespace SchoolTrack.Application.Features.Routes;

public sealed record CreateRouteCommand(
    string Name,
    string Code,
    string DirectionType = RouteDirectionTypes.Both);

public sealed class CreateRouteCommandValidator : AbstractValidator<CreateRouteCommand>
{
    public CreateRouteCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Code).NotEmpty().MaximumLength(50);
        RuleFor(x => x.DirectionType).Must(d => RouteDirectionTypes.All.Contains(d))
            .WithMessage($"Direction must be one of: {string.Join(", ", RouteDirectionTypes.All)}");
    }
}

public sealed class CreateRouteCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public CreateRouteCommandHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<RouteDto> HandleAsync(CreateRouteCommand command, CancellationToken cancellationToken = default)
    {
        var schoolId = _tenantContext.SchoolId
            ?? throw new UnauthorizedAccessException("Active school tenant context is required.");

        var code = command.Code.Trim().ToUpperInvariant();

        var exists = await _context.Routes
            .AnyAsync(r => r.SchoolId == schoolId && r.Code == code, cancellationToken);
        if (exists)
        {
            throw new ArgumentException($"A route with code '{code}' already exists in this school.");
        }

        var route = new Route
        {
            Id = Guid.CreateVersion7(),
            SchoolId = schoolId,
            Name = command.Name.Trim(),
            Code = code,
            DirectionType = command.DirectionType.Trim(),
            IsActive = true
        };

        _context.Routes.Add(route);
        await _context.SaveChangesAsync(cancellationToken);

        return new RouteDto(route.Id, route.SchoolId, route.Name, route.Code, route.DirectionType, route.IsActive, Array.Empty<RouteStopDto>());
    }
}

public sealed record AddRouteStopCommand(
    Guid RouteId,
    string StopName,
    int SequenceOrder,
    decimal Latitude,
    decimal Longitude,
    int ScheduledOffsetMinutes,
    int GeofenceRadiusMeters = 50);

public sealed class AddRouteStopCommandValidator : AbstractValidator<AddRouteStopCommand>
{
    public AddRouteStopCommandValidator()
    {
        RuleFor(x => x.RouteId).NotEmpty();
        RuleFor(x => x.StopName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.SequenceOrder).GreaterThan(0);
        RuleFor(x => x.Latitude).InclusiveBetween(-90m, 90m);
        RuleFor(x => x.Longitude).InclusiveBetween(-180m, 180m);
        RuleFor(x => x.ScheduledOffsetMinutes).GreaterThanOrEqualTo(0);
        RuleFor(x => x.GeofenceRadiusMeters).InclusiveBetween(10, 500);
    }
}

public sealed class AddRouteStopCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public AddRouteStopCommandHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<RouteStopDto> HandleAsync(AddRouteStopCommand command, CancellationToken cancellationToken = default)
    {
        var schoolId = _tenantContext.SchoolId
            ?? throw new UnauthorizedAccessException("Active school tenant context is required.");

        var route = await _context.Routes
            .FirstOrDefaultAsync(r => r.Id == command.RouteId && r.SchoolId == schoolId, cancellationToken);
        if (route == null)
        {
            throw new KeyNotFoundException($"Route with ID '{command.RouteId}' was not found.");
        }

        var seqExists = await _context.RouteStops
            .AnyAsync(s => s.RouteId == command.RouteId && s.SequenceOrder == command.SequenceOrder, cancellationToken);
        if (seqExists)
        {
            throw new ArgumentException($"Stop sequence order {command.SequenceOrder} already exists on this route.");
        }

        var stop = new RouteStop
        {
            Id = Guid.CreateVersion7(),
            SchoolId = schoolId,
            RouteId = command.RouteId,
            StopName = command.StopName.Trim(),
            SequenceOrder = command.SequenceOrder,
            Latitude = command.Latitude,
            Longitude = command.Longitude,
            ScheduledOffsetMinutes = command.ScheduledOffsetMinutes,
            GeofenceRadiusMeters = command.GeofenceRadiusMeters
        };

        _context.RouteStops.Add(stop);
        await _context.SaveChangesAsync(cancellationToken);

        return new RouteStopDto(
            stop.Id,
            stop.RouteId,
            stop.StopName,
            stop.SequenceOrder,
            stop.Latitude,
            stop.Longitude,
            stop.ScheduledOffsetMinutes,
            stop.GeofenceRadiusMeters);
    }
}

public sealed record CreateRouteAssignmentCommand(
    Guid RouteId,
    Guid BusId,
    Guid DriverId,
    string Shift,
    DateTimeOffset EffectiveFrom,
    DateTimeOffset? EffectiveTo = null);

public sealed class CreateRouteAssignmentCommandValidator : AbstractValidator<CreateRouteAssignmentCommand>
{
    public CreateRouteAssignmentCommandValidator()
    {
        RuleFor(x => x.RouteId).NotEmpty();
        RuleFor(x => x.BusId).NotEmpty();
        RuleFor(x => x.DriverId).NotEmpty();
        RuleFor(x => x.Shift).Must(s => TripShifts.All.Contains(s))
            .WithMessage($"Shift must be one of: {string.Join(", ", TripShifts.All)}");
    }
}

public sealed class CreateRouteAssignmentCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public CreateRouteAssignmentCommandHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<RouteAssignmentDto> HandleAsync(CreateRouteAssignmentCommand command, CancellationToken cancellationToken = default)
    {
        var schoolId = _tenantContext.SchoolId
            ?? throw new UnauthorizedAccessException("Active school tenant context is required.");

        var route = await _context.Routes
            .FirstOrDefaultAsync(r => r.Id == command.RouteId && r.SchoolId == schoolId, cancellationToken);
        if (route == null) throw new KeyNotFoundException($"Route with ID '{command.RouteId}' was not found.");

        var bus = await _context.Buses
            .FirstOrDefaultAsync(b => b.Id == command.BusId && b.SchoolId == schoolId, cancellationToken);
        if (bus == null) throw new KeyNotFoundException($"Bus with ID '{command.BusId}' was not found.");

        var driver = await _context.Drivers
            .Include(d => d.User)
            .FirstOrDefaultAsync(d => d.Id == command.DriverId && d.SchoolId == schoolId, cancellationToken);
        if (driver == null) throw new KeyNotFoundException($"Driver with ID '{command.DriverId}' was not found.");

        var assignment = new RouteAssignment
        {
            Id = Guid.CreateVersion7(),
            SchoolId = schoolId,
            RouteId = command.RouteId,
            BusId = command.BusId,
            DriverId = command.DriverId,
            Shift = command.Shift,
            EffectiveFrom = command.EffectiveFrom,
            EffectiveTo = command.EffectiveTo,
            IsActive = true
        };

        _context.RouteAssignments.Add(assignment);
        await _context.SaveChangesAsync(cancellationToken);

        return new RouteAssignmentDto(
            assignment.Id,
            assignment.SchoolId,
            assignment.RouteId,
            route.Name,
            assignment.BusId,
            bus.BusNumber,
            assignment.DriverId,
            driver.User?.FullName ?? "Unknown",
            assignment.Shift,
            assignment.EffectiveFrom,
            assignment.EffectiveTo,
            assignment.IsActive);
    }
}

public sealed record GetRoutesQuery;

public sealed class GetRoutesQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetRoutesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<RouteDto>> HandleAsync(GetRoutesQuery query, CancellationToken cancellationToken = default)
    {
        var routes = await _context.Routes
            .Include(r => r.Stops.OrderBy(s => s.SequenceOrder))
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        return routes.Select(r => new RouteDto(
            r.Id,
            r.SchoolId,
            r.Name,
            r.Code,
            r.DirectionType,
            r.IsActive,
            r.Stops.OrderBy(s => s.SequenceOrder).Select(s => new RouteStopDto(
                s.Id,
                s.RouteId,
                s.StopName,
                s.SequenceOrder,
                s.Latitude,
                s.Longitude,
                s.ScheduledOffsetMinutes,
                s.GeofenceRadiusMeters)).ToList())).ToList();
    }
}

public sealed record GetRouteByIdQuery(Guid RouteId);

public sealed class GetRouteByIdQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetRouteByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RouteDto> HandleAsync(GetRouteByIdQuery query, CancellationToken cancellationToken = default)
    {
        var route = await _context.Routes
            .Include(r => r.Stops.OrderBy(s => s.SequenceOrder))
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == query.RouteId, cancellationToken);

        if (route == null)
        {
            throw new KeyNotFoundException($"Route with ID '{query.RouteId}' was not found.");
        }

        return new RouteDto(
            route.Id,
            route.SchoolId,
            route.Name,
            route.Code,
            route.DirectionType,
            route.IsActive,
            route.Stops.OrderBy(s => s.SequenceOrder).Select(s => new RouteStopDto(
                s.Id,
                s.RouteId,
                s.StopName,
                s.SequenceOrder,
                s.Latitude,
                s.Longitude,
                s.ScheduledOffsetMinutes,
                s.GeofenceRadiusMeters)).ToList());
    }
}

