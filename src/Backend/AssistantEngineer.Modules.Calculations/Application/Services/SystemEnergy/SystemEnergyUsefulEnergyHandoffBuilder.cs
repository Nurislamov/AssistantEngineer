using AssistantEngineer.Modules.Calculations.Application.Contracts.AnnualEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy.En15316;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy.En15316;

namespace AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

public sealed class SystemEnergyUsefulEnergyHandoffBuilder
{
    private const string StandardBasedCalculationLabel = "Standard-Based Calculation";
    private const string SourceModule = "EnergyCalculationPipelineService";
    private readonly En15316SystemEnergyReferenceDataProvider _referenceDataProvider;

    public SystemEnergyUsefulEnergyHandoffBuilder(
        En15316SystemEnergyReferenceDataProvider referenceDataProvider)
    {
        _referenceDataProvider = referenceDataProvider ?? throw new ArgumentNullException(nameof(referenceDataProvider));
    }

    public SystemEnergyHandoffResult Build(
        BuildingEnergyBalanceResult buildingResult,
        SystemEnergyOptions options,
        DomesticHotWaterEn15316Handoff? dhwHandoff = null)
    {
        ArgumentNullException.ThrowIfNull(buildingResult);
        ArgumentNullException.ThrowIfNull(options);

        var diagnostics = new List<SystemEnergyHandoffDiagnostics>();
        var buildingEntries = BuildBuildingEntries(buildingResult, options, diagnostics);
        var dhwEntries = BuildDomesticHotWaterEntries(dhwHandoff, options, diagnostics);
        var timeSteps = BuildTimeSteps(buildingResult, buildingEntries, dhwEntries, diagnostics);

        var heatingDefaults = _referenceDataProvider.ResolveGenerationDefaults(options.DefaultHeatingTechnology);
        var heatingCircuitDefaults = _referenceDataProvider.ResolveHeatingCircuitDefaults(
            HeatingCircuitType.Radiator,
            En15316EmissionModelKind.Simplified);

        var circuit = new HeatingCircuit(
            CircuitId: "building-default",
            CircuitType: HeatingCircuitType.Radiator,
            Emission: new EmissionSystemModel(
                ModelKind: heatingCircuitDefaults.Emission.ModelKind,
                Efficiency: heatingCircuitDefaults.Emission.Efficiency,
                LossFactor: heatingCircuitDefaults.Emission.LossFactor),
            Distribution: new DistributionCircuitModel(
                Efficiency: heatingCircuitDefaults.Distribution.Efficiency,
                LossFactor: heatingCircuitDefaults.Distribution.LossFactor,
                AuxiliaryEnergyFraction: heatingCircuitDefaults.Distribution.AuxiliaryEnergyFraction),
            Generation: new GenerationSystemModel(
                Technology: options.DefaultHeatingTechnology,
                Carrier: options.DefaultHeatingCarrier,
                Efficiency: heatingDefaults.GenerationEfficiency ?? 1.0,
                Cop: heatingDefaults.GenerationEfficiency.HasValue ? null : heatingDefaults.GenerationCop,
                PrimaryEnergyFactor: 1.0),
            Storage: new StorageSystemModel(
                Efficiency: heatingCircuitDefaults.Storage.Efficiency,
                LossFactor: heatingCircuitDefaults.Storage.LossFactor),
            DesignFlowReturnTemperatureC: new FlowReturnTemperaturePair(55, 45));

        var en15316Input = new En15316HeatingSystemInput(
            CalculationId: $"B{buildingResult.BuildingId}-EN15316-HANDOFF",
            Circuits: [circuit],
            OperatingConditions:
            [
                new HeatingOperatingCondition(
                    ConditionId: "default",
                    FlowReturnTemperatureC: new FlowReturnTemperaturePair(55, 45))
            ],
            TimeSteps: timeSteps,
            DiagnosticsContext: $"Building {buildingResult.BuildingId} Standard-Based Calculation useful-energy handoff");

        var annualHeating = buildingEntries
            .Where(entry => entry.EnergyServiceType == SystemEnergyHandoffEnergyServiceType.SpaceHeating)
            .Sum(entry => entry.UsefulEnergyKWh);
        var annualCooling = buildingEntries
            .Where(entry => entry.EnergyServiceType == SystemEnergyHandoffEnergyServiceType.SpaceCooling)
            .Sum(entry => entry.UsefulEnergyKWh);
        var annualDhw = dhwEntries?.Sum(entry => entry.UsefulEnergyKWh) ?? 0.0;
        var coolingDefaults = _referenceDataProvider.ResolveGenerationDefaults(options.DefaultCoolingTechnology);

        var systemInput = new SystemEnergyInput(
            UsefulHeatingEnergyKWh: Round6(annualHeating),
            UsefulCoolingEnergyKWh: Round6(annualCooling),
            UsefulDhwEnergyKWh: Round6(annualDhw),
            HeatingEfficiency: null,
            HeatingCop: null,
            CoolingCop: coolingDefaults.GenerationCop,
            DhwEfficiency: null,
            DhwCop: null,
            FanEnergyKWh: 0,
            PrimaryEnergyFactor: null,
            DiagnosticsContext: $"Building {buildingResult.BuildingId} system-energy handoff",
            En15316HeatingCircuitInput: en15316Input);

        var buildingHandoff = new BuildingUsefulEnergyToSystemEnergyHandoff(
            BuildingId: buildingResult.BuildingId,
            CalculationMethodLabel: StandardBasedCalculationLabel,
            SourceModule: SourceModule,
            Entries: buildingEntries,
            Diagnostics: diagnostics.ToArray());

        DomesticHotWaterUsefulEnergyToSystemEnergyHandoff? mappedDhw = null;
        if (dhwEntries is not null)
        {
            mappedDhw = new DomesticHotWaterUsefulEnergyToSystemEnergyHandoff(
                CalculationId: dhwHandoff!.CalculationId,
                CalculationMethodLabel: StandardBasedCalculationLabel,
                SourceModule: "DomesticHotWaterSystemLoadCalculator",
                Entries: dhwEntries,
                Diagnostics: diagnostics.ToArray());
        }

        diagnostics.Add(new SystemEnergyHandoffDiagnostics(
            Severity: CalculationDiagnosticSeverity.Info,
            Code: "SystemEnergy.Handoff.Built",
            Message: "Standard-Based Calculation useful energy handoff was built for EN15316-style circuit-level system energy calculation.",
            Context: SourceModule));

        return new SystemEnergyHandoffResult(
            CalculationMethodLabel: StandardBasedCalculationLabel,
            SourceModule: SourceModule,
            BuildingHandoff: buildingHandoff,
            DomesticHotWaterHandoff: mappedDhw,
            SystemEnergyInput: systemInput,
            SystemEnergyResult: null,
            Diagnostics: diagnostics.ToArray());
    }

