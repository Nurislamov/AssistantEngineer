using AssistantEngineer.Contracts.Requests;
using FluentValidation;

namespace AssistantEngineer.Application.Validation;

public class CreateWindowRequestValidator : AbstractValidator<CreateWindowRequest>
{
    public CreateWindowRequestValidator()
    {
        RuleFor(x => x.AreaM2).InclusiveBetween(0.1, 100);
    }
}
