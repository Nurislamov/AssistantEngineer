using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using FluentValidation;

namespace AssistantEngineer.Modules.Buildings.Application.Validation;

public class UpdateBuildingRequestValidator : AbstractValidator<UpdateBuildingRequest>
{
    public UpdateBuildingRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);
        RuleFor(x => x.ClimateZoneId)
            .GreaterThan(0)
            .When(x => x.ClimateZoneId.HasValue);
    }
}
