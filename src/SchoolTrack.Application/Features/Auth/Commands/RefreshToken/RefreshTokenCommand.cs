using SchoolTrack.Shared.DTOs.Auth;

namespace SchoolTrack.Application.Features.Auth.Commands.RefreshToken;

public sealed record RefreshTokenCommand(
    string RefreshToken,
    string? IpAddress = null);

