using Microsoft.EntityFrameworkCore;
using SchoolTrack.Domain.Entities;
using SchoolTrack.Domain.Interfaces;
using SchoolTrack.Infrastructure.Persistence.Context;
using SchoolTrack.Infrastructure.Services;
using SchoolTrack.Shared.Constants;

namespace SchoolTrack.Api.Tests;

/// <summary>
/// Verifies EF Core multi-tenant global query filters and strict school data isolation.
/// </summary>
public sealed class MultiTenantIsolationTests
{
    private readonly IDateTimeProvider _dateTimeProvider = new SystemDateTimeProvider();

    private SchoolTrackDbContext CreateContext(
        string databaseName,
        Guid? tenantSchoolId,
        bool isPlatformAdmin = false)
    {
        var options = new DbContextOptionsBuilder<SchoolTrackDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var tenantContext = new StubTenantContext(tenantSchoolId, isPlatformAdmin);
        return new SchoolTrackDbContext(options, tenantContext, _dateTimeProvider);
    }

    [Fact]
    public async Task QueryUsers_TenantUser_CannotSeeOtherSchoolUsers()
    {
        var dbName = Guid.NewGuid().ToString();
        var schoolAId = Guid.NewGuid();
        var schoolBId = Guid.NewGuid();

        // 1. Seed database using an administrative/unscoped context
        await using (var seedContext = CreateContext(dbName, null, isPlatformAdmin: true))
        {
            var schoolA = new School { Id = schoolAId, Name = "School A", Code = "SCH-A" };
            var schoolB = new School { Id = schoolBId, Name = "School B", Code = "SCH-B" };

            var userA = new User
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolAId,
                Email = "teacher@schoolA.org",
                FullName = "Alice Teacher",
                Role = SystemRoles.SchoolAdmin
            };

            var userB = new User
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolBId,
                Email = "teacher@schoolB.org",
                FullName = "Bob Teacher",
                Role = SystemRoles.SchoolAdmin
            };

            seedContext.Schools.AddRange(schoolA, schoolB);
            seedContext.Users.AddRange(userA, userB);
            await seedContext.SaveChangesAsync();
        }

        // 2. Query as School A user
        await using (var contextSchoolA = CreateContext(dbName, schoolAId, isPlatformAdmin: false))
        {
            var visibleUsers = await contextSchoolA.Users.ToListAsync();

            Assert.Single(visibleUsers);
            Assert.Equal("teacher@schoolA.org", visibleUsers[0].Email);
            Assert.Equal(schoolAId, visibleUsers[0].SchoolId);

            // Directly attempting to fetch User B by ID yields null due to query filter
            var userBDirect = await contextSchoolA.Users
                .FirstOrDefaultAsync(u => u.Email == "teacher@schoolB.org");
            Assert.Null(userBDirect);
        }

        // 3. Query as School B user
        await using (var contextSchoolB = CreateContext(dbName, schoolBId, isPlatformAdmin: false))
        {
            var visibleUsers = await contextSchoolB.Users.ToListAsync();

            Assert.Single(visibleUsers);
            Assert.Equal("teacher@schoolB.org", visibleUsers[0].Email);
            Assert.Equal(schoolBId, visibleUsers[0].SchoolId);
        }

        // 4. Query as SuperAdmin (Platform Admin)
        await using (var superAdminContext = CreateContext(dbName, null, isPlatformAdmin: true))
        {
            var allUsers = await superAdminContext.Users.ToListAsync();

            Assert.Equal(2, allUsers.Count);
        }
    }

    [Fact]
    public async Task QuerySchoolSettings_TenantUser_OnlySeesOwnSchoolSettings()
    {
        var dbName = Guid.NewGuid().ToString();
        var schoolAId = Guid.NewGuid();
        var schoolBId = Guid.NewGuid();

        await using (var seedContext = CreateContext(dbName, null, isPlatformAdmin: true))
        {
            seedContext.SchoolSettings.AddRange(
                new SchoolSettings
                {
                    Id = Guid.NewGuid(),
                    SchoolId = schoolAId,
                    DuplicateScanWindowSeconds = 45
                },
                new SchoolSettings
                {
                    Id = Guid.NewGuid(),
                    SchoolId = schoolBId,
                    DuplicateScanWindowSeconds = 90
                });

            await seedContext.SaveChangesAsync();
        }

        await using (var contextSchoolA = CreateContext(dbName, schoolAId, isPlatformAdmin: false))
        {
            var settings = await contextSchoolA.SchoolSettings.ToListAsync();

            Assert.Single(settings);
            Assert.Equal(schoolAId, settings[0].SchoolId);
            Assert.Equal(45, settings[0].DuplicateScanWindowSeconds);
        }
    }

    private sealed class StubTenantContext(Guid? schoolId, bool isPlatformAdmin) : ITenantContext
    {
        public Guid? SchoolId => schoolId;
        public bool IsPlatformAdmin => isPlatformAdmin;
    }
}

