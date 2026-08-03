using FluentValidation;
using IRA.Application.DTOs;

namespace IRA.Application.Validation;

public class EvaluateCandidatesValidator : AbstractValidator<EvaluateCandidatesRequestDto>
{
    public EvaluateCandidatesValidator()
    {
        RuleFor(x => x.JobDescriptionId).NotEmpty().WithMessage("A job description id is required.");
        RuleFor(x => x.InterviewShortlistSize).InclusiveBetween(0, 100);
    }
}