    private static IReadOnlyList<SystemEnergyHandoffEntry> BuildBuildingEntries(
        BuildingEnergyBalanceResult buildingResult,
        SystemEnergyOptions options,
        ICollection<SystemEnergyHandoffDiagnostics> diagnostics)
    {
        if (buildingResult.HourlyBalanceRecords.Count == 8760)
        {
            var entries = new List<SystemEnergyHandoffEntry>(8760 * 2);
            for (var index = 0; index < buildingResult.HourlyBalanceRecords.Count; index++)
            {
                var hour = buildingResult.HourlyBalanceRecords[index];
                var heatingKWh = Math.Max(0, hour.HeatingLoadW * hour.HourDurationH / 1000.0);
                var coolingKWh = Math.Max(0, hour.CoolingLoadW * hour.HourDurationH / 1000.0);

                entries.Add(new SystemEnergyHandoffEntry(
                    TimeStepIndex: index,
                    Month: hour.Month,
                    EnergyServiceType: SystemEnergyHandoffEnergyServiceType.SpaceHeating,
                    UsefulEnergyKWh: Round6(heatingKWh),
                    Carrier: options.DefaultHeatingCarrier));
                entries.Add(new SystemEnergyHandoffEntry(
                    TimeStepIndex: index,
                    Month: hour.Month,
                    EnergyServiceType: SystemEnergyHandoffEnergyServiceType.SpaceCooling,
                    UsefulEnergyKWh: Round6(coolingKWh),
                    Carrier: options.DefaultCoolingCarrier));
            }

            return entries;
        }

        diagnostics.Add(new SystemEnergyHandoffDiagnostics(
            Severity: CalculationDiagnosticSeverity.Info,
            Code: "SystemEnergy.Handoff.MonthlyFallback",
            Message: "True hourly 8760 useful-energy records were not available; monthly useful energy balances were used for system-energy handoff.",
            Context: SourceModule));

        var monthly = buildingResult.MonthlyBalances
            .GroupBy(item => item.Month)
            .ToDictionary(group => group.Key, group => group.First());

        var monthlyEntries = new List<SystemEnergyHandoffEntry>(24);
        for (var month = 1; month <= 12; month++)
        {
            var hasMonth = monthly.TryGetValue(month, out var value);
            var heatingKWh = hasMonth ? Math.Max(0, value!.HeatingDemandKWh) : 0.0;
            var coolingKWh = hasMonth ? Math.Max(0, value!.CoolingDemandKWh) : 0.0;
            var index = month - 1;

            monthlyEntries.Add(new SystemEnergyHandoffEntry(
                TimeStepIndex: index,
                Month: month,
                EnergyServiceType: SystemEnergyHandoffEnergyServiceType.SpaceHeating,
                UsefulEnergyKWh: Round6(heatingKWh),
                Carrier: options.DefaultHeatingCarrier));
            monthlyEntries.Add(new SystemEnergyHandoffEntry(
                TimeStepIndex: index,
                Month: month,
                EnergyServiceType: SystemEnergyHandoffEnergyServiceType.SpaceCooling,
                UsefulEnergyKWh: Round6(coolingKWh),
                Carrier: options.DefaultCoolingCarrier));
        }

        return monthlyEntries;
    }

