using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.MultiZone;

public interface IIso52016MultiZoneInputValidator
{
    MultiZoneInputValidationResult Validate(MultiZoneCalculationInput input);
}
