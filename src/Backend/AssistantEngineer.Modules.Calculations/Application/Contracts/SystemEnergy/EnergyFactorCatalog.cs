using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record EnergyFactorCatalog(
    string CatalogId,
    string Version,
    IReadOnlyList<EnergyFactorCatalogEntry> Entries,
    string? Source = null,
    IReadOnlyList<StandardCalculationDiagnostic>? Diagnostics = null);
