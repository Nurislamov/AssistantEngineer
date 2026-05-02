using AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;
using FluentValidation;

namespace AssistantEngineer.Modules.Calculations.Application.Validation;

public sealed class SyntheticDesignDayRequestValidator : AbstractValidator<SyntheticDesignDayRequest>
{
    public SyntheticDesignDayRequestValidator()
    {
        RuleFor(x => x.Mode).IsInEnum();
        RuleFor(x => x.DayOfYear).InclusiveBetween(1, 365);

        RuleFor(x => x.DesignOutdoorDryBulbC).InclusiveBetween(-50, 70);
        RuleFor(x => x.OutdoorDailyRangeC).InclusiveBetween(0, 30);
        RuleFor(x => x.WindSpeedMPerS).InclusiveBetween(0, 40);
        RuleFor(x => x.SolarPeakWPerM2).InclusiveBetween(0, 1500);

        RuleFor(x => x.CoolingSetpointC).InclusiveBetween(10, 40);
        RuleFor(x => x.HeatingSetpointC).InclusiveBetween(5, 35);
        RuleFor(x => x.GroundBoundaryTemperatureC).InclusiveBetween(-10, 40);

        RuleFor(x => x.CoolingSafetyFactor).InclusiveBetween(1.0, 3.0);
        RuleFor(x => x.HeatingSafetyFactor).InclusiveBetween(1.0, 3.0);
    }
}