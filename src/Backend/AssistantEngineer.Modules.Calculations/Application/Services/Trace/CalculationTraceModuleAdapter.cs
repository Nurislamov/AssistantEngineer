using AssistantEngineer.Modules.Calculations.Application.Abstractions.Trace;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Trace;

public sealed class CalculationTraceModuleAdapter : ICalculationTraceModuleAdapter
{
    private readonly ICalculationTraceDiagnosticMapper _diagnosticMapper;
    private readonly ICalculationTraceSanitizer _sanitizer;

    public CalculationTraceModuleAdapter(
        ICalculationTraceDiagnosticMapper diagnosticMapper,
        ICalculationTraceSanitizer sanitizer)
    {
        _diagnosticMapper = diagnosticMapper;
        _sanitizer = sanitizer;
    }

    public CalculationTraceDocument BuildWeatherSolarTrace(
        WeatherSolarTraceSource source,
        CalculationTraceDetailLevel detailLevel = CalculationTraceDetailLevel.Standard)
    {
        ArgumentNullException.ThrowIfNull(source);

        var builder = CreateInitializedBuilder(
            detailLevel,
            traceId: $"trace-weather-solar-{source.Context.Year}",
            calculationType: "WeatherSolarFoundation",
            rootModule: CalculationTraceModuleKind.Weather);

        var weatherStep = builder.AddStep(
            CalculationTraceModuleKind.Weather,
            "Weather source and solar context intake");
        builder.AddInputValue(weatherStep, TraceValue("weatherSource", "Weather source", source.WeatherSource, null));
        builder.AddInputValue(weatherStep, TraceValue("year", "Weather year", source.Context.Year, null));
        builder.AddInputValue(weatherStep, TraceValue("timeZoneOffsetHours", "Timezone offset", source.Context.TimeZoneOffset.TotalHours, Unit("h")));
        builder.AddInputValue(weatherStep, TraceValue("latitudeDegrees", "Latitude", source.Context.LatitudeDegrees, Unit("deg")));
        builder.AddInputValue(weatherStep, TraceValue("longitudeDegrees", "Longitude", source.Context.LongitudeDegrees, Unit("deg")));
        builder.AddOutputValue(weatherStep, TraceValue("hourCount", "Hourly profile length", source.Context.HourCount, Unit("h")));
        builder.AddAssumption(weatherStep, "Local solar time assumptions use longitude and timezone offset from the weather context.");
        if (source.Assumptions is not null)
        {
            foreach (var assumption in source.Assumptions)
            {
                builder.AddAssumption(weatherStep, assumption);
            }
        }

        foreach (var diagnostic in source.Context.Diagnostics)
        {
            builder.AddDiagnostic(weatherStep, _diagnosticMapper.Map(diagnostic, CalculationTraceModuleKind.Weather));
        }

        var solarStep = builder.AddStep(
            CalculationTraceModuleKind.Solar,
            "Solar position step",
            formulaOrConventionLabel: "ISO52010-inspired position chain");

        builder.AddInputValue(solarStep, TraceValue("surfaceCount", "Reference surfaces per hour", GetSurfaceCount(source.Context), null));
        builder.AddOutputValue(solarStep, TraceValue("avgSolarAltitudeDegrees", "Average solar altitude", Average(source.Context.Hours.Select(hour => hour.SolarAltitudeDegrees)), Unit("deg")));
        builder.AddOutputValue(solarStep, TraceValue("avgSolarAzimuthDegrees", "Average solar azimuth", Average(source.Context.Hours.Select(hour => hour.SolarAzimuthDegrees)), Unit("deg")));
        builder.AddIntermediateValue(
            solarStep,
            TraceValue(
                "solarAltitudeProfile",
                "Solar altitude profile",
                source.Context.Hours.Select(hour => hour.SolarAltitudeDegrees).ToArray(),
                Unit("deg"),
                CalculationTraceValueKind.Intermediate));

        var irradianceStep = builder.AddStep(
            CalculationTraceModuleKind.Solar,
            "Surface irradiance step",
            formulaOrConventionLabel: "Perez anisotropic irradiance convention");

        builder.AddInputValue(irradianceStep, TraceValue("dniWm2", "Direct normal irradiance mean", Average(source.Context.Hours.Select(hour => hour.DirectNormalIrradianceWm2)), Unit("W/m2")));
        builder.AddInputValue(irradianceStep, TraceValue("dhiWm2", "Diffuse horizontal irradiance mean", Average(source.Context.Hours.Select(hour => hour.DiffuseHorizontalIrradianceWm2)), Unit("W/m2")));
        builder.AddOutputValue(irradianceStep, TraceValue("ghiWm2", "Global horizontal irradiance mean", Average(source.Context.Hours.Select(hour => hour.GlobalHorizontalIrradianceWm2)), Unit("W/m2")));
        builder.AddOutputValue(irradianceStep, TraceValue("maxSurfaceIrradianceWm2", "Maximum surface irradiance", MaxSurfaceIrradiance(source.Context), Unit("W/m2")));

        return _sanitizer.Sanitize(builder.Build(), detailLevel);
    }

