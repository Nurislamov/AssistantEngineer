using AssistantEngineer.Modules.Calculations.Application.Abstractions.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;

public sealed class DomesticHotWaterEn15316HandoffBuilder : IDomesticHotWaterEn15316HandoffBuilder
{
    public DomesticHotWaterEn15316Handoff Build(DomesticHotWaterSystemLoadResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var diagnostics = new List<StandardCalculationDiagnostic>();
        diagnostics.Add(CreateInfo(
            "AE-DHW-EN15316-HANDOFF-ONLY",
            "DHW EN15316 handoff is provided for downstream system-energy calculation and is not a final/primary energy result."));

        return new DomesticHotWaterEn15316Handoff(
            CalculationId: result.CalculationId,
            EndUse: "DomesticHotWater",
            UsefulEnergySource: "AE-DHW-ISO12831-001A useful demand + AE-DHW-ISO12831-001B system load preparation",
            AnnualUsefulDhwEnergyKWh: result.AnnualUsefulEnergyKWh,
            AnnualDhwSystemHeatRequirementKWh: result.AnnualSystemHeatRequirementKWh,
            AnnualDhwAuxiliaryElectricityKWh: result.AnnualAuxiliaryElectricityKWh,
            HourlyUsefulDhwEnergyKWh8760: result.UsefulDemand.HourlyUsefulEnergyKWh8760,
            HourlyDhwSystemHeatRequirementKWh8760: result.HourlySystemHeatRequirementKWh8760,
            HourlyDhwAuxiliaryElectricityKWh8760: result.HourlyAuxiliaryElectricityKWh8760,
            HourlyRecoverableLossKWh8760: result.HourlyRecoverableLossKWh8760,
            HourlyNonRecoverableLossKWh8760: result.HourlyNonRecoverableLossKWh8760,
            Diagnostics: diagnostics);
    }

    private static StandardCalculationDiagnostic CreateInfo(
        string code,
        string message) =>
        DomesticHotWaterDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Info,
            code,
            message,
            StandardCalculationStage.SystemEnergy,
            "DomesticHotWaterEn15316HandoffBuilder");
}
