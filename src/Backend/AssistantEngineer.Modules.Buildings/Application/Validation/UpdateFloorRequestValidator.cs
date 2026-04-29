using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using FluentValidation;

namespace AssistantEngineer.Modules.Buildings.Application.Validation;

public class UpdateFloorRequestValidator : AbstractValidator<UpdateFloorRequest>
{
    public UpdateFloorRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);
    }
}
