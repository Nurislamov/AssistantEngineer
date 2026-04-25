using AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;
using FluentValidation;

namespace AssistantEngineer.Modules.Calculations.Application.Validation;

public sealed class EquipmentRecommendationRequestValidator : AbstractValidator<EquipmentRecommendationRequest>
{
    public EquipmentRecommendationRequestValidator()
    {
        RuleFor(x => x.Granularity).IsInEnum();

        RuleFor(x => x.SystemType)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.UnitType)
            .NotEmpty()
            .MaximumLength(50);

        RuleFor(x => x.OccupancyThreshold).InclusiveBetween(0, 1);

        RuleFor(x => x.CoolingSeasonStartMonth).InclusiveBetween(1, 12);
        RuleFor(x => x.CoolingSeasonEndMonth).InclusiveBetween(1, 12);

        RuleFor(x => x.CoolingSafetyFactor).InclusiveBetween(1.0, 3.0);

        RuleFor(x => x.MaxUnitsPerScope).InclusiveBetween(1, 50);
        RuleFor(x => x.TopRecommendationsPerScope).InclusiveBetween(1, 20);
        RuleFor(x => x.MaxOversizeRatio).InclusiveBetween(0, 5);

        RuleFor(x => x.OversizePenaltyWeight).InclusiveBetween(0, 10);
        RuleFor(x => x.UnitCountPenaltyWeight).InclusiveBetween(0, 10);

        RuleFor(x => x.PreferredManufacturerBonus).InclusiveBetween(0, 100);
        RuleFor(x => x.PreferredModelKeywordBonus).InclusiveBetween(0, 100);
        RuleFor(x => x.PreferredEfficiencyKeywordBonus).InclusiveBetween(0, 100);

        RuleFor(x => x.MinimumCatalogScore).InclusiveBetween(-100, 100);

        RuleFor(x => x.SizingWeight).InclusiveBetween(0, 1);
        RuleFor(x => x.CapexWeight).InclusiveBetween(0, 1);
        RuleFor(x => x.EfficiencyWeight).InclusiveBetween(0, 1);
        RuleFor(x => x.ComplexityWeight).InclusiveBetween(0, 1);

        RuleFor(x => x.CapexPerKwFactor).InclusiveBetween(0, 100);
        RuleFor(x => x.CapexPerUnitFactor).InclusiveBetween(0, 100);

        RuleFor(x => x)
            .Must(x => x.SizingWeight + x.CapexWeight + x.EfficiencyWeight + x.ComplexityWeight > 0)
            .WithMessage("At least one scoring weight must be greater than zero.");
    }
}