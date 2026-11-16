using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SchoolTrack.Application.Common.Interfaces;
using SchoolTrack.Domain.Entities;
using SchoolTrack.Domain.Interfaces;
using SchoolTrack.Shared.DTOs.Devices;

namespace SchoolTrack.Application.Features.Devices;

public sealed record CreateRfidCardCommand(
    string CardUid,
    string CardType = "Permanent",
    Guid? StudentId = null);

public sealed class CreateRfidCardCommandValidator : AbstractValidator<CreateRfidCardCommand>
{
    public CreateRfidCardCommandValidator()
    {
        RuleFor(x => x.CardUid).NotEmpty().MaximumLength(50);
    }
}

public sealed class CreateRfidCardCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateRfidCardCommandHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _tenantContext = tenantContext;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<RfidCardDto> HandleAsync(CreateRfidCardCommand command, CancellationToken cancellationToken = default)
    {
        var schoolId = _tenantContext.SchoolId
            ?? throw new UnauthorizedAccessException("Active school tenant context is required.");

        var normalizedUid = command.CardUid.Trim().ToUpperInvariant();

        var uidExists = await _context.RfidCards
            .AnyAsync(c => c.SchoolId == schoolId && c.CardUid == normalizedUid, cancellationToken);
        if (uidExists)
        {
            throw new ArgumentException($"An RFID card with UID '{normalizedUid}' already exists in this school.");
        }

        string? studentName = null;
        if (command.StudentId.HasValue)
        {
            var student = await _context.Students
                .FirstOrDefaultAsync(s => s.Id == command.StudentId.Value && s.SchoolId == schoolId, cancellationToken);
            if (student == null)
            {
                throw new KeyNotFoundException($"Student with ID '{command.StudentId.Value}' was not found.");
            }
            studentName = $"{student.FirstName} {student.LastName}";
        }

        var card = new RfidCard
        {
            Id = Guid.CreateVersion7(),
            SchoolId = schoolId,
            CardUid = normalizedUid,
            CardType = command.CardType.Trim(),
            StudentId = command.StudentId,
            Status = command.StudentId.HasValue ? "Active" : "Unassigned",
            IssuedAt = _dateTimeProvider.UtcNow
        };

        _context.RfidCards.Add(card);
        await _context.SaveChangesAsync(cancellationToken);

        return new RfidCardDto(
            card.Id,
            card.SchoolId,
            card.CardUid,
            card.StudentId,
            studentName,
            card.CardType,
            card.Status,
            card.IssuedAt,
            card.ExpiresAt);
    }
}

public sealed record AssignRfidCardCommand(
    Guid CardId,
    Guid StudentId);

public sealed class AssignRfidCardCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public AssignRfidCardCommandHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task HandleAsync(AssignRfidCardCommand command, CancellationToken cancellationToken = default)
    {
        var schoolId = _tenantContext.SchoolId
            ?? throw new UnauthorizedAccessException("Active school tenant context is required.");

        var card = await _context.RfidCards
            .FirstOrDefaultAsync(c => c.Id == command.CardId && c.SchoolId == schoolId, cancellationToken);
        if (card == null)
        {
            throw new KeyNotFoundException($"RFID card with ID '{command.CardId}' was not found.");
        }

        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.Id == command.StudentId && s.SchoolId == schoolId, cancellationToken);
        if (student == null)
        {
            throw new KeyNotFoundException($"Student with ID '{command.StudentId}' was not found.");
        }

        // Unlink any card previously assigned to this student
        var existingAssignedCards = await _context.RfidCards
            .Where(c => c.StudentId == command.StudentId && c.Id != command.CardId)
            .ToListAsync(cancellationToken);

        foreach (var oldCard in existingAssignedCards)
        {
            oldCard.StudentId = null;
            oldCard.Status = "Unassigned";
        }

        card.StudentId = command.StudentId;
        card.Status = "Active";

        await _context.SaveChangesAsync(cancellationToken);
    }
}

public sealed record RegisterGpsDeviceCommand(
    string DeviceCode,
    Guid? BusId = null,
    string? Imei = null,
    string? Model = null,
    string? SimCardNumber = null,
    string? ProtocolType = "Simulator");

public sealed class RegisterGpsDeviceCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public RegisterGpsDeviceCommandHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<GpsDeviceDto> HandleAsync(RegisterGpsDeviceCommand command, CancellationToken cancellationToken = default)
    {
        var schoolId = _tenantContext.SchoolId
            ?? throw new UnauthorizedAccessException("Active school tenant context is required.");

        var code = command.DeviceCode.Trim().ToUpperInvariant();

        var codeExists = await _context.GpsDevices
            .AnyAsync(g => g.SchoolId == schoolId && g.DeviceCode == code, cancellationToken);
        if (codeExists)
        {
            throw new ArgumentException($"A GPS device with code '{code}' already exists in this school.");
        }

        string? busNumber = null;
        if (command.BusId.HasValue)
        {
            var bus = await _context.Buses
                .FirstOrDefaultAsync(b => b.Id == command.BusId.Value && b.SchoolId == schoolId, cancellationToken);
            if (bus == null)
            {
                throw new KeyNotFoundException($"Bus with ID '{command.BusId.Value}' was not found.");
            }
            busNumber = bus.BusNumber;
        }

        var gpsDevice = new GpsDevice
        {
            Id = Guid.CreateVersion7(),
            SchoolId = schoolId,
            DeviceCode = code,
            BusId = command.BusId,
            Imei = command.Imei?.Trim(),
            Model = command.Model?.Trim(),
            SimCardNumber = command.SimCardNumber?.Trim(),
            ProtocolType = string.IsNullOrWhiteSpace(command.ProtocolType) ? "Simulator" : command.ProtocolType.Trim(),
            Status = "Provisioning"
        };

        _context.GpsDevices.Add(gpsDevice);
        await _context.SaveChangesAsync(cancellationToken);

        return new GpsDeviceDto(
            gpsDevice.Id,
            gpsDevice.SchoolId,
            gpsDevice.DeviceCode,
            gpsDevice.BusId,
            busNumber,
            gpsDevice.Imei,
            gpsDevice.Model,
            gpsDevice.ProtocolType,
            gpsDevice.Status,
            gpsDevice.LastSeenAt,
            gpsDevice.LastLocationAt);
    }
}

