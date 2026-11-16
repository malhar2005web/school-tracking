using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SchoolTrack.Application.Common.Interfaces;
using SchoolTrack.Domain.Entities;
using SchoolTrack.Domain.Interfaces;
using SchoolTrack.Shared.DTOs.Students;

namespace SchoolTrack.Application.Features.Students;

public sealed record CreateStudentCommand(
    Guid ClassId,
    string AdmissionNumber,
    string FirstName,
    string LastName,
    string? PhotoUrl = null,
    string? Gender = null,
    DateTimeOffset? DateOfBirth = null,
    string? EmergencyContactPhone = null,
    string? SeatNumber = null);

public sealed class CreateStudentCommandValidator : AbstractValidator<CreateStudentCommand>
{
    public CreateStudentCommandValidator()
    {
        RuleFor(x => x.ClassId).NotEmpty();
        RuleFor(x => x.AdmissionNumber).NotEmpty().MaximumLength(50);
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
    }
}

public sealed class CreateStudentCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public CreateStudentCommandHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<StudentDto> HandleAsync(CreateStudentCommand command, CancellationToken cancellationToken = default)
    {
        var schoolId = _tenantContext.SchoolId
            ?? throw new UnauthorizedAccessException("Active school tenant context is required.");

        var admissionNo = command.AdmissionNumber.Trim().ToUpperInvariant();

        var classEntity = await _context.Classes
            .FirstOrDefaultAsync(c => c.Id == command.ClassId && c.SchoolId == schoolId, cancellationToken);
        if (classEntity == null)
        {
            throw new KeyNotFoundException($"Class with ID '{command.ClassId}' was not found.");
        }

        var exists = await _context.Students
            .AnyAsync(s => s.SchoolId == schoolId && s.AdmissionNumber == admissionNo, cancellationToken);
        if (exists)
        {
            throw new ArgumentException($"A student with admission number '{admissionNo}' already exists in this school.");
        }

        var student = new Student
        {
            Id = Guid.CreateVersion7(),
            SchoolId = schoolId,
            ClassId = command.ClassId,
            AdmissionNumber = admissionNo,
            FirstName = command.FirstName.Trim(),
            LastName = command.LastName.Trim(),
            PhotoUrl = command.PhotoUrl?.Trim(),
            Gender = command.Gender?.Trim(),
            DateOfBirth = command.DateOfBirth,
            EmergencyContactPhone = command.EmergencyContactPhone?.Trim(),
            SeatNumber = command.SeatNumber?.Trim(),
            IsActive = true
        };

        _context.Students.Add(student);
        await _context.SaveChangesAsync(cancellationToken);

        return new StudentDto(
            student.Id,
            student.SchoolId,
            student.ClassId,
            $"{classEntity.Grade} - {classEntity.Division}",
            student.AdmissionNumber,
            student.FirstName,
            student.LastName,
            student.PhotoUrl,
            student.EmergencyContactPhone,
            student.SeatNumber,
            null,
            student.IsActive);
    }
}

public sealed record UpdateStudentCommand(
    Guid StudentId,
    Guid ClassId,
    string FirstName,
    string LastName,
    string? PhotoUrl = null,
    string? EmergencyContactPhone = null,
    string? SeatNumber = null,
    bool IsActive = true);

public sealed class UpdateStudentCommandHandler
{
    private readonly IApplicationDbContext _context;

    public UpdateStudentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<StudentDto> HandleAsync(UpdateStudentCommand command, CancellationToken cancellationToken = default)
    {
        var student = await _context.Students
            .Include(s => s.Class)
            .Include(s => s.AssignedRfidCard)
            .FirstOrDefaultAsync(s => s.Id == command.StudentId, cancellationToken);

        if (student == null)
        {
            throw new KeyNotFoundException($"Student with ID '{command.StudentId}' was not found.");
        }

        student.ClassId = command.ClassId;
        student.FirstName = command.FirstName.Trim();
        student.LastName = command.LastName.Trim();
        student.PhotoUrl = command.PhotoUrl?.Trim();
        student.EmergencyContactPhone = command.EmergencyContactPhone?.Trim();
        student.SeatNumber = command.SeatNumber?.Trim();
        student.IsActive = command.IsActive;

        await _context.SaveChangesAsync(cancellationToken);

        var className = student.Class != null
            ? $"{student.Class.Grade} - {student.Class.Division}"
            : "Unassigned";

        return new StudentDto(
            student.Id,
            student.SchoolId,
            student.ClassId,
            className,
            student.AdmissionNumber,
            student.FirstName,
            student.LastName,
            student.PhotoUrl,
            student.EmergencyContactPhone,
            student.SeatNumber,
            student.AssignedRfidCard?.CardUid,
            student.IsActive);
    }
}

public sealed record GetStudentsQuery;

public sealed class GetStudentsQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetStudentsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<StudentDto>> HandleAsync(GetStudentsQuery query, CancellationToken cancellationToken = default)
    {
        var students = await _context.Students
            .Include(s => s.Class)
            .Include(s => s.AssignedRfidCard)
            .AsNoTracking()
            .OrderBy(s => s.LastName).ThenBy(s => s.FirstName)
            .ToListAsync(cancellationToken);

        return students.Select(s => new StudentDto(
            s.Id,
            s.SchoolId,
            s.ClassId,
            s.Class != null ? $"{s.Class.Grade} - {s.Class.Division}" : "Unassigned",
            s.AdmissionNumber,
            s.FirstName,
            s.LastName,
            s.PhotoUrl,
            s.EmergencyContactPhone,
            s.SeatNumber,
            s.AssignedRfidCard?.CardUid,
            s.IsActive)).ToList();
    }
}

public sealed record GetStudentByIdQuery(Guid StudentId);

public sealed class GetStudentByIdQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetStudentByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<StudentDto> HandleAsync(GetStudentByIdQuery query, CancellationToken cancellationToken = default)
    {
        var student = await _context.Students
            .Include(s => s.Class)
            .Include(s => s.AssignedRfidCard)
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == query.StudentId, cancellationToken);

        if (student == null)
        {
            throw new KeyNotFoundException($"Student with ID '{query.StudentId}' was not found.");
        }

        return new StudentDto(
            student.Id,
            student.SchoolId,
            student.ClassId,
            student.Class != null ? $"{student.Class.Grade} - {student.Class.Division}" : "Unassigned",
            student.AdmissionNumber,
            student.FirstName,
            student.LastName,
            student.PhotoUrl,
            student.EmergencyContactPhone,
            student.SeatNumber,
            student.AssignedRfidCard?.CardUid,
            student.IsActive);
    }
}

