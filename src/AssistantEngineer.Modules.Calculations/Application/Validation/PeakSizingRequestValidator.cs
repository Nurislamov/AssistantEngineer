using AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;
using FluentValidation;

namespace AssistantEngineer.Modules.Calculations.Application.Validation;

public sealed class PeakSizingRequestValidator : AbstractValidator<PeakSizingRequest>
{
    public PeakSizingRequestValidator()
    {
        RuleFor(x => x.OccupancyThreshold).InclusiveBetween(0, 1);

        RuleFor(x => x.CoolingSeasonStartMonth).InclusiveBetween(1, 12);
        RuleFor(x => x.CoolingSeasonEndMonth).InclusiveBetween(1, 12);

        RuleFor(x => x.HeatingSeasonStartMonth).InclusiveBetween(1, 12);
        RuleFor(x => x.HeatingSeasonEndMonth).InclusiveBetween(1, 12);

        RuleFor(x => x.CoolingSafetyFactor).InclusiveBetween(1.0, 3.0);
        RuleFor(x => x.HeatingSafetyFactor).InclusiveBetween(1.0, 3.0);
    }
}