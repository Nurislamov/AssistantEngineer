using AssistantEngineer.Modules.Calculations.Application.Abstractions;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;
using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.SharedKernel.ValueObjects;

namespace AssistantEngineer.Modules.Benchmarks.Application.Services;

public sealed class Iso52016ReferenceBenchmarkService
{
    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, Iso52016BenchmarkMetricExpectation>>
        ExpectedMetrics = new Dictionary<string, IReadOnlyDictionary<string, Iso52016BenchmarkMetricExpectation>>
        {
            ["reference-box-heating"] = new Dictionary<string, Iso52016BenchmarkMetricExpectation>
            {
                ["AnnualHeatingDemandKWh"] = new(26411.04, 0.05),
                ["AnnualCoolingDemandKWh"] = new(0, 0.01),
                ["SolarGainsKWh"] = new(0, 0.01),
                ["InternalGainsKWh"] = new(0, 0.01)
            },
            ["reference-box-cooling"] = new Dictionary<string, Iso52016BenchmarkMetricExpectation>
            {
                ["AnnualHeatingDemandKWh"] = new(0, 0.01),
                ["AnnualCoolingDemandKWh"] = new(9137.10, 0.05),
                ["SolarGainsKWh"] = new(0, 0.01),
                ["InternalGainsKWh"] = new(0, 0.01)
            },
            ["reference-solar-shading"] = new Dictionary<string, Iso52016BenchmarkMetricExpectation>
            {
                ["AnnualHeatingDemandKWh"] = new(0, 0.01),
                ["AnnualCoolingDemandKWh"] = new(0, 0.01),
                ["SolarGainsKWh"] = new(1567.41, 0.05),
                ["InternalGainsKWh"] = new(0, 0.01),
                ["UnshadedSolarGainsKWh"] = new(3081.85, 0.05),
                ["ShadedSolarGainsKWh"] = new(1567.41, 0.05),
                ["SolarReductionPercent"] = new(49.14, 0.01)
            }
        };

    private readonly ISolarRadiationService _solarRadiationService;
    private readonly IVentilationHeatTransferCalculator _ventilationCalculator;
    private readonly IWindowShadingService _windowShadingService;
    private readonly Iso52016EnergyNeedOptions _options;

    public Iso52016ReferenceBenchmarkService(
        ISolarRadiationService solarRadiationService,
        IVentilationHeatTransferCalculator ventilationCalculator,
        IWindowShadingService windowShadingService,
        Iso52016EnergyNeedOptions options)
    {
        _solarRadiationService = solarRadiationService;
        _ventilationCalculator = ventilationCalculator;
        _windowShadingService = windowShadingService;
        _options = options;
    }

    public async Task<IReadOnlyList<Iso52016ReferenceBenchmarkResult>> RunAsync(
        CancellationToken cancellationToken = default)
    {
        var heating = await RunHeatingDominantCaseAsync(cancellationToken);
        var cooling = await RunCoolingDominantCaseAsync(cancellationToken);
        var shading = await RunShadingSensitivityCaseAsync(cancellationToken);
        return [heating, cooling, shading];
    }

    private async Task<Iso52016ReferenceBenchmarkResult> RunHeatingDominantCaseAsync(CancellationToken cancellationToken)
    {
        var climateZone = CreateClimateZone("Reference heating climate", summer: 28, winter: -10);
        var building = CreateReferenceBuilding(climateZone, "Reference heating box", includeSouthWindow: false);
        var weather = CreateAnnualWeather(climateZone, temperature: -5, directSolar: 0, diffuseSolar: 0);
        var result = await CreateCalculator(weather, _options)
            .CalculateBuildingEnergyNeedsAsync(building, year: weather.Year, cancellationToken: cancellationToken);

        return CreateResult(
            "reference-box-heating",
            "Simple insulated box in constant cold weather.",
            result!,
            [
                AssertGreaterThan("Annual heating demand is positive", result!.AnnualHeatingDemandKWh, 100),
                AssertLessThan("Annual cooling demand is near zero", result.AnnualCoolingDemandKWh, 1)
            ],
            ExpectedMetrics["reference-box-heating"]);
    }

