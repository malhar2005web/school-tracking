using Microsoft.EntityFrameworkCore;
using SchoolTrack.Application.Common.Interfaces;
using SchoolTrack.Domain.Interfaces;
using SchoolTrack.Shared.DTOs.Schools;

namespace SchoolTrack.Application.Features.Schools.Queries.GetSchools;

public sealed class GetSchoolsQueryHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public GetSchoolsQueryHandler(
        IApplicationDbContext context,
        ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<IReadOnlyList<SchoolDto>> HandleAsync(GetSchoolsQuery query, CancellationToken cancellationToken = default)
    {
        var schoolsQuery = _context.Schools
            .Include(s => s.Settings)
            .AsNoTracking();

        // If not platform admin, only return the user's specific school
        if (!_tenantContext.IsPlatformAdmin && _tenantContext.SchoolId.HasValue)
        {
            schoolsQuery = schoolsQuery.Where(s => s.Id == _tenantContext.SchoolId.Value);
        }

        var schools = await schoolsQuery
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

        return schools.Select(s => new SchoolDto(
            Id: s.Id,
            Name: s.Name,
            Code: s.Code,
            TimeZoneId: s.TimeZoneId,
            ContactEmail: s.ContactEmail,
            ContactPhone: s.ContactPhone,
            Address: s.Address,
            IsActive: s.IsActive,
            CreatedAt: s.CreatedAt,
            Settings: s.Settings == null ? null : new SchoolSettingsDto(
                Id: s.Settings.Id,
                SchoolId: s.Settings.SchoolId,
                DuplicateScanWindowSeconds: s.Settings.DuplicateScanWindowSeconds,
                DeviceOfflineThresholdSeconds: s.Settings.DeviceOfflineThresholdSeconds,
                GpsRetentionDays: s.Settings.GpsRetentionDays,
                EnableSmsFallback: s.Settings.EnableSmsFallback,
                UpdatedAt: s.Settings.UpdatedAt))).ToList();
    }
}

