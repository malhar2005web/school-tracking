using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SchoolTrack.Application.Common.Interfaces;
using SchoolTrack.Domain.Entities;
using SchoolTrack.Domain.Interfaces;
using SchoolTrack.Shared.Constants;
using SchoolTrack.Shared.DTOs.Drivers;

namespace SchoolTrack.Application.Features.Drivers;

public sealed record CreateDriverCommand(
    string FullName,
    string Email,
    string Password,
    string LicenseNumber,
    DateTimeOffset? LicenseExpiry,
    string Phone,
    int YearsExperience);

public sealed class CreateDriverCommandValidator : AbstractValidator<CreateDriverCommand>
{
    public CreateDriverCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.LicenseNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(50);
    }
}

public sealed class CreateDriverCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITenantContext _tenantContext;

    public CreateDriverCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ITenantContext tenantContext)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tenantContext = tenantContext;
    }

    public async Task<DriverDto> HandleAsync(CreateDriverCommand command, CancellationToken cancellationToken = default)
    {
        var schoolId = _tenantContext.SchoolId
            ?? throw new UnauthorizedAccessException("Active school tenant context is required.");

        var normalizedEmail = command.Email.Trim().ToLowerInvariant();
        var normalizedLicense = command.LicenseNumber.Trim().ToUpperInvariant();

        var emailExists = await _context.Users
            .AnyAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);
        if (emailExists)
        {
            throw new ArgumentException($"A user with email '{normalizedEmail}' already exists.");
        }

        var licenseExists = await _context.Drivers
            .AnyAsync(d => d.SchoolId == schoolId && d.LicenseNumber == normalizedLicense, cancellationToken);
        if (licenseExists)
        {
            throw new ArgumentException($"A driver with license number '{normalizedLicense}' already exists in this school.");
        }

        var userId = Guid.CreateVersion7();
        var user = new User
        {
            Id = userId,
            SchoolId = schoolId,
            Email = normalizedEmail,
            FullName = command.FullName.Trim(),
            PhoneNumber = command.Phone.Trim(),
            PasswordHash = _passwordHasher.HashPassword(command.Password),
            Role = SystemRoles.Driver,
            IsActive = true
        };

        var driver = new Driver
        {
            Id = Guid.CreateVersion7(),
            SchoolId = schoolId,
            UserId = userId,
            LicenseNumber = normalizedLicense,
            LicenseExpiry = command.LicenseExpiry,
            Phone = command.Phone.Trim(),
            AvailabilityStatus = "Available",
            YearsExperience = command.YearsExperience,
            Rating = 5.0
        };

        _context.Users.Add(user);
        _context.Drivers.Add(driver);

        await _context.SaveChangesAsync(cancellationToken);

        return new DriverDto(
            driver.Id,
            driver.SchoolId,
            driver.UserId,
            user.FullName,
            user.Email,
            driver.LicenseNumber,
            driver.LicenseExpiry,
            driver.Phone,
            driver.AvailabilityStatus,
            driver.YearsExperience,
            driver.Rating);
    }
}

public sealed record GetDriversQuery;

public sealed class GetDriversQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetDriversQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<DriverDto>> HandleAsync(GetDriversQuery query, CancellationToken cancellationToken = default)
    {
        var drivers = await _context.Drivers
            .Include(d => d.User)
            .AsNoTracking()
            .OrderBy(d => d.LicenseNumber)
            .ToListAsync(cancellationToken);

        return drivers.Select(d => new DriverDto(
            d.Id,
            d.SchoolId,
            d.UserId,
            d.User?.FullName ?? "Unknown",
            d.User?.Email ?? "Unknown",
            d.LicenseNumber,
            d.LicenseExpiry,
            d.Phone,
            d.AvailabilityStatus,
            d.YearsExperience,
            d.Rating)).ToList();
    }
}

