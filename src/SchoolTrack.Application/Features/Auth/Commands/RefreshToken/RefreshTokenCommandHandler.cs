using Microsoft.EntityFrameworkCore;
using SchoolTrack.Application.Common.Interfaces;
using SchoolTrack.Domain.Entities;
using SchoolTrack.Domain.Interfaces;
using SchoolTrack.Shared.DTOs.Auth;
using RefreshTokenEntity = SchoolTrack.Domain.Entities.RefreshToken;

namespace SchoolTrack.Application.Features.Auth.Commands.RefreshToken;

public sealed class RefreshTokenCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public RefreshTokenCommandHandler(
        IApplicationDbContext context,
        ITokenService tokenService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _tokenService = tokenService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<AuthResponse> HandleAsync(RefreshTokenCommand command, CancellationToken cancellationToken = default)
    {
        var tokenHash = _tokenService.HashRefreshToken(command.RefreshToken);

        var existingToken = await _context.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u!.School)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (existingToken == null)
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        var user = existingToken.User;
        if (user == null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("Account is inactive or not found.");
        }

        // Replay/theft detection: If already revoked, revoke all tokens for this user as a safeguard.
        if (existingToken.IsRevoked)
        {
            var compromisedTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == user.Id && rt.RevokedAt == null)
                .ToListAsync(cancellationToken);

            foreach (var token in compromisedTokens)
            {
                token.Revoke(command.IpAddress, "CompromisedTokenReused");
            }

            _context.AuditLogs.Add(new AuditLog
            {
                Id = Guid.CreateVersion7(),
                SchoolId = user.SchoolId,
                UserId = user.Id,
                Action = "SecurityAlert_CompromisedRefreshTokenReused",
                EntityType = nameof(RefreshToken),
                EntityId = existingToken.Id.ToString(),
                IpAddress = command.IpAddress,
                OccurredAt = _dateTimeProvider.UtcNow
            });

            await _context.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedAccessException("Compromised refresh token reused. All active sessions have been revoked.");
        }

        if (existingToken.IsExpired)
        {
            throw new UnauthorizedAccessException("Refresh token has expired. Please log in again.");
        }

        // Generate rotated token pair
        var authResponse = _tokenService.GenerateTokens(user, user.School?.Name);
        var newRawRefreshToken = authResponse.RefreshToken;
        var newTokenHash = _tokenService.HashRefreshToken(newRawRefreshToken);

        existingToken.Revoke(command.IpAddress, newTokenHash);

        var newRefreshTokenEntity = new RefreshTokenEntity
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            TokenHash = newTokenHash,
            ExpiresAt = _dateTimeProvider.UtcNow.AddDays(30),
            CreatedByIp = command.IpAddress
        };

        _context.RefreshTokens.Add(newRefreshTokenEntity);

        await _context.SaveChangesAsync(cancellationToken);

        return authResponse;
    }
}
