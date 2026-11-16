using Microsoft.EntityFrameworkCore;
using SchoolTrack.Application.Common.Interfaces;
using SchoolTrack.Domain.Entities;
using SchoolTrack.Domain.Interfaces;
using SchoolTrack.Shared.DTOs.Auth;
using RefreshTokenEntity = SchoolTrack.Domain.Entities.RefreshToken;

namespace SchoolTrack.Application.Features.Auth.Commands.Login;

public sealed class LoginCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITokenService _tokenService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public LoginCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher _hasher,
        ITokenService tokenService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _passwordHasher = _hasher;
        _tokenService = tokenService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<AuthResponse> HandleAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = command.Email.Trim().ToLowerInvariant();

        var user = await _context.Users
            .Include(u => u.School)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);

        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        if (user.School != null && !user.School.IsActive)
        {
            throw new UnauthorizedAccessException("The school associated with this account is inactive.");
        }

        var isPasswordValid = _passwordHasher.VerifyPassword(command.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            throw new UnauthorizedAccessException("Invalid credentials.");
        }

        var authResponse = _tokenService.GenerateTokens(user, user.School?.Name);

        var rawRefreshToken = authResponse.RefreshToken;
        var tokenHash = _tokenService.HashRefreshToken(rawRefreshToken);

        var refreshTokenEntity = new RefreshTokenEntity
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = _dateTimeProvider.UtcNow.AddDays(30),
            CreatedByIp = command.IpAddress
        };

        user.LastLoginAt = _dateTimeProvider.UtcNow;
        _context.RefreshTokens.Add(refreshTokenEntity);

        _context.AuditLogs.Add(new AuditLog
        {
            Id = Guid.CreateVersion7(),
            SchoolId = user.SchoolId,
            UserId = user.Id,
            Action = "UserLogin",
            EntityType = nameof(User),
            EntityId = user.Id.ToString(),
            IpAddress = command.IpAddress,
            OccurredAt = _dateTimeProvider.UtcNow
        });

        await _context.SaveChangesAsync(cancellationToken);

        return authResponse;
    }
}
