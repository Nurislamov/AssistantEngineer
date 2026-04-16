using AssistantEngineer.Contracts.Requests;
using FluentValidation;

namespace AssistantEngineer.Application.Validation;

public class CreateFloorRequestValidator : AbstractValidator<CreateFloorRequest>
{
    public CreateFloorRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);
    }
}
