using AssistantEngineer.Modules.Calculations.Application.Abstractions.ReferenceData;
using AssistantEngineer.Modules.Calculations.Application.Models.ReferenceData;

namespace AssistantEngineer.Modules.Calculations.Application.Services.ReferenceData;

public sealed class BuildingEnvelopeReferenceData : IBuildingEnvelopeReferenceData
{
    private static readonly BuildingEnvelopeDefaults Defaults = new(
        FloorUValueWPerM2K: 0.3,
        CeilingUValueWPerM2K: 0.2,
        FloorHeatCapacityKjPerM2K: 50.0,
        CeilingHeatCapacityKjPerM2K: 30.0);

    public BuildingEnvelopeDefaults GetDefaults() => Defaults;
}