    private async Task<Iso52016ReferenceBenchmarkResult> RunCoolingDominantCaseAsync(CancellationToken cancellationToken)
    {
        var climateZone = CreateClimateZone("Reference cooling climate", summer: 40, winter: 5);
        var building = CreateReferenceBuilding(climateZone, "Reference cooling box", includeSouthWindow: false);
        var weather = CreateAnnualWeather(climateZone, temperature: 35, directSolar: 0, diffuseSolar: 0);
        var result = await CreateCalculator(weather, _options)
            .CalculateBuildingEnergyNeedsAsync(building, year: weather.Year, cancellationToken: cancellationToken);

        return CreateResult(
            "reference-box-cooling",
            "Simple insulated box in constant hot weather.",
            result!,
            [
                AssertGreaterThan("Annual cooling demand is positive", result!.AnnualCoolingDemandKWh, 100),
                AssertLessThan("Annual heating demand is near zero", result.AnnualHeatingDemandKWh, 1)
            ],
            ExpectedMetrics["reference-box-cooling"]);
    }

    private async Task<Iso52016ReferenceBenchmarkResult> RunShadingSensitivityCaseAsync(CancellationToken cancellationToken)
    {
        var climateZone = CreateClimateZone("Reference solar climate", summer: 35, winter: 5);
        var building = CreateReferenceBuilding(climateZone, "Reference solar box", includeSouthWindow: true);
        var weather = CreateAnnualWeather(climateZone, temperature: 22, directSolar: 550, diffuseSolar: 90);
        var unshaded = await CreateCalculator(weather, _options)
            .CalculateBuildingEnergyNeedsAsync(building, year: weather.Year, cancellationToken: cancellationToken);
        var shadedOptions = new Iso52016EnergyNeedOptions
        {
            DefaultWeatherYear = _options.DefaultWeatherYear,
            DefaultHeatingSetbackC = _options.DefaultHeatingSetbackC,
            DefaultCoolingSetpointC = _options.DefaultCoolingSetpointC,
            DefaultCoolingSetbackC = _options.DefaultCoolingSetbackC,
            DefaultAirChangesPerHour = _options.DefaultAirChangesPerHour,
            AirHeatCapacityWhPerM3K = _options.AirHeatCapacityWhPerM3K,
            InternalHeatCapacityJPerM2K = _options.InternalHeatCapacityJPerM2K,
            DefaultSolarUtilizationFactor = _options.DefaultSolarUtilizationFactor,
            DefaultWindowFrameAreaFraction = _options.DefaultWindowFrameAreaFraction,
            DefaultDirectSolarShadingReductionFactor = _options.DefaultDirectSolarShadingReductionFactor,
            DefaultOverhangDepthM = 1.2,
            DefaultSideFinDepthM = 0.4,
            DefaultWindowRevealDepthM = 0.1,
            DefaultWindowHeightM = _options.DefaultWindowHeightM,
            DefaultWindowWidthM = _options.DefaultWindowWidthM,
            MinimumDirectSolarShadingReductionFactor = _options.MinimumDirectSolarShadingReductionFactor,
            DiffuseSolarShareUnaffectedByShading = _options.DiffuseSolarShareUnaffectedByShading,
            LatitudeDegrees = _options.LatitudeDegrees
        };
        var shaded = await CreateCalculator(weather, shadedOptions)
            .CalculateBuildingEnergyNeedsAsync(building, year: weather.Year, cancellationToken: cancellationToken);
        if (unshaded is null || shaded is null)
            throw new InvalidOperationException("Reference benchmark weather data must be complete.");

        var solarReductionPercent = unshaded.Breakdown.SolarGainsKWh > 0
            ? (unshaded.Breakdown.SolarGainsKWh - shaded.Breakdown.SolarGainsKWh) /
            unshaded.Breakdown.SolarGainsKWh * 100
            : 0;

        return CreateResult(
            "reference-solar-shading",
            "South glazing case checks that geometric shading reduces solar gains.",
            shaded,
            [
                new Iso52016ReferenceBenchmarkAssertion(
                    "Shaded solar gains are below unshaded gains",
                    shaded.Breakdown.SolarGainsKWh,
                    "<",
                    unshaded.Breakdown.SolarGainsKWh,
                    0,
                    shaded.Breakdown.SolarGainsKWh < unshaded.Breakdown.SolarGainsKWh),
                AssertGreaterThan("Solar reduction is material", solarReductionPercent, 5)
            ],
            ExpectedMetrics["reference-solar-shading"],
            new Dictionary<string, double>
            {
                ["UnshadedSolarGainsKWh"] = Round(unshaded.Breakdown.SolarGainsKWh),
                ["ShadedSolarGainsKWh"] = Round(shaded.Breakdown.SolarGainsKWh),
                ["SolarReductionPercent"] = Round(solarReductionPercent)
            });
    }

