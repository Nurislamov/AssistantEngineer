using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Services.Ventilation;

namespace AssistantEngineer.Tests.Calculations.Ventilation;

public sealed class NaturalVentilationControlledAirflowInputBuilderTests
{
    private readonly NaturalVentilationControlledAirflowInputBuilder _builder = new();

    [Fact]
    public void AppliesEvaluatedOpeningFractionsToHourlyAirflowInput()
    {
        var input = CreateBaseInput();
        var operations =
            new[]
            {
                CreateOperation("O1", 0.25)
            };

        var result = _builder.BuildHourlyAirflowInput(input, operations);

        var opening = Assert.Single(result.Openings);
        Assert.Equal(0.25, opening.OpeningFraction!.Value, 6);
    }

    [Fact]
    public void UnknownOperationOpeningProducesDiagnostic()
    {
        var input = CreateBaseInput();
        var operations =
            new[]
            {
                CreateOperation("UNKNOWN", 0.25)
            };

        var result = _builder.BuildHourlyAirflowInput(input, operations);

        Assert.Contains(result.Environment.Diagnostics, diagnostic => diagnostic.Code == "AE-VENT-CONTROL-OPENING-NOT-FOUND");
    }

    private static NaturalVentilationCalculationInput CreateBaseInput() =>
        new(
            CalculationId: "CALC-1",
            FlowConfiguration: NaturalVentilationFlowConfiguration.WindOnly,
            Openings:
            [
                new NaturalVentilationOpeningGeometry(
                    OpeningId: "O1",
                    RoomId: "R1",
                    ZoneId: "Z1",
                    SurfaceId: "S1",
                    OpeningType: NaturalVentilationOpeningType.Window,
                    OpeningAreaSquareMeters: 1.0,
                    OpeningHeightMeters: 1.0,
                    OpeningWidthMeters: 1.0,
                    OpeningCenterHeightMeters: 1.5,
                    BottomHeightMeters: 1.0,
                    TopHeightMeters: 2.0,
                    OpeningFraction: 1.0,
                    DischargeCoefficient: 0.6,
                    WindPressureCoefficient: 0.4,
                    OppositeWindPressureCoefficient: 0.0,
                    OrientationAzimuthDegrees: 180.0,
                    Source: "UnitTest",
                    Diagnostics: [])
            ],
            Environment: new NaturalVentilationEnvironment(
                IndoorTemperatureCelsius: 24.0,
                OutdoorTemperatureCelsius: 18.0,
                WindSpeedMetersPerSecond: 2.0,
                WindSpeedHeightMeters: 10.0,
                OpeningReferenceHeightMeters: 0.0,
                OutdoorAirDensityKgPerCubicMeter: 1.2,
                IndoorAirDensityKgPerCubicMeter: 1.2,
                AtmosphericPressurePa: 101325.0,
                Source: "UnitTest",
                Diagnostics: []),
            DisclosureOverride: null,
            Source: "UnitTest");

    private static NaturalVentilationOpeningOperationResult CreateOperation(
        string openingId,
        double fraction) =>
        new(
            RuleId: "R1",
            OpeningId: openingId,
            RoomId: "R1",
            ZoneId: "Z1",
            HourIndex: 0,
            ControlMode: NaturalVentilationControlMode.FixedFraction,
            OpeningFraction: fraction,
            IsOpen: fraction > 0.0,
            IsNightVentilationActive: false,
            ActiveReasons: ["UnitTest"],
            Diagnostics: []);
}
