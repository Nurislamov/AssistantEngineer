using AssistantEngineer.Modules.Calculations.Application.Abstractions.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;

namespace AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

public sealed class SystemEnergyLoadIntakeBuilder : ISystemEnergyLoadIntakeBuilder
{
    private const string Source = "SystemEnergyLoadIntakeBuilder";

    public SystemEnergyLoadIntakeResult Build(SystemEnergyLoadIntakeRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var assumptions = new List<string>();
        var warnings = new List<string>();
        var diagnostics = new List<StandardCalculationDiagnostic>();

        var timeStep = request.TimeStepHours > 0.0 && double.IsFinite(request.TimeStepHours)
            ? request.TimeStepHours
            : 1.0;
        if (timeStep != request.TimeStepHours)
        {
            warnings.Add("TimeStepHours defaulted to 1h because input value was invalid.");
            diagnostics.Add(CreateWarning(
                "AE-SYS-INTAKE-TIMESTEP-DEFAULTED",
                "System energy intake timestep hours should be positive and finite."));
        }

        var usefulLoads = new List<SystemEnergyUsefulLoadInput>();
        AddLoad(request.HeatingUsefulProfileKWh, SystemEnergyEndUse.SpaceHeating, "Heating", request, usefulLoads, diagnostics, warnings);
        AddLoad(request.CoolingUsefulProfileKWh, SystemEnergyEndUse.SpaceCooling, "Cooling", request, usefulLoads, diagnostics, warnings);
        AddDhwLoad(request.DhwHandoff, request, usefulLoads, diagnostics, warnings);

        var auxiliaryLoads = new List<SystemEnergyAuxiliaryLoadInput>();
        if (request.AuxiliaryElectricityProfileKWh is { Count: > 0 } auxiliaryProfile)
        {
            var normalized = NormalizeProfile(auxiliaryProfile, request.NormalizeSignedLoads, "Auxiliary", diagnostics, warnings);
            auxiliaryLoads.Add(new SystemEnergyAuxiliaryLoadInput(
                AuxiliaryId: $"{request.CalculationId}-AUX",
                BuildingId: null,
                ZoneId: null,
                RoomId: null,
                EndUse: SystemEnergyEndUse.Auxiliary,
                Carrier: SystemEnergyCarrier.Electricity,
                HourlyAuxiliaryEnergyKWh8760: normalized,
                Source: request.Source ?? Source,
                Diagnostics: []));
        }

        assumptions.Add("Heating, cooling and DHW are kept as separate end uses.");
        assumptions.Add("Sign convention normalization converts signed useful loads into positive demand magnitudes.");
        assumptions.Add("DHW profile uses handoff ownership policy to avoid double counting.");

        var loadSet = new SystemEnergyUsefulLoadSet(
            CalculationId: request.CalculationId,
            UsefulLoads: usefulLoads,
            AuxiliaryLoads: auxiliaryLoads,
            DisclosureOverride: null,
            Source: request.Source ?? Source);

        return new SystemEnergyLoadIntakeResult(
            UsefulLoadSet: loadSet,
            Assumptions: assumptions.ToArray(),
            Warnings: warnings.ToArray(),
            Diagnostics: SortDiagnostics(diagnostics));
    }

    private static void AddLoad(
        IReadOnlyList<double>? profile,
        SystemEnergyEndUse endUse,
        string label,
        SystemEnergyLoadIntakeRequest request,
        ICollection<SystemEnergyUsefulLoadInput> usefulLoads,
        ICollection<StandardCalculationDiagnostic> diagnostics,
        ICollection<string> warnings)
    {
        if (profile is not { Count: > 0 })
            return;

        var normalized = NormalizeProfile(profile, request.NormalizeSignedLoads, label, diagnostics, warnings);
        var monthly = normalized.Count == 8760
            ? SystemEnergyProfileHelper.AggregateMonthly(normalized)
            : null;

        usefulLoads.Add(new SystemEnergyUsefulLoadInput(
            LoadId: $"{request.CalculationId}-{endUse}",
            BuildingId: null,
            ZoneId: null,
            RoomId: null,
            EndUse: endUse,
            HourlyUsefulEnergyKWh8760: normalized,
            MonthlyUsefulEnergyKWh: monthly,
            AnnualUsefulEnergyKWh: normalized.Sum(),
            Source: request.Source ?? Source,
            Diagnostics: [],
            HourlySystemLoadKWh8760: null,
            TimeStepHours: request.TimeStepHours,
            LossOwnershipPolicy: request.LossOwnershipPolicy,
            Assumptions: [$"{label} useful load intake from source module '{request.Source ?? Source}'"]));
    }

