using AssistantEngineer.Modules.Calculations.Application.Models.ReferenceData;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.ReferenceData;

public interface IBuildingEnvelopeReferenceData
{
    BuildingEnvelopeDefaults GetDefaults();
}
