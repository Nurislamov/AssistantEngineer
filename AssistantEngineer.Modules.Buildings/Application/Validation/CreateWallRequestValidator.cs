using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using FluentValidation;

namespace AssistantEngineer.Modules.Buildings.Application.Validation;

public class CreateWallRequestValidator : AbstractValidator<CreateWallRequest>
{
    public CreateWallRequestValidator()
    {
        RuleFor(x => x.AreaM2).InclusiveBetween(0.1, 1000);
        RuleFor(x => x.UValue).InclusiveBetween(0.1, 10);
        RuleFor(x => x.Orientation).IsInEnum();
    }
}
