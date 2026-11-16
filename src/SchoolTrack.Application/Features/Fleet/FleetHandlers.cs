using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SchoolTrack.Application.Common.Interfaces;
using SchoolTrack.Domain.Entities;
using SchoolTrack.Domain.Interfaces;
using SchoolTrack.Shared.DTOs.Fleet;

namespace SchoolTrack.Application.Features.Fleet;

public sealed record CreateBusCommand(
    string BusNumber,
    string RegistrationNumber,
    string? Model = null,
    int Capacity = 30,
    DateTimeOffset? InsuranceExpiry = null,
    DateTimeOffset? FitnessExpiry = null);

public sealed class CreateBusCommandValidator : AbstractValidator<CreateBusCommand>
{
    public CreateBusCommandValidator()
    {
        RuleFor(x => x.BusNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.RegistrationNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Capacity).InclusiveBetween(5, 120);
    }
}

public sealed class CreateBusCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public CreateBusCommandHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<BusDto> HandleAsync(CreateBusCommand command, CancellationToken cancellationToken = default)
    {
        var schoolId = _tenantContext.SchoolId
            ?? throw new UnauthorizedAccessException("Active school tenant context is required.");

        var normalizedRegNo = command.RegistrationNumber.Trim().ToUpperInvariant();

        var regExists = await _context.Buses
            .AnyAsync(b => b.SchoolId == schoolId && b.RegistrationNumber == normalizedRegNo, cancellationToken);
        if (regExists)
        {
            throw new ArgumentException($"A bus with registration number '{normalizedRegNo}' already exists in this school.");
        }

        var bus = new Bus
        {
            Id = Guid.CreateVersion7(),
            SchoolId = schoolId,
            BusNumber = command.BusNumber.Trim(),
            RegistrationNumber = normalizedRegNo,
            Model = command.Model?.Trim(),
            Capacity = command.Capacity,
            MaintenanceStatus = "Active",
            InsuranceExpiry = command.InsuranceExpiry,
            FitnessExpiry = command.FitnessExpiry,
            IsActive = true
        };

        _context.Buses.Add(bus);
        await _context.SaveChangesAsync(cancellationToken);

        return new BusDto(
            bus.Id,
            bus.SchoolId,
            bus.BusNumber,
            bus.RegistrationNumber,
            bus.Model,
            bus.Capacity,
            bus.MaintenanceStatus,
            bus.InsuranceExpiry,
            bus.FitnessExpiry,
            bus.IsActive,
            null,
            null,
            null,
            null);
    }
}

public sealed record AssignDriverCommand(
    Guid BusId,
    Guid DriverId);

public sealed class AssignDriverCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AssignDriverCommandHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task HandleAsync(AssignDriverCommand command, CancellationToken cancellationToken = default)
    {
        var schoolId = _tenantContext.SchoolId
            ?? throw new UnauthorizedAccessException("Active school tenant context is required.");

        var bus = await _context.Buses
            .FirstOrDefaultAsync(b => b.Id == command.BusId && b.SchoolId == schoolId, cancellationToken);
        if (bus == null)
        {
            throw new KeyNotFoundException($"Bus with ID '{command.BusId}' was not found.");
        }

        var driver = await _context.Drivers
            .FirstOrDefaultAsync(d => d.Id == command.DriverId && d.SchoolId == schoolId, cancellationToken);
        if (driver == null)
        {
            throw new KeyNotFoundException($"Driver with ID '{command.DriverId}' was not found.");
        }

        // Close any currently active assignments for this bus or driver
        var currentBusAssignments = await _context.BusAssignments
            .Where(ba => (ba.BusId == command.BusId || ba.DriverId == command.DriverId) && ba.IsCurrent)
            .ToListAsync(cancellationToken);

        foreach (var assignment in currentBusAssignments)
        {
            assignment.IsCurrent = false;
            assignment.AssignedTo = _dateTimeProvider.UtcNow;
        }

        var newAssignment = new BusAssignment
        {
            Id = Guid.CreateVersion7(),
            SchoolId = schoolId,
            BusId = command.BusId,
            DriverId = command.DriverId,
            AssignedFrom = _dateTimeProvider.UtcNow,
            IsCurrent = true
        };

        _context.BusAssignments.Add(newAssignment);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public sealed record GetBusesQuery;

public sealed class GetBusesQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetBusesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<BusDto>> HandleAsync(GetBusesQuery query, CancellationToken cancellationToken = default)
    {
        var buses = await _context.Buses
            .Include(b => b.GpsDevice)
            .Include(b => b.RfidDevice)
            .Include(b => b.Assignments.Where(a => a.IsCurrent))
                .ThenInclude(a => a.Driver)
                    .ThenInclude(d => d!.User)
            .AsNoTracking()
            .OrderBy(b => b.BusNumber)
            .ToListAsync(cancellationToken);

        return buses.Select(b =>
        {
            var activeAssignment = b.Assignments.FirstOrDefault(a => a.IsCurrent);
            return new BusDto(
                b.Id,
                b.SchoolId,
                b.BusNumber,
                b.RegistrationNumber,
                b.Model,
                b.Capacity,
                b.MaintenanceStatus,
                b.InsuranceExpiry,
                b.FitnessExpiry,
                b.IsActive,
                activeAssignment?.DriverId,
                activeAssignment?.Driver?.User?.FullName,
                b.GpsDevice?.DeviceCode,
                b.RfidDevice?.DeviceCode);
        }).ToList();
    }
}

