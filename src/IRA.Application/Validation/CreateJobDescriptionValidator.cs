using FluentValidation;
using IRA.Application.DTOs;

namespace IRA.Application.Validation;

public class CreateJobDescriptionValidator : AbstractValidator<CreateJobDescriptionDto>
{
    public CreateJobDescriptionValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.RawText).NotEmpty().WithMessage("Job description text is required.");
        RuleFor(x => x.MinYearsExperience).GreaterThanOrEqualTo(0);
        RuleFor(x => x.RequiredSkills).NotNull();
    }
}
