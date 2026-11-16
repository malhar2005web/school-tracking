using Microsoft.EntityFrameworkCore;
using SchoolTrack.Application.Common.Interfaces;
using SchoolTrack.Domain.Entities;
using SchoolTrack.Domain.Interfaces;
using SchoolTrack.Shared.Constants;
using SchoolTrack.Shared.DTOs.Schools;

namespace SchoolTrack.Application.Features.Schools.Commands.CreateSchool;

public sealed class CreateSchoolCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateSchoolCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<SchoolDto> HandleAsync(CreateSchoolCommand command, CancellationToken cancellationToken = default)
    {
        var normalizedCode = command.Code.Trim().ToUpperInvariant();
        var normalizedAdminEmail = command.AdminEmail.Trim().ToLowerInvariant();

        var codeExists = await _context.Schools
            .AnyAsync(s => s.Code == normalizedCode, cancellationToken);
        if (codeExists)
        {
            throw new ArgumentException($"A school with code '{normalizedCode}' already exists.");
        }

        var emailExists = await _context.Users
            .AnyAsync(u => u.Email.ToLower() == normalizedAdminEmail, cancellationToken);
        if (emailExists)
        {
            throw new ArgumentException($"A user with email '{normalizedAdminEmail}' already exists.");
        }

        var schoolId = Guid.CreateVersion7();
        var school = new School
        {
            Id = schoolId,
            Name = command.Name.Trim(),
            Code = normalizedCode,
            TimeZoneId = string.IsNullOrWhiteSpace(command.TimeZoneId) ? "UTC" : command.TimeZoneId.Trim(),
            ContactEmail = command.ContactEmail.Trim(),
            ContactPhone = command.ContactPhone.Trim(),
            Address = command.Address?.Trim(),
            IsActive = true
        };

        var settings = new SchoolSettings
        {
            Id = Guid.CreateVersion7(),
            SchoolId = schoolId,
            DuplicateScanWindowSeconds = 60,
            DeviceOfflineThresholdSeconds = 180,
            GpsRetentionDays = 90,
            EnableSmsFallback = false
        };

        var adminUser = new User
        {
            Id = Guid.CreateVersion7(),
            SchoolId = schoolId,
            Email = normalizedAdminEmail,
            FullName = command.AdminFullName.Trim(),
            PasswordHash = _passwordHasher.HashPassword(command.AdminPassword),
            Role = SystemRoles.SchoolAdmin,
            IsActive = true
        };

        _context.Schools.Add(school);
        _context.SchoolSettings.Add(settings);
        _context.Users.Add(adminUser);

        _context.AuditLogs.Add(new AuditLog
        {
            Id = Guid.CreateVersion7(),
            SchoolId = schoolId,
            UserId = adminUser.Id,
            Action = "CreateSchool",
            EntityType = nameof(School),
            EntityId = schoolId.ToString(),
            DetailsJson = $"{{\"name\":\"{school.Name}\",\"code\":\"{school.Code}\"}}",
            OccurredAt = _dateTimeProvider.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        return new SchoolDto(
            Id: school.Id,
            Name: school.Name,
            Code: school.Code,
            TimeZoneId: school.TimeZoneId,
            ContactEmail: school.ContactEmail,
            ContactPhone: school.ContactPhone,
            Address: school.Address,
            IsActive: school.IsActive,
            CreatedAt: school.CreatedAt,
            Settings: new SchoolSettingsDto(
                Id: settings.Id,
                SchoolId: settings.SchoolId,
                DuplicateScanWindowSeconds: settings.DuplicateScanWindowSeconds,
                DeviceOfflineThresholdSeconds: settings.DeviceOfflineThresholdSeconds,
                GpsRetentionDays: settings.GpsRetentionDays,
                EnableSmsFallback: settings.EnableSmsFallback,
                UpdatedAt: settings.UpdatedAt));
    }
}

