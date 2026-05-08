namespace AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Common;

public sealed record CalculationDisclosure(
    string CoreStatus,
    string CalculationScope,
    string CalculationMethod,
    string ActualMethod,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> Assumptions,
    IReadOnlyList<string> ExplicitNonClaims,
    IReadOnlyList<string> OutOfScopeV1,
    IReadOnlyList<string> DocumentationFiles);

public static class EngineeringCoreReportDisclosures
{
    private const string ClosedV1 = "ClosedV1";
    private const string UnknownMethod = "Unknown";

    public static CalculationDisclosure CoolingDesignPoint(
        string? calculationMethod = null,
        string? actualMethod = null) =>
        new(
            CoreStatus: ClosedV1,
            CalculationScope: "Engineering-core v1 cooling design-point report.",
            CalculationMethod: Normalize(calculationMethod),
            ActualMethod: Normalize(actualMethod),
            Warnings:
            [
                "Cooling report uses engineering design-point load calculation.",
                "Report does not claim full ISO 52016 node/matrix solver equivalence.",
                "Report does not claim exact EnergyPlus, ASHRAE 140 or StandardReference numerical equivalence.",
                "Latent load, moisture balance and detailed psychrometrics are out of scope for engineering-core v1."
            ],
            Assumptions:
            [
                "Cooling load is assembled from transmission, ventilation, infiltration, solar and internal gain components.",
                "Window solar gains use simplified SHGC/shading based engineering model.",
                "Surface irradiance uses ISO52010-inspired solar geometry and isotropic sky transposition.",
                "Equipment selection, when requested, is capacity-margin based and does not model part-load curves."
            ],
            ExplicitNonClaims: ExplicitNonClaims(),
            OutOfScopeV1: OutOfScopeV1(),
            DocumentationFiles: DocumentationFiles());

    public static CalculationDisclosure HeatingDesignPoint(
        string? calculationMethod = null,
        string? actualMethod = null) =>
        new(
            CoreStatus: ClosedV1,
            CalculationScope: "Engineering-core v1 heating design-point report.",
            CalculationMethod: Normalize(calculationMethod),
            ActualMethod: Normalize(actualMethod),
            Warnings:
            [
                "Heating report uses engineering design-point load calculation.",
                "Report does not claim full ISO 52016 node/matrix solver equivalence.",
                "Report does not claim exact EnergyPlus, ASHRAE 140 or StandardReference numerical equivalence.",
                "Latent load, moisture balance and detailed psychrometrics are out of scope for engineering-core v1."
            ],
            Assumptions:
            [
                "Heating load is assembled from transmission and ventilation/infiltration components.",
                "Transmission uses steady-state U*A*?T component heat transfer.",
                "Ventilation and infiltration use sensible-only airflow heat transfer.",
                "Ground and adjacent boundaries are simplified engineering models when present."
            ],
            ExplicitNonClaims: ExplicitNonClaims(),
            OutOfScopeV1: OutOfScopeV1(),
            DocumentationFiles: DocumentationFiles());

    public static CalculationDisclosure AnnualEnergy(
        string? calculationMethod = null,
        string? actualMethod = null) =>
        new(
            CoreStatus: ClosedV1,
            CalculationScope: "Engineering-core v1 hourly annual energy integration report.",
            CalculationMethod: Normalize(calculationMethod),
            ActualMethod: Normalize(actualMethod),
            Warnings:
            [
                "Annual energy is true hourly 8760 only when EnergyDataSource=TrueHourlySimulation, IsTrueHourly8760=true and HourlyRecordCount=8760.",
                "Monthly adapter, synthetic weather and deterministic short fixtures must not be presented as true hourly 8760 annual simulation.",
                "Report does not claim exact EnergyPlus, ASHRAE 140 or StandardReference numerical equivalence."
            ],
            Assumptions:
            [
                "Annual energy is calculated as sum of hourly W*h divided by 1000.",
                "Monthly totals are aggregated from hourly records.",
                "EPW and PVGIS import gates normalize weather to 8760 hourly records."
            ],
            ExplicitNonClaims: ExplicitNonClaims(),
            OutOfScopeV1: OutOfScopeV1(),
            DocumentationFiles: DocumentationFiles());

    private static IReadOnlyList<string> ExplicitNonClaims() =>
    [
        "No exact StandardReference numerical equivalence claim.",
        "No exact EnergyPlus numerical equivalence claim.",
        "No ASHRAE 140 / BESTEST-style validation anchor coverage claim.",
        "No full ISO 52016 node/matrix solver equivalence claim.",
        "No full ISO 52010 climate conversion equivalence claim.",
        "No full ISO 13370 implementation claim.",
        "No full EN 15316 generation/distribution/storage/emission chain claim.",
        "No full coupled multi-zone heat-balance simulation claim.",
        "No latent/moisture/humidity calculation claim."
    ];

    private static IReadOnlyList<string> OutOfScopeV1() =>
    [
        "HVAC.LATENT_LOAD",
        "HVAC.MOISTURE_BALANCE",
        "Humidification/dehumidification conditions",
        "Detailed psychrometric supply-air treatment",
        "Detailed HVAC plant simulation"
    ];

    private static IReadOnlyList<string> DocumentationFiles() =>
    [
        "docs/calculations/EngineeringCoreV1Scope.md",
        "docs/calculations/EngineeringCoreV1ReleaseNotes.md",
        "docs/calculations/EnergyPlusAshrae140ValidationPlan.md"
    ];

    private static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? UnknownMethod
            : value.Trim();
}