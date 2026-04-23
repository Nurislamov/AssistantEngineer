using AssistantEngineer.Modules.Calculations.Application.Contracts.Comfort;
using FluentValidation;

namespace AssistantEngineer.Modules.Calculations.Application.Validation;

public sealed class BuildingComfortMetricsRequestValidator
    : AbstractValidator<BuildingComfortMetricsRequest>
{
    public BuildingComfortMetricsRequestValidator()
    {
        RuleFor(x => x.OverheatingThresholdC).InclusiveBetween(10, 50);
        RuleFor(x => x.SevereOverheatingThresholdC).InclusiveBetween(10, 60);
        RuleFor(x => x.UnderheatingThresholdC).InclusiveBetween(0, 40);
        RuleFor(x => x.OccupancyThreshold).InclusiveBetween(0, 1);
        RuleFor(x => x.CoolingSeasonStartMonth).InclusiveBetween(1, 12);
        RuleFor(x => x.CoolingSeasonEndMonth).InclusiveBetween(1, 12);

        RuleFor(x => x)
            .Must(x => x.SevereOverheatingThresholdC >= x.OverheatingThresholdC)
            .WithMessage("SevereOverheatingThresholdC must be greater than or equal to OverheatingThresholdC.");

        RuleFor(x => x)
            .Must(x => x.UnderheatingThresholdC <= x.OverheatingThresholdC)
            .WithMessage("UnderheatingThresholdC must be less than or equal to OverheatingThresholdC.");
    }
}