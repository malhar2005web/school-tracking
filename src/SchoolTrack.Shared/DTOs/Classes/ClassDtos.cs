namespace SchoolTrack.Shared.DTOs.Classes;

public sealed record ClassDto(
    Guid Id,
    Guid SchoolId,
    string Grade,
    string Division,
    string AcademicYear,
    int StudentCount);

public sealed record CreateClassRequest(
    string Grade,
    string Division,
    string AcademicYear);

