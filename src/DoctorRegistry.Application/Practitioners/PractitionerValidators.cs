using FluentValidation;

namespace DoctorRegistry.Application.Practitioners;

public sealed class CreatePractitionerCommandValidator : AbstractValidator<CreatePractitionerCommand>
{
    public CreatePractitionerCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(32).Matches("^[+0-9() .-]+$");
        RuleFor(x => x.Speciality).NotEmpty().MaximumLength(150);
        RuleFor(x => x.PlaceOfWork).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().MaximumLength(320).EmailAddress();
    }
}

public sealed class UpdatePractitionerCommandValidator : AbstractValidator<UpdatePractitionerCommand>
{
    public UpdatePractitionerCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(32).Matches("^[+0-9() .-]+$");
        RuleFor(x => x.Speciality).NotEmpty().MaximumLength(150);
        RuleFor(x => x.PlaceOfWork).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Email).NotEmpty().MaximumLength(320).EmailAddress();
        RuleFor(x => x.Version).NotEmpty();
    }
}

public sealed class SearchPractitionersQueryValidator : AbstractValidator<SearchPractitionersQuery>
{
    public SearchPractitionersQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.Speciality).MaximumLength(150).When(x => x.Speciality is not null);
        RuleFor(x => x.PlaceOfWork).MaximumLength(200).When(x => x.PlaceOfWork is not null);
        RuleFor(x => x.Email).MaximumLength(320).When(x => x.Email is not null);
        RuleFor(x => x.Phone).MaximumLength(32).When(x => x.Phone is not null);
        RuleFor(x => x.Name).MaximumLength(200).When(x => x.Name is not null);
    }
}
