using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SchoolTrack.Application.Features.Auth.Commands.Login;
using SchoolTrack.Application.Features.Auth.Commands.Logout;
using SchoolTrack.Application.Features.Auth.Commands.RefreshToken;
using SchoolTrack.Application.Features.Schools.Commands.CreateSchool;
using SchoolTrack.Application.Features.Schools.Queries.GetSchoolById;
using SchoolTrack.Domain.Entities;
using SchoolTrack.Domain.Interfaces;
using SchoolTrack.Infrastructure.Authentication;
using SchoolTrack.Infrastructure.Persistence.Context;
using SchoolTrack.Infrastructure.Services;
using SchoolTrack.Shared.Constants;

namespace SchoolTrack.Api.Tests;

/// <summary>
/// Verifies end-to-end authentication flows, token rotation/reuse security, and school onboarding handlers.
/// </summary>
public sealed class AuthAndSchoolFlowTests
{
    private readonly IDateTimeProvider _dateTimeProvider = new SystemDateTimeProvider();
    private readonly PasswordHasher _passwordHasher = new();
    private readonly JwtTokenService _tokenService;

    public AuthAndSchoolFlowTests()
    {
        var settings = new JwtSettings
        {
            SecretKey = "SuperSecretKeyForIntegrationTesting123456789!",
            Issuer = "SchoolTrack.Test",
            Audience = "SchoolTrack.Test",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        };
        _tokenService = new JwtTokenService(Options.Create(settings), _dateTimeProvider);
    }

    private SchoolTrackDbContext CreateDbContext(string dbName, Guid? schoolId = null, bool isPlatformAdmin = false)
    {
        var options = new DbContextOptionsBuilder<SchoolTrackDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        var tenantContext = new StubTenant(schoolId, isPlatformAdmin);
        return new SchoolTrackDbContext(options, tenantContext, _dateTimeProvider);
    }

    [Fact]
    public async Task CreateSchool_ValidRequest_CreatesSchoolAndAdminUser()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var context = CreateDbContext(dbName, null, isPlatformAdmin: true);

        var handler = new CreateSchoolCommandHandler(context, _passwordHasher, _dateTimeProvider);
        var command = new CreateSchoolCommand(
            Name: "Greenwood High School",
            Code: "GWH-01",
            TimeZoneId: "America/New_York",
            ContactEmail: "contact@greenwood.edu",
            ContactPhone: "+1-555-0199",
            Address: "123 Education Lane",
            AdminEmail: "admin@greenwood.edu",
            AdminPassword: "SuperAdminPassword123!",
            AdminFullName: "Principal Vance");

        var result = await handler.HandleAsync(command);

        Assert.NotNull(result);
        Assert.Equal("GWH-01", result.Code);
        Assert.Equal("Greenwood High School", result.Name);
        Assert.NotNull(result.Settings);
        Assert.Equal(60, result.Settings!.DuplicateScanWindowSeconds);

        var schoolInDb = await context.Schools.FirstOrDefaultAsync(s => s.Code == "GWH-01");
        Assert.NotNull(schoolInDb);