    private Iso52016HourlySteadyStateCalculator CreateCalculator(
        AnnualClimateData weather,
        Iso52016EnergyNeedOptions options) =>
        new(
            new InMemoryAnnualClimateDataProvider(weather),
            _solarRadiationService,
            _ventilationCalculator,
            _windowShadingService,
            options);

    private static Iso52016ReferenceBenchmarkResult CreateResult(
        string caseId,
        string description,
        Iso52016AnnualEnergyNeedResult energyNeed,
        IReadOnlyList<Iso52016ReferenceBenchmarkAssertion> assertions,
        IReadOnlyDictionary<string, Iso52016BenchmarkMetricExpectation> expectedMetrics,
        IReadOnlyDictionary<string, double>? additionalMetrics = null)
    {
        var metrics = new Dictionary<string, double>
        {
            ["AnnualHeatingDemandKWh"] = Round(energyNeed.AnnualHeatingDemandKWh),
            ["AnnualCoolingDemandKWh"] = Round(energyNeed.AnnualCoolingDemandKWh),
            ["SolarGainsKWh"] = Round(energyNeed.Breakdown.SolarGainsKWh),
            ["InternalGainsKWh"] = Round(energyNeed.Breakdown.InternalGainsKWh)
        };
        if (additionalMetrics is not null)
        {
            foreach (var metric in additionalMetrics)
                metrics[metric.Key] = metric.Value;
        }

        var allAssertions = assertions
            .Concat(CreateMetricAssertions(metrics, expectedMetrics))
            .ToArray();

        return new Iso52016ReferenceBenchmarkResult(
            caseId,
            description,
            allAssertions.All(assertion => assertion.Passed),
            metrics,
            allAssertions);
    }

    private static IReadOnlyList<Iso52016ReferenceBenchmarkAssertion> CreateMetricAssertions(
        IReadOnlyDictionary<string, double> actualMetrics,
        IReadOnlyDictionary<string, Iso52016BenchmarkMetricExpectation> expectedMetrics) =>
        expectedMetrics
            .Select(metric =>
            {
                var actual = actualMetrics.TryGetValue(metric.Key, out var value)
                    ? value
                    : double.NaN;
                var passed = double.IsFinite(actual) &&
                    Math.Abs(actual - metric.Value.ExpectedValue) <= metric.Value.AbsoluteTolerance;
                return new Iso52016ReferenceBenchmarkAssertion(
                    $"{metric.Key} matches locked benchmark fixture",
                    actual,
                    "+/-",
                    metric.Value.ExpectedValue,
                    metric.Value.AbsoluteTolerance,
                    passed);
            })
            .ToArray();

    private static Iso52016ReferenceBenchmarkAssertion AssertGreaterThan(
        string name,
        double actual,
        double threshold) =>
        new(name, Round(actual), ">", threshold, 0, actual > threshold);