    private static void AddDhwLoad(
        DomesticHotWaterEn15316Handoff? dhwHandoff,
        SystemEnergyLoadIntakeRequest request,
        ICollection<SystemEnergyUsefulLoadInput> usefulLoads,
        ICollection<StandardCalculationDiagnostic> diagnostics,
        ICollection<string> warnings)
    {
        if (dhwHandoff is null)
            return;

        var useSystemLoad = dhwHandoff.LossOwnershipPolicy != DomesticHotWaterLossOwnershipPolicy.SystemEnergyOwnLosses;
        var profile = useSystemLoad
            ? dhwHandoff.HourlyDhwSystemHeatRequirementKWh8760
            : dhwHandoff.HourlyUsefulDhwEnergyKWh8760;
        var normalized = NormalizeProfile(profile, request.NormalizeSignedLoads, "DHW", diagnostics, warnings);

        diagnostics.Add(CreateInfo(
            "AE-SYS-INTAKE-DHW-OWNERSHIP",
            $"DHW intake applied ownership policy '{dhwHandoff.LossOwnershipPolicy}'."));

        usefulLoads.Add(new SystemEnergyUsefulLoadInput(
            LoadId: $"{request.CalculationId}-DHW",
            BuildingId: null,
            ZoneId: null,
            RoomId: null,
            EndUse: SystemEnergyEndUse.DomesticHotWater,
            HourlyUsefulEnergyKWh8760: normalized,
            MonthlyUsefulEnergyKWh: normalized.Count == 8760 ? SystemEnergyProfileHelper.AggregateMonthly(normalized) : null,
            AnnualUsefulEnergyKWh: normalized.Sum(),
            Source: "DomesticHotWaterEn15316Handoff",
            Diagnostics: [],
            HourlySystemLoadKWh8760: useSystemLoad ? normalized : null,
            TimeStepHours: request.TimeStepHours,
            LossOwnershipPolicy: MapOwnership(dhwHandoff.LossOwnershipPolicy),
            Assumptions:
            [
                useSystemLoad
                    ? "DHW system-load lane accepted from upstream handoff."
                    : "DHW useful-load lane accepted from upstream handoff; system-energy chain owns losses."
            ]));
    }

    private static SystemEnergyLossOwnershipPolicy MapOwnership(DomesticHotWaterLossOwnershipPolicy policy) =>
        policy switch
        {
            DomesticHotWaterLossOwnershipPolicy.DhwOwnLosses => SystemEnergyLossOwnershipPolicy.UpstreamOwnsLosses,
            DomesticHotWaterLossOwnershipPolicy.SystemEnergyOwnLosses => SystemEnergyLossOwnershipPolicy.SystemEnergyOwnsLosses,
            _ => SystemEnergyLossOwnershipPolicy.NoDoubleCounting
        };

    private static IReadOnlyList<double> NormalizeProfile(
        IReadOnlyList<double> profile,
        bool normalizeSignedLoads,
        string label,
        ICollection<StandardCalculationDiagnostic> diagnostics,
        ICollection<string> warnings)
    {
        IReadOnlyList<double> values;
        if (profile.Count == 12)
        {
            values = SystemEnergyProfileHelper.ExpandMonthlyToHourly(profile);
        }
        else if (profile.Count == 8760)
        {
            values = profile;
        }
        else
        {
            diagnostics.Add(CreateError(
                "AE-SYS-INTAKE-PROFILE-LENGTH-MISMATCH",
                $"{label} profile must contain 8760 hourly or 12 monthly values."));
            values = new double[8760];
        }

        var result = new double[8760];
        for (var index = 0; index < result.Length; index++)
        {
            var value = index < values.Count ? values[index] : 0.0;
            if (!double.IsFinite(value))
            {
                diagnostics.Add(CreateError(
                    "AE-SYS-INTAKE-PROFILE-NAN",
                    $"{label} profile contains non-finite value."));
                value = 0.0;
            }

            if (value < 0.0)
            {
                if (normalizeSignedLoads)
                {
                    warnings.Add($"{label} profile had negative values and was normalized to positive demand.");
                    diagnostics.Add(CreateWarning(
                        "AE-SYS-INTAKE-SIGNED-NORMALIZED",
                        $"{label} signed profile was normalized to positive demand values."));
                    value = Math.Abs(value);
                }
                else
                {
                    diagnostics.Add(CreateError(
                        "AE-SYS-INTAKE-NEGATIVE-ENERGY",
                        $"{label} profile contains negative values."));
                    value = 0.0;
                }
            }

            result[index] = value;
        }

        return result;
    }

    private static IReadOnlyList<StandardCalculationDiagnostic> SortDiagnostics(
        IEnumerable<StandardCalculationDiagnostic> diagnostics) =>
        diagnostics
            .OrderByDescending(item => item.Severity)
            .ThenBy(item => item.Code, StringComparer.Ordinal)
            .ThenBy(item => item.Message, StringComparer.Ordinal)
            .ToArray();

    private static StandardCalculationDiagnostic CreateInfo(
        string code,
        string message) =>
        SystemEnergyDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Info,
            code,
            message,
            Source);

    private static StandardCalculationDiagnostic CreateWarning(
        string code,
        string message) =>
        SystemEnergyDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Warning,
            code,
            message,
            Source);

    private static StandardCalculationDiagnostic CreateError(
        string code,
        string message) =>
        SystemEnergyDiagnosticsFactory.Create(
            CalculationDiagnosticSeverity.Error,
            code,
            message,
            Source);
}