public sealed record RegisterRfidDeviceCommand(
    string DeviceCode,
    Guid? BusId = null,
    string? GateLocation = null,
    string? Model = null,
    string? IpAddress = null,
    int? Port = null,
    string? ProtocolType = "Simulator");

public sealed class RegisterRfidDeviceCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public RegisterRfidDeviceCommandHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<RfidDeviceDto> HandleAsync(RegisterRfidDeviceCommand command, CancellationToken cancellationToken = default)
    {
        var schoolId = _tenantContext.SchoolId
            ?? throw new UnauthorizedAccessException("Active school tenant context is required.");

        var code = command.DeviceCode.Trim().ToUpperInvariant();

        var codeExists = await _context.RfidDevices
            .AnyAsync(r => r.SchoolId == schoolId && r.DeviceCode == code, cancellationToken);
        if (codeExists)
        {
            throw new ArgumentException($"An RFID device with code '{code}' already exists in this school.");
        }

        string? busNumber = null;
        if (command.BusId.HasValue)
        {
            var bus = await _context.Buses
                .FirstOrDefaultAsync(b => b.Id == command.BusId.Value && b.SchoolId == schoolId, cancellationToken);
            if (bus == null)
            {
                throw new KeyNotFoundException($"Bus with ID '{command.BusId.Value}' was not found.");
            }
            busNumber = bus.BusNumber;
        }

        var rfidDevice = new RfidDevice
        {
            Id = Guid.CreateVersion7(),
            SchoolId = schoolId,
            DeviceCode = code,
            BusId = command.BusId,
            GateLocation = command.GateLocation?.Trim(),
            Model = command.Model?.Trim(),
            IpAddress = command.IpAddress?.Trim(),
            Port = command.Port,
            ProtocolType = string.IsNullOrWhiteSpace(command.ProtocolType) ? "Simulator" : command.ProtocolType.Trim(),
            Status = "Provisioning"
        };

        _context.RfidDevices.Add(rfidDevice);
        await _context.SaveChangesAsync(cancellationToken);

        return new RfidDeviceDto(
            rfidDevice.Id,
            rfidDevice.SchoolId,
            rfidDevice.DeviceCode,
            rfidDevice.BusId,
            busNumber,
            rfidDevice.GateLocation,
            rfidDevice.Model,
            rfidDevice.IpAddress,
            rfidDevice.Port,
            rfidDevice.ProtocolType,
            rfidDevice.Status,
            rfidDevice.LastSeenAt,
            rfidDevice.LastScanAt);
    }
}

public sealed record GetRfidCardsQuery;

public sealed class GetRfidCardsQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetRfidCardsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<RfidCardDto>> HandleAsync(GetRfidCardsQuery query, CancellationToken cancellationToken = default)
    {
        var cards = await _context.RfidCards
            .Include(c => c.Student)
            .AsNoTracking()
            .OrderBy(c => c.CardUid)
            .ToListAsync(cancellationToken);

        return cards.Select(c => new RfidCardDto(
            c.Id,
            c.SchoolId,
            c.CardUid,
            c.StudentId,
            c.Student != null ? $"{c.Student.FirstName} {c.Student.LastName}" : null,
            c.CardType,
            c.Status,
            c.IssuedAt,
            c.ExpiresAt)).ToList();
    }
}

public sealed record GetGpsDevicesQuery;

public sealed class GetGpsDevicesQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetGpsDevicesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<GpsDeviceDto>> HandleAsync(GetGpsDevicesQuery query, CancellationToken cancellationToken = default)
    {
        var devices = await _context.GpsDevices
            .Include(g => g.Bus)
            .AsNoTracking()
            .OrderBy(g => g.DeviceCode)
            .ToListAsync(cancellationToken);

        return devices.Select(g => new GpsDeviceDto(
            g.Id,
            g.SchoolId,
            g.DeviceCode,
            g.BusId,
            g.Bus?.BusNumber,
            g.Imei,
            g.Model,
            g.ProtocolType,
            g.Status,
            g.LastSeenAt,
            g.LastLocationAt)).ToList();
    }
}

public sealed record GetRfidDevicesQuery;

public sealed class GetRfidDevicesQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetRfidDevicesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<RfidDeviceDto>> HandleAsync(GetRfidDevicesQuery query, CancellationToken cancellationToken = default)
    {
        var devices = await _context.RfidDevices
            .Include(r => r.Bus)
            .AsNoTracking()
            .OrderBy(r => r.DeviceCode)
            .ToListAsync(cancellationToken);

        return devices.Select(r => new RfidDeviceDto(
            r.Id,
            r.SchoolId,
            r.DeviceCode,
            r.BusId,
            r.Bus?.BusNumber,
            r.GateLocation,
            r.Model,
            r.IpAddress,
            r.Port,
            r.ProtocolType,
            r.Status,
            r.LastSeenAt,
            r.LastScanAt)).ToList();
    }
}

