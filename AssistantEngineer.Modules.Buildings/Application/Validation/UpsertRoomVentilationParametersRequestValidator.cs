using AssistantEngineer.Modules.Buildings.Application.Contracts.Requests;
using FluentValidation;

namespace AssistantEngineer.Modules.Buildings.Application.Validation;

public sealed class UpsertRoomVentilationParametersRequestValidator
    : AbstractValidator<UpsertRoomVentilationParametersRequest>
{
    public UpsertRoomVentilationParametersRequestValidator()
    {
        RuleFor(x => x.AirChangesPerHour).InclusiveBetween(0, 20);
        RuleFor(x => x.HeatRecoveryEfficiency).InclusiveBetween(0, 1);
        RuleFor(x => x.InfiltrationAirChangesPerHour).InclusiveBetween(0, 10);
        RuleFor(x => x.WindExposureFactor).InclusiveBetween(0, 5);
        RuleFor(x => x.StackCoefficient).InclusiveBetween(0, 5);
        RuleFor(x => x.WindCoefficient).InclusiveBetween(0, 5);
    }
}