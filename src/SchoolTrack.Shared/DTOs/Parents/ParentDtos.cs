namespace SchoolTrack.Shared.DTOs.Parents;

public sealed record ParentDto(
    Guid Id,
    Guid SchoolId,
    Guid UserId,
    string FullName,
    string Email,
    string PrimaryPhone,
    string? AlternatePhone,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? Address,
    int LinkedStudentsCount);

public sealed record CreateParentRequest(
    string FullName,
    string Email,
    string Password,
    string PrimaryPhone,
    string? AlternatePhone,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? Address);

public sealed record LinkStudentRequest(
    Guid StudentId,
    string Relationship,
    bool IsPrimaryGuardian,
    bool CanAuthorizePickup);

