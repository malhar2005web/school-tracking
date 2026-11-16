using FluentValidation;
using Microsoft.EntityFrameworkCore;
using SchoolTrack.Application.Common.Interfaces;
using SchoolTrack.Domain.Entities;
using SchoolTrack.Domain.Interfaces;
using SchoolTrack.Shared.DTOs.Classes;

namespace SchoolTrack.Application.Features.Classes;

public sealed record CreateClassCommand(
    string Grade,
    string Division,
    string AcademicYear);

public sealed class CreateClassCommandValidator : AbstractValidator<CreateClassCommand>
{
    public CreateClassCommandValidator()
    {
        RuleFor(x => x.Grade).NotEmpty().MaximumLength(50);
        RuleFor(x => x.Division).NotEmpty().MaximumLength(50);
        RuleFor(x => x.AcademicYear).NotEmpty().MaximumLength(20);
    }
}

public sealed class CreateClassCommandHandler
{
    private readonly IApplicationDbContext _context;
    private readonly ITenantContext _tenantContext;

    public CreateClassCommandHandler(IApplicationDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<ClassDto> HandleAsync(CreateClassCommand command, CancellationToken cancellationToken = default)
    {
        var schoolId = _tenantContext.SchoolId
            ?? throw new UnauthorizedAccessException("Active school tenant context is required.");

        var grade = command.Grade.Trim();
        var division = command.Division.Trim().ToUpperInvariant();
        var academicYear = command.AcademicYear.Trim();

        var exists = await _context.Classes
            .AnyAsync(c => c.SchoolId == schoolId && c.Grade == grade && c.Division == division && c.AcademicYear == academicYear, cancellationToken);

        if (exists)
        {
            throw new ArgumentException($"Class '{grade} - {division}' already exists for academic year '{academicYear}'.");
        }

        var classEntity = new Class
        {
            Id = Guid.CreateVersion7(),
            SchoolId = schoolId,
            Grade = grade,
            Division = division,
            AcademicYear = academicYear
        };

        _context.Classes.Add(classEntity);
        await _context.SaveChangesAsync(cancellationToken);

        return new ClassDto(classEntity.Id, classEntity.SchoolId, classEntity.Grade, classEntity.Division, classEntity.AcademicYear, 0);
    }
}

public sealed record GetClassesQuery;

public sealed class GetClassesQueryHandler
{
    private readonly IApplicationDbContext _context;

    public GetClassesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<ClassDto>> HandleAsync(GetClassesQuery query, CancellationToken cancellationToken = default)
    {
        var classes = await _context.Classes
            .Include(c => c.Students)
            .AsNoTracking()
            .OrderBy(c => c.Grade).ThenBy(c => c.Division)
            .ToListAsync(cancellationToken);

        return classes.Select(c => new ClassDto(
            c.Id,
            c.SchoolId,
            c.Grade,
            c.Division,
            c.AcademicYear,
            c.Students.Count)).ToList();
    }
}