    public CalculationTraceDocument BuildThermalTopologyTrace(
        ThermalTopologyTraceSource source,
        CalculationTraceDetailLevel detailLevel = CalculationTraceDetailLevel.Standard)
    {
        ArgumentNullException.ThrowIfNull(source);

        var builder = CreateInitializedBuilder(
            detailLevel,
            traceId: $"trace-topology-{source.Topology.BuildingId}",
            calculationType: "ThermalTopologyFoundation",
            rootModule: CalculationTraceModuleKind.ThermalTopology,
            calculationId: source.Topology.BuildingId);

        var intakeStep = builder.AddStep(
            CalculationTraceModuleKind.ThermalTopology,
            "Thermal zones and boundaries intake");

        builder.AddInputValue(intakeStep, TraceValue("zoneCount", "Zone count", source.Topology.Zones.Count, null));
        builder.AddInputValue(intakeStep, TraceValue("roomCount", "Room count", source.Topology.Rooms.Count, null));
        builder.AddInputValue(intakeStep, TraceValue("surfaceCount", "Boundary surface count", source.Topology.Surfaces.Count, null));
        builder.AddInputValue(intakeStep, TraceValue("adjacentBoundaryPolicy", "Adjacent boundary policy", source.AdjacentBoundaryPolicy, null));

        var boundarySummary = source.Topology.Surfaces
            .GroupBy(surface => surface.BoundaryKind.ToString())
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);

        builder.AddOutputValue(intakeStep, TraceValue("boundaryClassificationSummary", "Boundary classification summary", boundarySummary, null));
        builder.AddAssumption(intakeStep, "Same-use adjacent boundaries may be treated as adiabatic by thermal-topology policy.");

        foreach (var assumption in source.Topology.Disclosure.ClaimBoundary.Assumptions)
        {
            builder.AddAssumption(intakeStep, assumption);
        }

        if (source.Assumptions is not null)
        {
            foreach (var assumption in source.Assumptions)
            {
                builder.AddAssumption(intakeStep, assumption);
            }
        }

        foreach (var diagnostic in source.Topology.Diagnostics)
        {
            builder.AddDiagnostic(intakeStep, _diagnosticMapper.Map(diagnostic, CalculationTraceModuleKind.ThermalTopology));
        }

        var validationStep = builder.AddStep(
            CalculationTraceModuleKind.Validation,
            "Topology validation diagnostics",
            parentStepId: intakeStep);

        foreach (var diagnostic in source.Topology.Diagnostics)
        {
            builder.AddDiagnostic(validationStep, _diagnosticMapper.Map(diagnostic, CalculationTraceModuleKind.Validation));
        }

