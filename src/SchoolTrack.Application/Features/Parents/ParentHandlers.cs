using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SchoolTrack.Application.Common.Interfaces;
using SchoolTrack.Domain.Entities;
using SchoolTrack.Domain.Interfaces;
using SchoolTrack.Shared.Constants;
using SchoolTrack.Shared.DTOs.Parents;

namespace SchoolTrack.Application.Features.Parents;

public sealed record CreateParentCommand(
    string FullName,
    string Email,
    string Password,
    string PrimaryPhone,
    string? AlternatePhone = null,
    string? EmergencyContactName = null,
    string? EmergencyContactPhone = null,
    string? Address = null);

public sealed class CreateParentCommandValidator : AbstractValidator<CreateParentCommand>
{
    public CreateParentCommandValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.PrimaryPhone).NotEmpty().MaximumLength(50);
    }
}

public sealed class CreateParentCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITenantContext _tenantContext;

    public CreateParentCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        ITenantContext tenantContext)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tenantContext = tenantContext;
    }

    public async Task<ParentDto> HandleAsync(CreateParentCommand command, CancellationToken cancellationToken = default)
    {
        var schoolId = _tenantContext.SchoolId
            ?? throw new UnauthorizedAccessException("Active school tenant context is required.");

        var normalizedEmail = command.Email.Trim().ToLowerInvariant();

        var emailExists = await _context.Users
            .AnyAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);
        if (emailExists)
        {
            throw new ArgumentException($"A user with email '{normalizedEmail}' already exists.");
        }

        var userId = Guid.CreateVersion7();
        var user = new User
        {
            Id = userId,
            SchoolId = schoolId,
            Email = normalizedEmail,
            FullName = command.FullName.Trim(),
            PhoneNumber = command.PrimaryPhone.Trim(),
            PasswordHash = _passwordHasher.HashPassword(command.Password),
            Role = SystemRoles.Parent,
            IsActive = true
        };

        var parent = new Parent
        {
            Id = Guid.CreateVersion7(),
            SchoolId = schoolId,
            UserId = userId,
            PrimaryPhone = command.PrimaryPhone.Trim(),
            AlternatePhone = command.AlternatePhone?.Trim(),
            EmergencyContactName = command.EmergencyContactName?.Trim(),
            EmergencyContactPhone = command.EmergencyContactPhone?.Trim(),
            Address = command.Address?.Trim()
        };

        _context.Users.Add(user);
        _context.Parents.Add(parent);

        await _context.SaveChangesAsync(cancellationToken);

        return new ParentDto(
            parent.Id,
            parent.SchoolId,
            parent.UserId,
            user.FullName,
            user.Email,
            parent.PrimaryPhone,
            parent.AlternatePhone,
            parent.EmergencyContactName,
            parent.EmergencyContactPhone,
            parent.Address,
            0);
    }
}

public sealed record LinkStudentCommand(
    Guid ParentId,
    Guid StudentId,
    string Relationship,
    bool IsPrimaryGuardian = true,
    bool CanAuthorizePickup = true);

public sealed class LinkStudentCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public LinkStudentCommandHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task HandleAsync(LinkStudentCommand command, CancellationToken cancellationToken = default)
    {
        var schoolId = _tenantContext.SchoolId
            ?? throw new UnauthorizedAccessException("Active school tenant context is required.");

        var parent = await _context.Parents
            .FirstOrDefaultAsync(p => p.Id == command.ParentId && p.SchoolId == schoolId, cancellationToken);
        if (parent == null)
        {
            throw new KeyNotFoundException($"Parent with ID '{command.ParentId}' was not found.");
        }

        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.Id == command.StudentId && s.SchoolId == schoolId, cancellationToken);
        if (student == null)
        {
            throw new KeyNotFoundException($"Student with ID '{command.StudentId}' was not found.");
        }

        var linkExists = await _context.ParentStudents
            .AnyAsync(ps => ps.ParentId == command.ParentId && ps.StudentId == command.StudentId, cancellationToken);
        if (linkExists)
        {
            throw new ArgumentException("Student is already linked to this parent.");
        }

        var link = new ParentStudent
        {
            Id = Guid.CreateVersion7(),
            SchoolId = schoolId,
            ParentId = command.ParentId,
            StudentId = command.StudentId,
            Relationship = command.Relationship.Trim(),
            IsPrimaryGuardian = command.IsPrimaryGuardian,
            CanAuthorizePickup = command.CanAuthorizePickup
        };

        _context.ParentStudents.Add(link);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

public sealed record GetParentsQuery;

public sealed class GetParentsQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetParentsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ParentDto>> HandleAsync(GetParentsQuery query, CancellationToken cancellationToken = default)
    {
        var parents = await _context.Parents
            .Include(p => p.User)
            .Include(p => p.StudentLinks)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return parents.Select(p => new ParentDto(
            p.Id,
            p.SchoolId,
            p.UserId,
            p.User?.FullName ?? "Unknown",
            p.User?.Email ?? "Unknown",
            p.PrimaryPhone,
            p.AlternatePhone,
            p.EmergencyContactName,
            p.EmergencyContactPhone,
            p.Address,
            p.StudentLinks.Count)).ToList();
    }
}

