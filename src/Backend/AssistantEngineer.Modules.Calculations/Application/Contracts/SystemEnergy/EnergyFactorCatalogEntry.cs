using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public sealed record EnergyFactorCatalogEntry(
    SystemEnergyCarrierKind CarrierKind,
    double? PrimaryEnergyFactorNonRenewable,
    double? PrimaryEnergyFactorRenewable,
    double TotalPrimaryEnergyFactor,
    double Co2FactorKgPerKWh,
    string SourceLabel,
    DateOnly? EffectiveDate = null,
    IReadOnlyList<StandardCalculationDiagnostic>? Diagnostics = null);