    private static Iso52016ReferenceBenchmarkAssertion AssertLessThan(
        string name,
        double actual,
        double threshold) =>
        new(name, Round(actual), "<", threshold, 0, actual < threshold);

    private static Building CreateReferenceBuilding(
        ClimateZone climateZone,
        string name,
        bool includeSouthWindow)
    {
        var project = Project.Create("ISO 52016 benchmark project").Value;
        var building = Building.Create(name, project, climateZone).Value;
        var floor = building.AddFloor("Ground").Value;
        var room = floor.AddRoom(
            "Reference room",
            Area.FromSquareMeters(50).Value,
            3,
            Temperature.FromCelsius(21).Value,
            Temperature.FromCelsius(climateZone.SummerDesignTemperature.Celsius).Value,
            peopleCount: 0,
            equipmentLoad: Power.FromWatts(0).Value,
            lightingLoad: Power.FromWatts(0).Value,
            type: RoomType.Office).Value;

        _ = room.AddWall(Area.FromSquareMeters(30).Value, true, ThermalTransmittance.FromValue(0.35).Value, CardinalDirection.North);
        _ = room.AddWall(Area.FromSquareMeters(30).Value, true, ThermalTransmittance.FromValue(0.35).Value, CardinalDirection.East);
        _ = room.AddWall(Area.FromSquareMeters(30).Value, true, ThermalTransmittance.FromValue(0.35).Value, CardinalDirection.South);
        _ = room.AddWall(Area.FromSquareMeters(30).Value, true, ThermalTransmittance.FromValue(0.35).Value, CardinalDirection.West);

        if (includeSouthWindow)
        {
            _ = room.AddWindow(
                Area.FromSquareMeters(8).Value,
                ThermalTransmittance.FromValue(1.2).Value,
                SolarHeatGainCoefficient.FromValue(0.55).Value,
                CardinalDirection.South);
        }

        return building;
    }

    private static ClimateZone CreateClimateZone(string name, double summer, double winter) =>
        ClimateZone.Create(
            name,
            Temperature.FromCelsius(summer).Value,
            Temperature.FromCelsius(winter).Value).Value;

    private static AnnualClimateData CreateAnnualWeather(
        ClimateZone climateZone,
        double temperature,
        double directSolar,
        double diffuseSolar)
    {
        var weather = AnnualClimateData.Create(climateZone, year: 2020).Value;
        for (var hour = 0; hour < 8760; hour++)
        {
            var hourOfDay = hour % 24;
            var daylightFactor = hourOfDay is >= 7 and <= 17
                ? Math.Sin(Math.PI * (hourOfDay - 6) / 12.0)
                : 0;
            _ = weather.AddHourlyData(
                hour,
                temperature,
                directSolar * daylightFactor,
                diffuseSolar * daylightFactor);
        }

        return weather;
    }

    private static double Round(double value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private sealed class InMemoryAnnualClimateDataProvider : IAnnualClimateDataProvider
    {
        private readonly AnnualClimateData _weather;

        public InMemoryAnnualClimateDataProvider(AnnualClimateData weather)
        {
            _weather = weather;
        }

        public Task<AnnualClimateData?> GetForClimateZoneAsync(
            int climateZoneId,
            int year,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<AnnualClimateData?>(
                climateZoneId == _weather.ClimateZoneId && year == _weather.Year
                    ? _weather
                    : null);
    }
}

public sealed record Iso52016ReferenceBenchmarkResult(
    string CaseId,
    string Description,
    bool Passed,
    IReadOnlyDictionary<string, double> Metrics,
    IReadOnlyList<Iso52016ReferenceBenchmarkAssertion> Assertions);

public sealed record Iso52016ReferenceBenchmarkAssertion(
    string Name,
    double Actual,
    string Operator,
    double Expected,
    double Tolerance,
    bool Passed);

public sealed record Iso52016BenchmarkMetricExpectation(
    double ExpectedValue,
    double AbsoluteTolerance);
