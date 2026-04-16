using AssistantEngineer.Contracts.Requests;
using FluentValidation;

namespace AssistantEngineer.Application.Validation;

public class CreateWallRequestValidator : AbstractValidator<CreateWallRequest>
{
    public CreateWallRequestValidator()
    {
        RuleFor(x => x.AreaM2).InclusiveBetween(0.1, 1_000);
    }
}
