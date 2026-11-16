using SchoolTrack.Shared.DTOs.Schools;

namespace SchoolTrack.Application.Features.Schools.Commands.CreateSchool;

public sealed record CreateSchoolCommand(
    string Name,
    string Code,
    string? TimeZoneId,
    string ContactEmail,
    string ContactPhone,
    string? Address,
    string AdminEmail,
    string AdminPassword,
    string AdminFullName);

