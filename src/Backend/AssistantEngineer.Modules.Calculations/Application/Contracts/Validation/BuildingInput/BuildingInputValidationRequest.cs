using AssistantEngineer.Modules.Buildings.Domain.Entities;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Validation.BuildingInput;

public sealed record BuildingInputValidationRequest(
    Building Building,
    bool EvaluateIso52016Readiness = true,
    bool IsConstructionLayerMassOptInIntended = false,
    bool DhwExpected = false,
    int? DhwPeopleCount = null,
    double? DhwLitersPerPersonPerDay = null,
    bool SystemEnergyExpected = false,
    double? SystemUsefulHeatingEnergyKWh = null,
    double? SystemUsefulCoolingEnergyKWh = null,
    double? SystemUsefulDhwEnergyKWh = null,
    double? SystemHeatingEfficiency = null,
    double? SystemHeatingCop = null,
    double? SystemCoolingCop = null,
    double? SystemDhwEfficiency = null,
    double? SystemDhwCop = null,
    double? SystemPrimaryEnergyFactor = null);
