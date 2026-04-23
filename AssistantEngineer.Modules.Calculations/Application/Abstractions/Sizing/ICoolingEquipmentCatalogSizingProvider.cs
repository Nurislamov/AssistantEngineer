using AssistantEngineer.Modules.Calculations.Application.Models.Sizing;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Sizing;

public interface ICoolingEquipmentCatalogSizingProvider
{
    Task<IReadOnlyList<CoolingEquipmentCatalogSizingCandidate>> ListActiveCoolingCandidatesAsync(
        string systemType,
        string unitType,
        CancellationToken cancellationToken = default);
}