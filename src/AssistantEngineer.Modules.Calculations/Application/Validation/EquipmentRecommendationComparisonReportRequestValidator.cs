using AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;
using FluentValidation;

namespace AssistantEngineer.Modules.Calculations.Application.Validation;

public sealed class EquipmentRecommendationComparisonReportRequestValidator
    : AbstractValidator<EquipmentRecommendationComparisonReportRequest>
{
    public EquipmentRecommendationComparisonReportRequestValidator()
    {
        RuleFor(x => x.Request)
            .NotNull()
            .SetValidator(new EquipmentRecommendationComparisonRequestValidator());
    }
}