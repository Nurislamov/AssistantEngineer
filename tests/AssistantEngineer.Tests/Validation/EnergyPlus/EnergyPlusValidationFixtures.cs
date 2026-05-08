namespace AssistantEngineer.Tests.Validation.EnergyPlus;

public static class EnergyPlusValidationFixtures
{
    public static IReadOnlyList<EnergyPlusValidationCase> Cases { get; } =
    [
        new(
            CaseId: "EP-SMOKE-001",
            Name: "Single zone transmission-only heating smoke case",
            Stage: EnergyPlusValidationStage.Smoke,
            Source: "Offline committed reference fixture; EnergyPlus model to be added in future validation milestone.",
            WeatherSource: "Constant winter outdoor temperature synthetic fixture.",
            Geometry: "Single-zone rectangular box with opaque envelope and no windows.",
            Envelope: "Simple U-value envelope, no thermal bridge detail, no ground coupling beyond simplified boundary.",
            InternalGains: "No internal gains.",
            Ventilation: "No ventilation or infiltration.",
            HvacControl: "Ideal heating load, fixed heating setpoint, no cooling.",
            Metrics:
            [
                new(
                    MetricId: "annual-heating-kwh",
                    Name: "Annual heating energy",
                    Unit: "kWh",
                    AssistantEngineerValue: 12_000,
                    ReferenceValue: 12_750,
                    TolerancePercent: 20,
                    Type: EnergyPlusValidationMetricType.NumericWithinTolerance,
                    Notes: "Smoke tolerance checks that the simplified transmission-only result remains in engineering range."),

                new(
                    MetricId: "peak-heating-w",
                    Name: "Peak heating load",
                    Unit: "W",
                    AssistantEngineerValue: 4_000,
                    ReferenceValue: 4_250,
                    TolerancePercent: 25,
                    Type: EnergyPlusValidationMetricType.NumericWithinTolerance,
                    Notes: "Peak load tolerance is wider because timestep and heat-balance assumptions differ."),

                new(
                    MetricId: "annual-cooling-kwh",
                    Name: "Annual cooling energy",
                    Unit: "kWh",
                    AssistantEngineerValue: 0,
                    ReferenceValue: 0,
                    TolerancePercent: 0,
                    Type: EnergyPlusValidationMetricType.SameSign,
                    Notes: "Transmission-only winter heating case should not produce cooling.")
            ],
            Assumptions:
            [
                "Single thermal zone.",
                "No windows.",
                "No solar gains.",
                "No internal gains.",
                "No ventilation or infiltration.",
                "Ideal load behavior only; no plant simulation."
            ],
            KnownDifferences:
            [
                "AssistantEngineer uses engineering component formulas and simplified hourly balance.",
                "EnergyPlus uses its own zone heat-balance algorithms and timestep handling.",
                "Exact watt-by-watt equivalence is not expected."
            ],
            NonClaims:
            [
                "Does not claim exact EnergyPlus numerical equivalence.",
                "Does not claim ASHRAE 140 / BESTEST-style validation anchor coverage.",
                "Does not claim full ISO 52016 node/matrix solver equivalence."
            ]),

        new(
            CaseId: "EP-SMOKE-002",
            Name: "Single zone solar cooling smoke case",
            Stage: EnergyPlusValidationStage.Smoke,
            Source: "Offline committed reference fixture; EnergyPlus model to be added in future validation milestone.",
            WeatherSource: "Synthetic summer weather with daytime solar profile.",
            Geometry: "Single-zone rectangular box with one south-facing window.",
            Envelope: "Simple U-value envelope with simplified SHGC window.",
            InternalGains: "No internal gains.",
            Ventilation: "No ventilation or infiltration.",
            HvacControl: "Ideal cooling load, fixed cooling setpoint, no heating.",
            Metrics:
            [
                new(
                    MetricId: "annual-cooling-kwh",
                    Name: "Annual cooling energy",
                    Unit: "kWh",
                    AssistantEngineerValue: 3_500,
                    ReferenceValue: 3_950,
                    TolerancePercent: 20,
                    Type: EnergyPlusValidationMetricType.NumericWithinTolerance,
                    Notes: "Checks solar-driven cooling magnitude with simplified SHGC/isotropic sky assumptions."),

                new(
                    MetricId: "peak-cooling-w",
                    Name: "Peak cooling load",
                    Unit: "W",
                    AssistantEngineerValue: 1_800,
                    ReferenceValue: 2_050,
                    TolerancePercent: 25,
                    Type: EnergyPlusValidationMetricType.NumericWithinTolerance,
                    Notes: "Peak tolerance accounts for solar distribution and timestep differences."),

                new(
                    MetricId: "solar-orientation-response",
                    Name: "Solar orientation response",
                    Unit: "direction",
                    AssistantEngineerValue: 1,
                    ReferenceValue: 1,
                    TolerancePercent: 0,
                    Type: EnergyPlusValidationMetricType.DirectionalTrend,
                    Notes: "South-facing daytime solar case must increase cooling demand directionally.")
            ],
            Assumptions:
            [
                "Single thermal zone.",
                "One simplified south-facing window.",
                "SHGC/shading engineering solar model.",
                "ISO52010-inspired isotropic sky transposition.",
                "Ideal cooling load behavior only; no plant simulation."
            ],
            KnownDifferences:
            [
                "AssistantEngineer does not model detailed EnergyPlus solar distribution.",
                "AssistantEngineer does not model detailed glazing optics.",
                "Exact hourly solar equivalence is not expected."
            ],
            NonClaims:
            [
                "Does not claim exact EnergyPlus numerical equivalence.",
                "Does not claim ASHRAE 140 / BESTEST-style validation anchor coverage.",
                "Does not claim full optical glazing or EnergyPlus solar distribution equivalence."
            ]),

        new(
            CaseId: "EP-SMOKE-003",
            Name: "Single zone internal gains cooling smoke case",
            Stage: EnergyPlusValidationStage.Smoke,
            Source: "Offline committed reference fixture; EnergyPlus model to be added in future validation milestone.",
            WeatherSource: "Constant mild weather synthetic fixture.",
            Geometry: "Single-zone rectangular box with no windows.",
            Envelope: "Simple envelope with low transmission load.",
            InternalGains: "Fixed sensible internal gains schedule.",
            Ventilation: "No ventilation or infiltration.",
            HvacControl: "Ideal cooling load, fixed cooling setpoint.",
            Metrics:
            [
                new(
                    MetricId: "annual-cooling-kwh",
                    Name: "Annual cooling energy",
                    Unit: "kWh",
                    AssistantEngineerValue: 2_100,
                    ReferenceValue: 2_350,
                    TolerancePercent: 20,
                    Type: EnergyPlusValidationMetricType.NumericWithinTolerance,
                    Notes: "Checks sensible internal-gain-driven cooling magnitude."),

                new(
                    MetricId: "annual-heating-kwh",
                    Name: "Annual heating energy",
                    Unit: "kWh",
                    AssistantEngineerValue: 0,
                    ReferenceValue: 0,
                    TolerancePercent: 0,
                    Type: EnergyPlusValidationMetricType.SameSign,
                    Notes: "Mild internal-gain case should not produce heating."),

                new(
                    MetricId: "internal-gain-response",
                    Name: "Internal gain response",
                    Unit: "direction",
                    AssistantEngineerValue: 1,
                    ReferenceValue: 1,
                    TolerancePercent: 0,
                    Type: EnergyPlusValidationMetricType.DirectionalTrend,
                    Notes: "Adding internal gains must increase cooling demand directionally.")
            ],
            Assumptions:
            [
                "Single thermal zone.",
                "Sensible internal gains only.",
                "No latent or moisture balance.",
                "No detailed HVAC plant simulation.",
                "Ideal cooling load behavior only."
            ],
            KnownDifferences:
            [
                "AssistantEngineer internal gains are sensible-only in v1.",
                "Latent heat and humidity are intentionally out of scope.",
                "Detailed EnergyPlus heat storage/distribution behavior is not claimed."
            ],
            NonClaims:
            [
                "Does not claim exact EnergyPlus numerical equivalence.",
                "Does not claim ASHRAE 140 / BESTEST-style validation anchor coverage.",
                "Does not claim latent or moisture validation coverage."
            ])
    ];
}
