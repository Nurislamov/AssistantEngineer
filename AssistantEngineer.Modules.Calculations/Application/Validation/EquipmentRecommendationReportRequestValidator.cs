using AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;
using FluentValidation;

namespace AssistantEngineer.Modules.Calculations.Application.Validation;

public sealed class EquipmentRecommendationReportRequestValidator
    : AbstractValidator<EquipmentRecommendationReportRequest>
{
    public EquipmentRecommendationReportRequestValidator()
    {
        RuleFor(x => x.ScenarioName)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Request)
            .NotNull()
            .SetValidator(new EquipmentRecommendationRequestValidator());
    }
}