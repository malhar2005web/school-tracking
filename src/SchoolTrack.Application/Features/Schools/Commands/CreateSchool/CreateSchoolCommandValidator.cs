using FluentValidation;

namespace SchoolTrack.Application.Features.Schools.Commands.CreateSchool;

public sealed class CreateSchoolCommandValidator : AbstractValidator<CreateSchoolCommand>
{
    public CreateSchoolCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("School name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("School unique code is required.")
            .MaximumLength(50);

        RuleFor(x => x.ContactEmail)
            .NotEmpty().WithMessage("School contact email is required.")
            .EmailAddress();

        RuleFor(x => x.ContactPhone)
            .NotEmpty().WithMessage("School contact phone is required.")
            .MaximumLength(50);

        RuleFor(x => x.AdminEmail)
            .NotEmpty().WithMessage("Administrator email is required.")
            .EmailAddress();

        RuleFor(x => x.AdminPassword)
            .NotEmpty().WithMessage("Administrator password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.");

        RuleFor(x => x.AdminFullName)
            .NotEmpty().WithMessage("Administrator full name is required.")
            .MaximumLength(150);
    }
}

