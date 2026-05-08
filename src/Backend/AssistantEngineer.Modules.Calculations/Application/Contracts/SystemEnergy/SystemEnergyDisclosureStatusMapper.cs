using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;

namespace AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

public static class SystemEnergyDisclosureStatusMapper
{
    public static SystemEnergyDisclosureStatus FromStandardMode(StandardCalculationMode mode) =>
        mode switch
        {
            StandardCalculationMode.InternalEngineering => SystemEnergyDisclosureStatus.InternalEngineering,
            StandardCalculationMode.StandardInspired => SystemEnergyDisclosureStatus.StandardInspired,
            StandardCalculationMode.ExternalAnchor => SystemEnergyDisclosureStatus.RequiresExternalValidation,
            StandardCalculationMode.Fallback => SystemEnergyDisclosureStatus.HandoffOnly,
            StandardCalculationMode.Disabled => SystemEnergyDisclosureStatus.HandoffOnly,
            StandardCalculationMode.Compatibility => SystemEnergyDisclosureStatus.NotForCompliance,
            _ => SystemEnergyDisclosureStatus.Unknown
        };
}
