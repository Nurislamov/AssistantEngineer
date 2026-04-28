using AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;
using FluentValidation;

namespace AssistantEngineer.Modules.Calculations.Application.Validation;

public sealed class EquipmentRecommendationComparisonRequestValidator
    : AbstractValidator<EquipmentRecommendationComparisonRequest>
{
    public EquipmentRecommendationComparisonRequestValidator()
    {
        RuleFor(x => x.Scenarios)
            .NotNull()
            .Must(x => x.Count is >= 2 and <= 6)
            .WithMessage("Comparison must contain between 2 and 6 scenarios.");

        RuleForEach(x => x.Scenarios)
            .SetValidator(new EquipmentRecommendationScenarioRequestValidator());

        RuleFor(x => x.Scenarios)
            .Must(HaveUniqueScenarioNames)
            .WithMessage("Scenario names must be unique.");

        RuleFor(x => x.Scenarios)
            .Must(HaveSameGranularity)
            .WithMessage("All scenarios in one comparison must use the same granularity.");
    }

    private static bool HaveUniqueScenarioNames(List<EquipmentRecommendationScenarioRequest> scenarios) =>
        scenarios
            .Select(x => x.ScenarioName.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count() == scenarios.Count;

    private static bool HaveSameGranularity(List<EquipmentRecommendationScenarioRequest> scenarios)
    {
        if (scenarios.Count == 0)
            return true;

        var granularity = scenarios[0].Request.Granularity;
        return scenarios.All(x => x.Request.Granularity == granularity);
    }
}