        var adminInDb = await context.Users.FirstOrDefaultAsync(u => u.Email == "admin@greenwood.edu");
        Assert.NotNull(adminInDb);
        Assert.Equal(SystemRoles.SchoolAdmin, adminInDb!.Role);
        Assert.True(_passwordHasher.VerifyPassword("SuperAdminPassword123!", adminInDb.PasswordHash));
    }

    [Fact]
    public async Task CreateSchool_DuplicateCode_ThrowsArgumentException()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var context = CreateDbContext(dbName, null, isPlatformAdmin: true);

        var handler = new CreateSchoolCommandHandler(context, _passwordHasher, _dateTimeProvider);
        var command1 = new CreateSchoolCommand(
            Name: "School One",
            Code: "DUP-CODE",
            TimeZoneId: "UTC",
            ContactEmail: "c1@school.org",
            ContactPhone: "+1-555-0101",
            Address: null,
            AdminEmail: "admin1@school.org",
            AdminPassword: "Password123!",
            AdminFullName: "Admin One");

        await handler.HandleAsync(command1);

        var command2 = command1 with { Name = "School Two", AdminEmail = "admin2@school.org" };
        await Assert.ThrowsAsync<ArgumentException>(() => handler.HandleAsync(command2));
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokensAndRecordsAudit()
    {
        var dbName = Guid.NewGuid().ToString();
        var schoolId = Guid.NewGuid();

        await using (var seedContext = CreateDbContext(dbName, null, isPlatformAdmin: true))
        {
            var school = new School { Id = schoolId, Name = "Oakridge Academy", Code = "OAK-01", IsActive = true };
            var user = new User
            {
                Id = Guid.NewGuid(),
                SchoolId = schoolId,
                Email = "driver.sam@oakridge.edu",
                PasswordHash = _passwordHasher.HashPassword("DriverSecret123!"),
                FullName = "Sam Driver",
                Role = SystemRoles.Driver,
                IsActive = true
            };
            seedContext.Schools.Add(school);
            seedContext.Users.Add(user);
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateDbContext(dbName, null, isPlatformAdmin: true);
        var handler = new LoginCommandHandler(context, _passwordHasher, _tokenService, _dateTimeProvider);

        var response = await handler.HandleAsync(new LoginCommand("driver.sam@oakridge.edu", "DriverSecret123!", "192.168.1.100"));

        Assert.NotNull(response);
        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(response.RefreshToken));
        Assert.Equal("driver.sam@oakridge.edu", response.User.Email);
        Assert.Equal("Oakridge Academy", response.User.SchoolName);

        var tokenHash = _tokenService.HashRefreshToken(response.RefreshToken);
        var tokenInDb = await context.RefreshTokens.FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash);
        Assert.NotNull(tokenInDb);
        Assert.True(tokenInDb!.IsActive);

        var auditInDb = await context.AuditLogs.FirstOrDefaultAsync(a => a.Action == "UserLogin");
        Assert.NotNull(auditInDb);
        Assert.Equal("192.168.1.100", auditInDb!.IpAddress);
    }

    [Fact]
    public async Task Login_InvalidPassword_ThrowsUnauthorizedAccessException()
    {
        var dbName = Guid.NewGuid().ToString();
        await using (var seedContext = CreateDbContext(dbName, null, isPlatformAdmin: true))
        {
            seedContext.Users.Add(new User
            {
                Id = Guid.NewGuid(),
                Email = "user@test.org",
                PasswordHash = _passwordHasher.HashPassword("RealPassword!"),
                FullName = "Test User",
                Role = SystemRoles.Parent
            });
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateDbContext(dbName, null, isPlatformAdmin: true);
        var handler = new LoginCommandHandler(context, _passwordHasher, _tokenService, _dateTimeProvider);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.HandleAsync(new LoginCommand("user@test.org", "WrongPassword!")));
    }

    [Fact]
    public async Task RefreshToken_ValidToken_RotatesSuccessfully()
    {
        var dbName = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        var rawToken = "initial-high-entropy-refresh-token";
        var tokenHash = _tokenService.HashRefreshToken(rawToken);

        await using (var seedContext = CreateDbContext(dbName, null, isPlatformAdmin: true))
        {
            var user = new User
            {
                Id = userId,
                Email = "teacher@school.org",
                FullName = "Teacher One",
                Role = SystemRoles.SchoolAdmin,
                IsActive = true
            };
            var refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = tokenHash,
                ExpiresAt = _dateTimeProvider.UtcNow.AddDays(7)
            };
            seedContext.Users.Add(user);
            seedContext.RefreshTokens.Add(refreshToken);
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateDbContext(dbName, null, isPlatformAdmin: true);
        var handler = new RefreshTokenCommandHandler(context, _tokenService, _dateTimeProvider);

        var response = await handler.HandleAsync(new RefreshTokenCommand(rawToken, "10.0.0.1"));

        Assert.NotNull(response);
        Assert.NotEqual(rawToken, response.RefreshToken);

        // Verify old token is marked revoked
        var oldToken = await context.RefreshTokens.FirstAsync(rt => rt.TokenHash == tokenHash);
        Assert.True(oldToken.IsRevoked);

        // Verify new token exists and is active
        var newTokenHash = _tokenService.HashRefreshToken(response.RefreshToken);
        var newToken = await context.RefreshTokens.FirstAsync(rt => rt.TokenHash == newTokenHash);
        Assert.True(newToken.IsActive);
    }

    [Fact]
    public async Task RefreshToken_ReusedRevokedToken_TriggersCompromiseRevocation()
    {
        var dbName = Guid.NewGuid().ToString();
        var userId = Guid.NewGuid();
        var leakedToken = "stolen-refresh-token";
        var leakedHash = _tokenService.HashRefreshToken(leakedToken);

        await using (var seedContext = CreateDbContext(dbName, null, isPlatformAdmin: true))
        {
            var user = new User
            {
                Id = userId,
                Email = "victim@school.org",
                FullName = "Victim User",
                Role = SystemRoles.Parent,
                IsActive = true
            };
            var alreadyRevokedToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = leakedHash,
                ExpiresAt = _dateTimeProvider.UtcNow.AddDays(7),
                RevokedAt = _dateTimeProvider.UtcNow.AddHours(-1) // Already revoked!
            };
            var activeSessionToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = _tokenService.HashRefreshToken("active-legitimate-token"),
                ExpiresAt = _dateTimeProvider.UtcNow.AddDays(7)
            };
            seedContext.Users.Add(user);
            seedContext.RefreshTokens.AddRange(alreadyRevokedToken, activeSessionToken);
            await seedContext.SaveChangesAsync();
        }

        await using var context = CreateDbContext(dbName, null, isPlatformAdmin: true);
        var handler = new RefreshTokenCommandHandler(context, _tokenService, _dateTimeProvider);

        var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.HandleAsync(new RefreshTokenCommand(leakedToken, "attacker-ip")));

        Assert.Contains("Compromised refresh token reused", ex.Message);

        // Verify all other active tokens for the user were revoked due to compromise
        var remainingTokens = await context.RefreshTokens.Where(rt => rt.UserId == userId).ToListAsync();
        Assert.All(remainingTokens, t => Assert.True(t.IsRevoked));

        var securityAudit = await context.AuditLogs.FirstOrDefaultAsync(a => a.Action.Contains("CompromisedRefreshToken"));
        Assert.NotNull(securityAudit);
    }

    [Fact]
    public async Task GetSchoolById_SchoolAdminQueryingDifferentSchool_ThrowsUnauthorized()
    {
        var dbName = Guid.NewGuid().ToString();
        var schoolAId = Guid.NewGuid();
        var schoolBId = Guid.NewGuid();

        await using (var seedContext = CreateDbContext(dbName, null, isPlatformAdmin: true))
        {
            seedContext.Schools.AddRange(
                new School { Id = schoolAId, Name = "School A", Code = "SCH-A" },
                new School { Id = schoolBId, Name = "School B", Code = "SCH-B" });
            await seedContext.SaveChangesAsync();
        }

        // School Admin context scoped to School A
        await using var contextSchoolA = CreateDbContext(dbName, schoolAId, isPlatformAdmin: false);
        var tenantContext = new StubTenant(schoolAId, isPlatformAdmin: false);
        var handler = new GetSchoolByIdQueryHandler(contextSchoolA, tenantContext);

        // Attempting to query School B
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.HandleAsync(new GetSchoolByIdQuery(schoolBId)));
    }

    private sealed class StubTenant(Guid? schoolId, bool isPlatformAdmin) : ITenantContext
    {
        public Guid? SchoolId => schoolId;
        public bool IsPlatformAdmin => isPlatformAdmin;
    }
}