        builder.AddOutputValue(validationStep, TraceValue("hasInvalidTopology", "Invalid topology diagnostics present", HasErrorDiagnostics(source.Topology.Diagnostics), null));

        return _sanitizer.Sanitize(builder.Build(), detailLevel);
    }

    public CalculationTraceDocument BuildIso52016MultiZoneTrace(
        Iso52016MultiZoneTraceSource source,
        CalculationTraceDetailLevel detailLevel = CalculationTraceDetailLevel.Standard)
    {
        ArgumentNullException.ThrowIfNull(source);

        var builder = CreateInitializedBuilder(
            detailLevel,
            traceId: $"trace-iso52016-multizone-{source.Result.BuildingId}",
            calculationType: "Iso52016MultiZoneFoundation",
            rootModule: CalculationTraceModuleKind.MultiZone,
            calculationId: source.Result.BuildingId);

        var modeStep = builder.AddStep(
            CalculationTraceModuleKind.Iso52016,
            "ISO52016 multi-zone mode and profile intake");

        builder.AddInputValue(modeStep, TraceValue("calculationMode", "Calculation mode", source.CalculationMode, null));
        builder.AddInputValue(modeStep, TraceValue("zoneCount", "Zone count", source.Result.Zones.Count, null));
        builder.AddInputValue(modeStep, TraceValue("timeStepCount", "Timestep/profile length", source.TimeStepCount, null));
        builder.AddOutputValue(modeStep, TraceValue("hourlyResultCount", "Hourly result count", source.Result.HourlyResults.Count, null));

        var loadStep = builder.AddStep(
            CalculationTraceModuleKind.MultiZone,
            "Transmission/ventilation/solar/internal lanes and heating/cooling summary");

        builder.AddOutputValue(loadStep, TraceValue("annualHeatingNeedKWh", "Annual heating need", source.Result.AnnualSummary.AnnualHeatingEnergyTotalKWh, Unit("kWh")));
        builder.AddOutputValue(loadStep, TraceValue("annualCoolingNeedKWh", "Annual cooling need", source.Result.AnnualSummary.AnnualCoolingEnergyTotalKWh, Unit("kWh")));
        builder.AddOutputValue(
            loadStep,
            TraceValue(
                "laneSummary",
                "Load-lane summary convention",
                "Transmission, ventilation, solar and internal-gain lanes are represented through aggregated hourly/annual zone loads in this foundation trace.",
                null));

        builder.AddIntermediateValue(
            loadStep,
            TraceValue(
                "buildingHeatingProfileW",
                "Building heating profile",
                source.Result.HourlyResults.Select(item => item.BuildingHeatingLoadW).ToArray(),
                Unit("W"),
                CalculationTraceValueKind.Intermediate));

        builder.AddIntermediateValue(
            loadStep,
            TraceValue(
                "buildingCoolingProfileW",
                "Building cooling profile",
                source.Result.HourlyResults.Select(item => item.BuildingCoolingLoadW).ToArray(),
                Unit("W"),
                CalculationTraceValueKind.Intermediate));

        foreach (var assumption in source.Assumptions ?? [])
        {
            builder.AddAssumption(loadStep, assumption);
        }

        foreach (var diagnostic in source.Result.Diagnostics)
        {
            builder.AddDiagnostic(loadStep, _diagnosticMapper.Map(diagnostic, CalculationTraceModuleKind.MultiZone));
        }

        return _sanitizer.Sanitize(builder.Build(), detailLevel);
    }

    public CalculationTraceDocument BuildNaturalVentilationTrace(
        NaturalVentilationTraceSource source,
        CalculationTraceDetailLevel detailLevel = CalculationTraceDetailLevel.Standard)
    {
        ArgumentNullException.ThrowIfNull(source);

        var builder = CreateInitializedBuilder(
            detailLevel,
            traceId: "trace-natural-ventilation",
            calculationType: "NaturalVentilationFoundation",
            rootModule: CalculationTraceModuleKind.Ventilation);

        var intakeStep = builder.AddStep(
            CalculationTraceModuleKind.Ventilation,
            "Natural ventilation control intake");

        builder.AddInputValue(intakeStep, TraceValue("openingCount", "Opening count", source.OpeningCount, null));
        builder.AddInputValue(intakeStep, TraceValue("controlMode", "Control mode", source.ControlMode, null));
        builder.AddOutputValue(intakeStep, TraceValue("selectedBranch", "Selected control branch", source.Result.SelectedBranch, null));

        if (!string.IsNullOrWhiteSpace(source.Result.ControlReason))
        {
            builder.AddDiagnostic(
                intakeStep,
                _diagnosticMapper.Map(
                    "AE-TRACE-VENT-CONTROL",
                    source.Result.ControlReason,
                    CalculationTraceSeverity.Info,
                    CalculationTraceModuleKind.Ventilation));
        }

        var airflowStep = builder.AddStep(
            CalculationTraceModuleKind.Ventilation,
            "Airflow and Hve summary");

        builder.AddOutputValue(airflowStep, TraceValue("totalAirflowM3PerS", "Total airflow", source.Result.TotalAirflowM3PerS, Unit("m3/s")));
        builder.AddOutputValue(airflowStep, TraceValue("airChangesPerHour", "Air changes per hour", source.Result.ClampedAirChangesPerHour, Unit("1/h")));
        builder.AddOutputValue(airflowStep, TraceValue("heatTransferCoefficientWPerK", "Ventilation heat-transfer coefficient (Hve)", source.Result.HeatTransferCoefficientWPerK, Unit("W/K")));
        builder.AddOutputValue(airflowStep, TraceValue("stackComponentM3PerS", "Stack airflow component", source.Result.StackAirflowM3PerS, Unit("m3/s")));
        builder.AddOutputValue(airflowStep, TraceValue("windComponentM3PerS", "Wind airflow component", source.Result.WindAirflowM3PerS, Unit("m3/s")));

        if (!string.IsNullOrWhiteSpace(source.Result.ClampReason))
        {
            builder.AddWarning(airflowStep, source.Result.ClampReason);
        }

        foreach (var assumption in source.Assumptions ?? [])
        {
            builder.AddAssumption(airflowStep, assumption);
        }

        foreach (var diagnostic in source.Result.Diagnostics)
        {
            builder.AddDiagnostic(
                airflowStep,
                _diagnosticMapper.Map(
                    diagnostic.Code,
                    diagnostic.Message,
                    InferSeverityFromCode(diagnostic.Code),
                    CalculationTraceModuleKind.Ventilation));
        }

        return _sanitizer.Sanitize(builder.Build(), detailLevel);
    }

    public CalculationTraceDocument BuildGroundTrace(
        GroundTraceSource source,
        CalculationTraceDetailLevel detailLevel = CalculationTraceDetailLevel.Standard)
    {
        ArgumentNullException.ThrowIfNull(source);

        var builder = CreateInitializedBuilder(
            detailLevel,
            traceId: "trace-ground-boundary",
            calculationType: "GroundBoundaryFoundation",
            rootModule: CalculationTraceModuleKind.Ground);

        var profileStep = builder.AddStep(
            CalculationTraceModuleKind.Ground,
            "Ground profile and H_ground convention");

        builder.AddInputValue(profileStep, TraceValue("groundBoundaryCount", "Ground boundary count", source.GroundBoundaryCount, null));
        builder.AddInputValue(profileStep, TraceValue("profileMode", "Ground temperature profile mode", source.ProfileMode, null));
        builder.AddInputValue(profileStep, TraceValue("hGroundConvention", "H_ground convention", source.HeatTransferConvention, null));
        builder.AddOutputValue(profileStep, TraceValue("equivalentGroundHeatTransferCoefficient", "Equivalent H_ground", source.Result.EquivalentGroundHeatTransferCoefficientWPerKelvin, Unit("W/K")));
        builder.AddOutputValue(profileStep, TraceValue("annualHeatLossKWh", "Annual heat loss", source.Result.AnnualHeatLossKiloWattHours, Unit("kWh")));
        builder.AddOutputValue(profileStep, TraceValue("annualHeatGainKWh", "Annual heat gain", source.Result.AnnualHeatGainKiloWattHours, Unit("kWh")));
        builder.AddIntermediateValue(
            profileStep,
            TraceValue(
                "groundTemperatureProfileC",
                "Ground temperature profile",
                source.Result.GroundTemperatureProfileCelsius.ToArray(),
                Unit("degC"),
                CalculationTraceValueKind.Intermediate));

        foreach (var assumption in source.Result.Assumptions)
        {
            builder.AddAssumption(profileStep, assumption);
        }

        foreach (var warning in source.Result.Warnings)
        {
            builder.AddWarning(profileStep, warning);
        }

        foreach (var diagnostic in source.Result.Diagnostics)
        {
            builder.AddDiagnostic(profileStep, _diagnosticMapper.Map(diagnostic, CalculationTraceModuleKind.Ground));
        }

        return _sanitizer.Sanitize(builder.Build(), detailLevel);
    }

    public CalculationTraceDocument BuildDomesticHotWaterTrace(
        DomesticHotWaterTraceSource source,
        CalculationTraceDetailLevel detailLevel = CalculationTraceDetailLevel.Standard)
    {
        ArgumentNullException.ThrowIfNull(source);

        var builder = CreateInitializedBuilder(
            detailLevel,
            traceId: $"trace-dhw-{source.Result.CalculationId}",
            calculationType: "DomesticHotWaterFoundation",
            rootModule: CalculationTraceModuleKind.DomesticHotWater,
            calculationId: source.Result.CalculationId);

        var usefulStep = builder.AddStep(
            CalculationTraceModuleKind.DomesticHotWater,
            "Useful demand basis and draw-off profile");

        builder.AddInputValue(usefulStep, TraceValue("demandBasis", "Demand basis", source.DemandBasis, null));
        builder.AddInputValue(usefulStep, TraceValue("drawOffProfileMode", "Draw-off profile mode", source.DrawOffProfileMode, null));
        builder.AddOutputValue(usefulStep, TraceValue("annualUsefulDemandKWh", "Annual useful demand", source.Result.AnnualUsefulEnergyKWh, Unit("kWh")));
        builder.AddOutputValue(usefulStep, TraceValue("annualVolumeLiters", "Annual draw-off volume", source.Result.UsefulDemand.AnnualVolumeLiters, Unit("L")));

        var lossesStep = builder.AddStep(
            CalculationTraceModuleKind.DomesticHotWater,
            "Losses, recovered losses, and auxiliary energy summary");

        builder.AddOutputValue(lossesStep, TraceValue("annualStorageLossKWh", "Annual storage losses", source.Result.AnnualStorageLossKWh, Unit("kWh")));
        builder.AddOutputValue(lossesStep, TraceValue("annualDistributionLossKWh", "Annual distribution losses", source.Result.AnnualDistributionLossKWh, Unit("kWh")));
        builder.AddOutputValue(lossesStep, TraceValue("annualCirculationLossKWh", "Annual circulation losses", source.Result.AnnualCirculationLossKWh, Unit("kWh")));
        builder.AddOutputValue(lossesStep, TraceValue("annualRecoverableLossKWh", "Annual recovered losses", source.Result.AnnualRecoverableLossKWh, Unit("kWh")));
        builder.AddOutputValue(lossesStep, TraceValue("annualAuxiliaryEnergyKWh", "Annual auxiliary energy", source.Result.AnnualAuxiliaryElectricityKWh, Unit("kWh")));
        builder.AddOutputValue(lossesStep, TraceValue("annualSystemHeatRequirementKWh", "Annual system heat requirement", source.Result.AnnualSystemHeatRequirementKWh, Unit("kWh")));

        foreach (var diagnostic in source.Result.Diagnostics)
        {
            builder.AddDiagnostic(lossesStep, _diagnosticMapper.Map(diagnostic, CalculationTraceModuleKind.DomesticHotWater));
        }

        return _sanitizer.Sanitize(builder.Build(), detailLevel);
    }

    public CalculationTraceDocument BuildSystemEnergyTrace(
        SystemEnergyTraceSource source,
        CalculationTraceDetailLevel detailLevel = CalculationTraceDetailLevel.Standard)
    {
        ArgumentNullException.ThrowIfNull(source);

        var builder = CreateInitializedBuilder(
            detailLevel,
            traceId: "trace-system-energy",
            calculationType: "SystemEnergyFoundation",
            rootModule: CalculationTraceModuleKind.SystemEnergy);

        var intakeStep = builder.AddStep(
            CalculationTraceModuleKind.SystemEnergy,
            "Intake uses and stage chain");

        builder.AddInputValue(intakeStep, TraceValue("intakeUses", "Intake uses", source.IntakeUses.ToArray(), null));
        builder.AddInputValue(intakeStep, TraceValue("stageChain", "Stage chain", source.StageChain, null));
        builder.AddAssumption(intakeStep, source.OwnershipDecision);
        builder.AddOutputValue(intakeStep, TraceValue("monthlyFinalEnergyCount", "Final energy monthly profile length", source.Result.MonthlyFinalEnergyKWh.Count, null));

        var finalStep = builder.AddStep(
            CalculationTraceModuleKind.SystemEnergy,
            "Final, primary, and CO2 summary by carrier");

        builder.AddOutputValue(finalStep, TraceValue("annualFinalEnergyKWh", "Annual final energy", source.Result.AnnualSummary.FinalEnergyKWh, Unit("kWh")));
        builder.AddOutputValue(finalStep, TraceValue("annualPrimaryEnergyKWh", "Annual primary energy", source.Result.AnnualSummary.PrimaryEnergyKWh, Unit("kWh")));
        builder.AddOutputValue(finalStep, TraceValue("annualCo2Kg", "Annual CO2 emissions", source.Result.AnnualSummary.Co2Kg, Unit("kg")));
        builder.AddOutputValue(
            finalStep,
            TraceValue(
                "finalEnergyByCarrierKWh",
                "Final energy by carrier",
                ToAnnualTotals(source.Result.FinalEnergyByCarrierKWh),
                Unit("kWh")));
        builder.AddOutputValue(
            finalStep,
            TraceValue(
                "primaryEnergyByCarrierKWh",
                "Primary energy by carrier",
                ToAnnualTotals(source.Result.PrimaryEnergyByCarrierKWh),
                Unit("kWh")));
        builder.AddOutputValue(
            finalStep,
            TraceValue(
                "co2ByCarrierKg",
                "CO2 by carrier",
                ToAnnualTotals(source.Result.Co2ByCarrierKg),
                Unit("kg")));

        foreach (var warning in source.Result.Warnings)
        {
            builder.AddWarning(finalStep, warning);
        }

        foreach (var assumption in source.Result.Assumptions)
        {
            builder.AddAssumption(finalStep, assumption);
        }

        foreach (var diagnostic in source.Result.Diagnostics)
        {
            builder.AddDiagnostic(finalStep, _diagnosticMapper.Map(diagnostic, CalculationTraceModuleKind.SystemEnergy));
        }

        return _sanitizer.Sanitize(builder.Build(), detailLevel);
    }

    public CalculationTraceDocument Merge(
        string traceId,
        string calculationType,
        CalculationTraceModuleKind rootModule,
        IReadOnlyList<CalculationTraceDocument> traces,
        string? calculationId = null,
        CalculationTraceDetailLevel detailLevel = CalculationTraceDetailLevel.Standard)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(traceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(calculationType);
        ArgumentNullException.ThrowIfNull(traces);

        var builder = CreateInitializedBuilder(
            detailLevel,
            traceId,
            calculationType,
            rootModule,
            calculationId);

        foreach (var trace in traces.Where(item => item is not null))
        {
            builder.Merge(trace);
        }

        return _sanitizer.Sanitize(builder.Build(), detailLevel);
    }

    private static CalculationTraceValue TraceValue(
        string key,
        string label,
        object? value,
        CalculationTraceUnit? unit,
        CalculationTraceValueKind valueKind = CalculationTraceValueKind.Output,
        string? source = null,
        string? displayFormat = null,
        IReadOnlyList<string>? tags = null) =>
        new(
            Key: key,
            Label: label,
            Value: value,
            Unit: unit,
            ValueKind: valueKind,
            Source: source,
            DisplayFormat: displayFormat,
            Tags: tags);

    private static CalculationTraceUnit Unit(
        string symbol,
        string? quantityKind = null,
        string? displayName = null) =>
        new(symbol, quantityKind, displayName);

    private static bool HasErrorDiagnostics(
        IReadOnlyList<AssistantEngineer.Modules.Calculations.Application.Contracts.Standards.StandardCalculationDiagnostic> diagnostics) =>
        diagnostics.Any(item => item.Severity == CalculationDiagnosticSeverity.Error);

    private static double Average(
        IEnumerable<double> values)
    {
        var lane = values.ToArray();
        return lane.Length == 0 ? 0d : lane.Average();
    }

    private static int GetSurfaceCount(
        AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Iso52016WeatherSolarContext context) =>
        context.Hours.Count == 0 ? 0 : context.Hours[0].SurfaceIrradiance.Count;

    private static double MaxSurfaceIrradiance(
        AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Iso52016WeatherSolarContext context) =>
        context.Hours
            .SelectMany(hour => hour.SurfaceIrradiance)
            .Select(record => record.TotalIrradianceWm2)
            .DefaultIfEmpty(0d)
            .Max();

    private static Dictionary<string, double> ToAnnualTotals<TEnum>(
        IReadOnlyDictionary<TEnum, IReadOnlyList<double>> data)
        where TEnum : struct, Enum =>
        data
            .OrderBy(item => item.Key.ToString(), StringComparer.Ordinal)
            .ToDictionary(
                item => item.Key.ToString(),
                item => item.Value.Sum(),
                StringComparer.Ordinal);

    private static CalculationTraceSeverity InferSeverityFromCode(
        string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return CalculationTraceSeverity.Info;

        if (code.Contains("ERROR", StringComparison.OrdinalIgnoreCase) ||
            code.Contains("INVALID", StringComparison.OrdinalIgnoreCase))
            return CalculationTraceSeverity.Error;

        if (code.Contains("WARN", StringComparison.OrdinalIgnoreCase) ||
            code.Contains("LOCKOUT", StringComparison.OrdinalIgnoreCase) ||
            code.Contains("CLAMP", StringComparison.OrdinalIgnoreCase))
            return CalculationTraceSeverity.Warning;

        return CalculationTraceSeverity.Info;
    }

    private static ICalculationTraceBuilder CreateInitializedBuilder(
        CalculationTraceDetailLevel detailLevel,
        string traceId,
        string calculationType,
        CalculationTraceModuleKind rootModule,
        string? calculationId = null)
    {
        var builder = new CalculationTraceBuilder();
        builder.SetDetailLevel(detailLevel);
        builder.Initialize(
            traceId: traceId,
            calculationType: calculationType,
            rootModule: rootModule,
            calculationId: calculationId,
            createdTimestampUtc: DateTimeOffset.UnixEpoch);
        return builder;
    }
}
