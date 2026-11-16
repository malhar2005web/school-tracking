using SchoolTrack.Shared.DTOs.Auth;

namespace SchoolTrack.Application.Features.Auth.Commands.Login;

public sealed record LoginCommand(
    string Email,
    string Password,
    string? IpAddress = null);

