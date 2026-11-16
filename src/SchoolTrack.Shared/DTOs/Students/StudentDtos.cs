namespace SchoolTrack.Shared.DTOs.Students;

public sealed record StudentDto(
    Guid Id,
    Guid SchoolId,
    Guid ClassId,
    string ClassName,
    string AdmissionNumber,
    string FirstName,
    string LastName,
    string? PhotoUrl,
    string? EmergencyContactPhone,
    string? SeatNumber,
    string? AssignedCardUid,
    bool IsActive);

public sealed record CreateStudentRequest(
    Guid ClassId,
    string AdmissionNumber,
    string FirstName,
    string LastName,
    string? PhotoUrl,
    string? Gender,
    DateTimeOffset? DateOfBirth,
    string? EmergencyContactPhone,
    string? SeatNumber);

public sealed record UpdateStudentRequest(
    Guid ClassId,
    string FirstName,
    string LastName,
    string? PhotoUrl,
    string? EmergencyContactPhone,
    string? SeatNumber,
    bool IsActive);

