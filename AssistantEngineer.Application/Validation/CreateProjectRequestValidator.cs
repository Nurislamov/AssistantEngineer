using AssistantEngineer.Contracts.Requests;
using FluentValidation;

namespace AssistantEngineer.Application.Validation;

public class CreateProjectRequestValidator : AbstractValidator<CreateProjectRequest>
{
    public CreateProjectRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MinimumLength(2)
            .MaximumLength(200);
    }
}