    private static IReadOnlyList<SystemEnergyHandoffEntry>? BuildDomesticHotWaterEntries(
        DomesticHotWaterEn15316Handoff? dhwHandoff,
        SystemEnergyOptions options,
        ICollection<SystemEnergyHandoffDiagnostics> diagnostics)
    {
        if (dhwHandoff is null)
            return null;

        var hourly = dhwHandoff.HourlyUsefulDhwEnergyKWh8760;
        if (hourly.Count != 8760)
        {
            diagnostics.Add(new SystemEnergyHandoffDiagnostics(
                Severity: CalculationDiagnosticSeverity.Warning,
                Code: "SystemEnergy.Handoff.DhwHourlyProfileLength",
                Message: "DHW hourly useful profile does not contain 8760 values; monthly fallback was used.",
                Context: "DomesticHotWaterSystemLoadCalculator"));
            var monthly = AggregateMonthly(hourly);
            return Enumerable.Range(1, 12)
                .Select(month => new SystemEnergyHandoffEntry(
                    TimeStepIndex: month - 1,
                    Month: month,
                    EnergyServiceType: SystemEnergyHandoffEnergyServiceType.DomesticHotWater,
                    UsefulEnergyKWh: Round6(monthly[month - 1]),
                    Carrier: options.DefaultDhwCarrier))
                .ToArray();
        }

        var entries = new List<SystemEnergyHandoffEntry>(8760);
        for (var index = 0; index < 8760; index++)
        {
            entries.Add(new SystemEnergyHandoffEntry(
                TimeStepIndex: index,
                Month: MonthFromHourIndex(index),
                EnergyServiceType: SystemEnergyHandoffEnergyServiceType.DomesticHotWater,
                UsefulEnergyKWh: Round6(Math.Max(0, hourly[index])),
                Carrier: options.DefaultDhwCarrier));
        }

        return entries;
    }

