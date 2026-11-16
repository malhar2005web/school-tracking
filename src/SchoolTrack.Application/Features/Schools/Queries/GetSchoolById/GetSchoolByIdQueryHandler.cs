using Microsoft.EntityFrameworkCore;
using SchoolTrack.Application.Common.Interfaces;
using SchoolTrack.Domain.Interfaces;
using SchoolTrack.Shared.DTOs.Schools;

namespace SchoolTrack.Application.Features.Schools.Queries.GetSchoolById;

public sealed class GetSchoolByIdQueryHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetSchoolByIdQueryHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<SchoolDto> HandleAsync(GetSchoolByIdQuery query, CancellationToken cancellationToken = default)
    {
        // Tenant isolation: if not platform admin and querying different school -> Unauthorized/Forbidden
        if (!_tenantContext.IsPlatformAdmin && _tenantContext.SchoolId.HasValue && _tenantContext.SchoolId.Value != query.SchoolId)
        {
            throw new UnauthorizedAccessException("Access denied. You can only view your assigned school.");
        }

        var school = await _context.Schools
            .Include(s => s.Settings)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == query.SchoolId, cancellationToken);

        if (school == null)
        {
            throw new KeyNotFoundException($"School with ID '{query.SchoolId}' was not found.");
        }

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
            Settings: school.Settings == null ? null : new SchoolSettingsDto(
                Id: school.Settings.Id,
                SchoolId: school.Settings.SchoolId,
                DuplicateScanWindowSeconds: school.Settings.DuplicateScanWindowSeconds,
                DeviceOfflineThresholdSeconds: school.Settings.DeviceOfflineThresholdSeconds,
                GpsRetentionDays: school.Settings.GpsRetentionDays,
                EnableSmsFallback: school.Settings.EnableSmsFallback,
                UpdatedAt: school.Settings.UpdatedAt));
    }
}

