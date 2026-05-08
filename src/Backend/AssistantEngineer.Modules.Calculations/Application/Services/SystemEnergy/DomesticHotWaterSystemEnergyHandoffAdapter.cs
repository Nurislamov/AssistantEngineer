using AssistantEngineer.Modules.Calculations.Application.Abstractions.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

public sealed class DomesticHotWaterSystemEnergyHandoffAdapter : IDomesticHotWaterSystemEnergyHandoffAdapter
{
    private const string Source = "DomesticHotWaterSystemEnergyHandoffAdapter";

    public SystemEnergyUsefulLoadSet BuildUsefulLoadSet(DomesticHotWaterEn15316Handoff handoff)
    {
        ArgumentNullException.ThrowIfNull(handoff);

        var usefulDiagnostics = new List<StandardCalculationDiagnostic>
        {
            CreateInfo(
                "AE-SYS-DHW-HANDOFF-ADAPTED",
                "DHW EN15316 handoff was adapted into canonical system-energy useful-load input.")
        };

        var auxiliaryDiagnostics = new List<StandardCalculationDiagnostic>
        {
            CreateInfo(
                "AE-SYS-DHW-HANDOFF-AUXILIARY-ADAPTED",
                "DHW auxiliary electricity handoff profile was adapted into canonical auxiliary-load input.")
        };

        var usefulHourly = SystemEnergyProfileHelper.Ensure8760(handoff.HourlyDhwSystemHeatRequirementKWh8760);
        var usefulMonthly = SystemEnergyProfileHelper.AggregateMonthly(usefulHourly);

        var usefulLoad = new SystemEnergyUsefulLoadInput(
            LoadId: $"{handoff.CalculationId}-DHW-SYSTEM-LOAD",
            BuildingId: null,
            ZoneId: null,
            RoomId: null,
            EndUse: SystemEnergyEndUse.DomesticHotWater,
            HourlyUsefulEnergyKWh8760: usefulHourly,
            MonthlyUsefulEnergyKWh: usefulMonthly,
            AnnualUsefulEnergyKWh: usefulHourly.Sum(),
            Source: "AE-DHW-ISO12831-001A useful demand + AE-DHW-ISO12831-001B system load preparation",
            Diagnostics: usefulDiagnostics);

        var auxiliaryHourly = SystemEnergyProfileHelper.Ensure8760(handoff.HourlyDhwAuxiliaryElectricityKWh8760);

        var auxiliaryLoad = new SystemEnergyAuxiliaryLoadInput(
            AuxiliaryId: $"{handoff.CalculationId}-DHW-AUXILIARY",
            BuildingId: null,
            ZoneId: null,
            RoomId: null,
            EndUse: SystemEnergyEndUse.DomesticHotWater,
            Carrier: SystemEnergyCarrier.Electricity,
            HourlyAuxiliaryEnergyKWh8760: auxiliaryHourly,
            Source: "DHW EN15316 handoff auxiliary electricity",
            Diagnostics: auxiliaryDiagnostics);

        return new SystemEnergyUsefulLoadSet(
            CalculationId: handoff.CalculationId,
            UsefulLoads: [usefulLoad],
            AuxiliaryLoads: [auxiliaryLoad],
            DisclosureOverride: null,
            Source: "DHW EN15316 handoff adapter");
    }

    private static StandardCalculationDiagnostic CreateInfo(
        string code,
        string message) =>
        SystemEnergyDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Info,
            code,
            message,
            Source);
}