    private static IReadOnlyList<HeatingSystemTimeStepInput> BuildTimeSteps(
        BuildingEnergyBalanceResult buildingResult,
        IReadOnlyList<SystemEnergyHandoffEntry> buildingEntries,
        IReadOnlyList<SystemEnergyHandoffEntry>? dhwEntries,
        ICollection<SystemEnergyHandoffDiagnostics> diagnostics)
    {
        var heatingByStep = buildingEntries
            .Where(entry => entry.EnergyServiceType == SystemEnergyHandoffEnergyServiceType.SpaceHeating)
            .ToDictionary(entry => entry.TimeStepIndex, entry => entry, EqualityComparer<int>.Default);
        var coolingByStep = buildingEntries
            .Where(entry => entry.EnergyServiceType == SystemEnergyHandoffEnergyServiceType.SpaceCooling)
            .ToDictionary(entry => entry.TimeStepIndex, entry => entry, EqualityComparer<int>.Default);
        var dhwByStep = dhwEntries?
            .ToDictionary(entry => entry.TimeStepIndex, entry => entry, EqualityComparer<int>.Default)
            ?? new Dictionary<int, SystemEnergyHandoffEntry>();

        var hasHourly = buildingResult.HourlyBalanceRecords.Count == 8760;
        var stepCount = hasHourly ? 8760 : 12;
        var steps = new List<HeatingSystemTimeStepInput>(stepCount);

        for (var index = 0; index < stepCount; index++)
        {
            var month = hasHourly
                ? buildingResult.HourlyBalanceRecords[index].Month
                : index + 1;
            var heating = heatingByStep.GetValueOrDefault(index)?.UsefulEnergyKWh ?? 0.0;
            var dhw = dhwByStep.GetValueOrDefault(index)?.UsefulEnergyKWh ?? 0.0;

            steps.Add(new HeatingSystemTimeStepInput(
                TimeStepIndex: index,
                Month: month,
                UsefulHeatingLoadKWh: Round6(heating),
                UsefulDhwLoadKWh: Round6(dhw)));
        }

        if (dhwEntries is not null && dhwEntries.Count > 0 && dhwEntries.Count != stepCount)
        {
            diagnostics.Add(new SystemEnergyHandoffDiagnostics(
                Severity: CalculationDiagnosticSeverity.Warning,
                Code: "SystemEnergy.Handoff.DhwStepAlignment",
                Message: "DHW useful-energy profile length did not match building timestep length; unmatched timesteps were truncated or zero-filled.",
                Context: SourceModule));
        }

        if (coolingByStep.Values.Sum(entry => entry.UsefulEnergyKWh) > 0)
        {
            diagnostics.Add(new SystemEnergyHandoffDiagnostics(
                Severity: CalculationDiagnosticSeverity.Info,
                Code: "SystemEnergy.Handoff.CoolingPreserved",
                Message: "Space cooling useful energy was preserved in handoff metadata and passed through SystemEnergyInput cooling fields.",
                Context: SourceModule));
        }

        return steps;
    }

    private static IReadOnlyList<double> AggregateMonthly(IReadOnlyList<double> hourlyProfile)
    {
        var monthly = new double[12];
        var daysPerMonth = new[] { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
        var hourOffset = 0;

        for (var month = 0; month < 12; month++)
        {
            var hours = daysPerMonth[month] * 24;
            var sum = 0.0;
            for (var index = 0; index < hours && hourOffset + index < hourlyProfile.Count; index++)
                sum += Math.Max(0, hourlyProfile[hourOffset + index]);

            monthly[month] = Round6(sum);
            hourOffset += hours;
        }

        return monthly;
    }

    private static int MonthFromHourIndex(int hourIndex)
    {
        var dayOfYear = hourIndex / 24;
        var daysPerMonth = new[] { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
        var accumulatedDays = 0;
        for (var month = 1; month <= 12; month++)
        {
            accumulatedDays += daysPerMonth[month - 1];
            if (dayOfYear < accumulatedDays)
                return month;
        }

        return 12;
    }

    private static double Round6(double value) =>
        Math.Round(value, 6, MidpointRounding.AwayFromZero);
}
