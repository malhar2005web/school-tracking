using Microsoft.EntityFrameworkCore;
using SchoolTrack.Application.Common.Interfaces;
using SchoolTrack.Domain.Entities;
using SchoolTrack.Domain.Interfaces;

namespace SchoolTrack.Application.Features.Auth.Commands.Logout;

public sealed class LogoutCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public LogoutCommandHandler(
        IApplicationDbContext context,
        ITokenService tokenService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _tokenService = tokenService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task HandleAsync(LogoutCommand command, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            var tokenHash = _tokenService.HashRefreshToken(command.RefreshToken);
            var token = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

            if (token != null && !token.IsRevoked)
            {
                token.Revoke(command.IpAddress, "UserLogout");
            }
        }
        else if (command.UserId.HasValue)
        {
            // Revoke active refresh tokens for the calling user
            var activeTokens = await _context.RefreshTokens
                .Where(rt => rt.UserId == command.UserId.Value && rt.RevokedAt == null)
                .ToListAsync(cancellationToken);

            foreach (var token in activeTokens)
            {
                token.Revoke(command.IpAddress, "UserLogout");
            }
        }

        if (command.UserId.HasValue)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                Id = Guid.CreateVersion7(),
                UserId = command.UserId.Value,
                Action = "UserLogout",
                EntityType = nameof(User),
                EntityId = command.UserId.Value.ToString(),
                IpAddress = command.IpAddress,
                OccurredAt = _dateTimeProvider.UtcNow
            });
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}

