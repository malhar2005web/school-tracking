namespace SchoolTrack.Application.Features.Auth.Commands.Logout;

public sealed record LogoutCommand(
    string? RefreshToken,
    Guid? UserId,
    string? IpAddress = null);

