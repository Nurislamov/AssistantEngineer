using AssistantEngineer.Modules.Calculations.Application.Contracts.Sizing;
using FluentValidation;

namespace AssistantEngineer.Modules.Calculations.Application.Validation;

public sealed class AutosizingRequestValidator : AbstractValidator<AutosizingRequest>
{
    public AutosizingRequestValidator()
    {
        RuleFor(x => x.Mode).IsInEnum();
        RuleFor(x => x.Granularity).IsInEnum();

        RuleFor(x => x.CandidateNominalCapacitiesKw)
            .NotNull()
            .Must(x => x.Count > 0)
            .WithMessage("At least one candidate nominal capacity must be provided.");

        RuleForEach(x => x.CandidateNominalCapacitiesKw)
            .GreaterThan(0);

        RuleFor(x => x.OccupancyThreshold).InclusiveBetween(0, 1);

        RuleFor(x => x.CoolingSeasonStartMonth).InclusiveBetween(1, 12);
        RuleFor(x => x.CoolingSeasonEndMonth).InclusiveBetween(1, 12);

        RuleFor(x => x.HeatingSeasonStartMonth).InclusiveBetween(1, 12);
        RuleFor(x => x.HeatingSeasonEndMonth).InclusiveBetween(1, 12);

        RuleFor(x => x.CoolingSafetyFactor).InclusiveBetween(1.0, 3.0);
        RuleFor(x => x.HeatingSafetyFactor).InclusiveBetween(1.0, 3.0);

        RuleFor(x => x.MaxUnitsPerScope).InclusiveBetween(1, 50);
        RuleFor(x => x.TopRecommendationsPerScope).InclusiveBetween(1, 20);
        RuleFor(x => x.MaxOversizeRatio).InclusiveBetween(0, 5);
    }
}