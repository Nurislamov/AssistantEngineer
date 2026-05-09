using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;
using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.MultiZone;
using AssistantEngineer.Modules.Calculations.Application.Services.Standards;
using AssistantEngineer.Modules.Calculations.Application.Services.Topology;

namespace AssistantEngineer.Tests.Calculations.Iso52016.MultiZone;

public sealed class Iso52016MultiZoneNormalizedInputBuilderTests
{
    [Fact]
    public void PipelineConsumesNormalizedAdjacentBoundaryLane()
    {
        var builder = CreateBuilder();
        var build = builder.Build(CreateSingleZoneExteriorAndAdjacentUnconditionedRequest());

        Assert.True(build.IsValid);
        Assert.Contains(
            build.Input.BoundaryLinks,
            link => link.BoundaryType == MultiZoneBoundaryLinkType.AdjacentUnconditionedZone);

        var service = CreateService();
        var result = service.Simulate(build.Input);

        Assert.True(result.IsValid);
        Assert.Single(result.HourlyResults);
    }

    [Fact]
    public void SameUseAdjacentBoundaryDoesNotBecomeExteriorHeatLoss()
    {
        var builder = CreateBuilder();
        var build = builder.Build(CreateSameUseRequest());

        Assert.True(build.IsValid);
        Assert.Contains(
            build.Input.BoundaryLinks,
            link => link.BoundaryType == MultiZoneBoundaryLinkType.AdjacentConditionedSameUseZone &&
                    link.AdjacentBoundaryCondition?.IsAdiabaticEquivalent == true);

        var service = CreateService();
        var result = service.Simulate(build.Input);

        Assert.True(result.IsValid);
        Assert.DoesNotContain(
            result.Diagnostics,
            item => item.Code == "Iso52016.MultiZone.HourlySolver.ExternalBoundaryTemperatureMissing");
        Assert.Contains(
            result.Diagnostics,
            item => item.Code == "Iso52016.MultiZone.HourlySolver.SameUseAdiabaticPolicyApplied");
    }

    [Fact]
    public void InterZoneBoundaryCreatesConductanceLinkOnce_NotDoubleCounted()
    {
        var builder = CreateBuilder();
        var build = builder.Build(CreateTwoZoneOpposingInterZoneBoundariesRequest());

        Assert.True(build.IsValid);
        Assert.Single(build.Input.BoundaryLinks, link => link.BoundaryType == MultiZoneBoundaryLinkType.InterZoneBoundary);
        Assert.Contains(
            build.Diagnostics,
            item => item.Code == "Iso52016.MultiZone.NormalizedInput.InterZonePairDeduplicated");
    }

    [Fact]
    public void NaturalVentilationResultIsMappedIntoZoneVentilationLane()
    {
        var builder = CreateBuilder();
        var request = CreateSingleZoneExteriorAndAdjacentUnconditionedRequest() with
        {
            NaturalVentilationZoneIntegration = CreateNaturalVentilationResult(
                ("ZONE-A", new[] { 45.0 })),
            VentilationLaneMergeMode = NaturalVentilationVentilationLaneMergeMode.NaturalOnly
        };

        var build = builder.Build(request);
        var zoneProfile = Assert.Single(build.Input.ZoneHourlyProfiles!, profile => profile.ZoneId == "ZONE-A");

        Assert.Equal(45.0, zoneProfile.VentilationInfiltrationConductanceProfileWPerK[0], 6);
    }

    [Fact]
    public void NoDoubleCountingModeUsesMaxAgainstBaseVentilationLane()
    {
        var builder = CreateBuilder();
        var request = CreateSingleZoneExteriorAndAdjacentUnconditionedRequest() with
        {
            ZoneHourlyProfiles =
            [
                new MultiZoneZoneHourlyProfile("ZONE-A", 20.0, 1_000_000.0, [21.0], [26.0], [0.0], [0.0], [30.0]),
                new MultiZoneZoneHourlyProfile("ZONE-U", 15.0, 500_000.0, [15.0], [35.0], [0.0], [0.0], [0.0])
            ],
            NaturalVentilationZoneIntegration = CreateNaturalVentilationResult(
                ("ZONE-A", new[] { 20.0 })),
            VentilationLaneMergeMode = NaturalVentilationVentilationLaneMergeMode.NoDoubleCountingMax
        };

        var build = builder.Build(request);
        var zoneProfile = Assert.Single(build.Input.ZoneHourlyProfiles!, profile => profile.ZoneId == "ZONE-A");

        Assert.Equal(30.0, zoneProfile.VentilationInfiltrationConductanceProfileWPerK[0], 6);
    }

    [Fact]
    public void AdditiveModeCanCombineBaseAndVentilationComponents()
    {
        var builder = CreateBuilder();
        var request = CreateSingleZoneExteriorAndAdjacentUnconditionedRequest() with
        {
            ZoneHourlyProfiles =
            [
                new MultiZoneZoneHourlyProfile("ZONE-A", 20.0, 1_000_000.0, [21.0], [26.0], [0.0], [0.0], [10.0]),
                new MultiZoneZoneHourlyProfile("ZONE-U", 15.0, 500_000.0, [15.0], [35.0], [0.0], [0.0], [0.0])
            ],
            NaturalVentilationZoneIntegration = CreateNaturalVentilationResult(
                ("ZONE-A", new[] { 15.0 })),
            InfiltrationVentilationConductanceProfilesByZoneId = new Dictionary<string, IReadOnlyList<double>>(StringComparer.Ordinal)
            {
                ["ZONE-A"] = new[] { 5.0 }
            },
            VentilationLaneMergeMode = NaturalVentilationVentilationLaneMergeMode.Additive
        };

        var build = builder.Build(request);
        var zoneProfile = Assert.Single(build.Input.ZoneHourlyProfiles!, profile => profile.ZoneId == "ZONE-A");

        Assert.Equal(30.0, zoneProfile.VentilationInfiltrationConductanceProfileWPerK[0], 6);
        Assert.Contains(build.Diagnostics, diagnostic => diagnostic.Code == "Iso52016.MultiZone.NormalizedInput.VentilationLaneMerged");
    }

    private static Iso52016MultiZoneNormalizedInputBuilder CreateBuilder() =>
        new(
            new ThermalBoundaryClassificationService(),
            new AdjacentUnconditionedZoneTemperatureCalculator());

    private static Iso52016MultiZoneEnergySimulationService CreateService()
    {
        var validator = new Iso52016MultiZoneInputValidator();
        var graphBuilder = new Iso52016MultiZoneGraphBuilder(validator);
        var solver = new Iso52016MultiZoneHourlySolver();
        return new Iso52016MultiZoneEnergySimulationService(validator, graphBuilder, solver);
    }

    private static MultiZoneNormalizedInputBuildRequest CreateSingleZoneExteriorAndAdjacentUnconditionedRequest() =>
        new(
            BuildingId: "BLD-NORM-1",
            Zones:
            [
                new ThermalZoneDefinition(
                    ZoneId: "ZONE-A",
                    Name: "Zone A",
                    Kind: ThermalZoneKind.Conditioned,
                    FloorAreaSquareMeters: 30.0,
                    VolumeCubicMeters: 75.0,
                    HeatingSetpointProfileId: "SP-H-A",
                    CoolingSetpointProfileId: "SP-C-A",
                    Boundaries:
                    [
                        new ThermalBoundaryDefinition(
                            BoundaryId: "A-OUT",
                            SourceZoneId: "ZONE-A",
                            AdjacentZoneId: null,
                            ExposureKind: BoundaryExposureKind.ExteriorAir,
                            ElementKind: BoundaryElementKind.Wall,
                            AreaSquareMeters: 10.0,
                            UValueWPerSquareMeterKelvin: 0.4),
                        new ThermalBoundaryDefinition(
                            BoundaryId: "A-UNCOND",
                            SourceZoneId: "ZONE-A",
                            AdjacentZoneId: "ZONE-U",
                            ExposureKind: BoundaryExposureKind.AdjacentUnconditionedZone,
                            ElementKind: BoundaryElementKind.InternalPartition,
                            AreaSquareMeters: 6.0,
                            UValueWPerSquareMeterKelvin: 0.5)
                    ]),
                new ThermalZoneDefinition(
                    ZoneId: "ZONE-U",
                    Name: "Unconditioned zone",
                    Kind: ThermalZoneKind.Unconditioned,
                    FloorAreaSquareMeters: 10.0,
                    VolumeCubicMeters: 20.0,
                    HeatingSetpointProfileId: null,
                    CoolingSetpointProfileId: null,
                    Boundaries: [])
            ],
            ZoneHourlyProfiles:
            [
                new MultiZoneZoneHourlyProfile("ZONE-A", 20.0, 1_000_000.0, [21.0], [26.0], [0.0], [0.0], [0.0]),
                new MultiZoneZoneHourlyProfile("ZONE-U", 15.0, 500_000.0, [15.0], [35.0], [0.0], [0.0], [0.0])
            ],
            ExteriorTemperatureProfileCelsius: [0.0],
            AdjacentUnconditionedReductionFactorByBoundaryId: new Dictionary<string, double>(StringComparer.Ordinal)
            {
                ["A-UNCOND"] = 0.4
            });

    private static MultiZoneNormalizedInputBuildRequest CreateSameUseRequest() =>
        new(
            BuildingId: "BLD-NORM-2",
            Zones:
            [
                new ThermalZoneDefinition(
                    ZoneId: "ZONE-A",
                    Name: "Zone A",
                    Kind: ThermalZoneKind.Conditioned,
                    FloorAreaSquareMeters: 20.0,
                    VolumeCubicMeters: 50.0,
                    HeatingSetpointProfileId: "SP-H-A",
                    CoolingSetpointProfileId: "SP-C-A",
                    Boundaries:
                    [
                        new ThermalBoundaryDefinition(
                            BoundaryId: "A-SAME",
                            SourceZoneId: "ZONE-A",
                            AdjacentZoneId: "ZONE-B",
                            ExposureKind: BoundaryExposureKind.SameUseAdjacentZone,
                            ElementKind: BoundaryElementKind.InternalPartition,
                            AreaSquareMeters: 8.0,
                            UValueWPerSquareMeterKelvin: 0.5)
                    ]),
                new ThermalZoneDefinition(
                    ZoneId: "ZONE-B",
                    Name: "Zone B",
                    Kind: ThermalZoneKind.Conditioned,
                    FloorAreaSquareMeters: 20.0,
                    VolumeCubicMeters: 50.0,
                    HeatingSetpointProfileId: "SP-H-B",
                    CoolingSetpointProfileId: "SP-C-B",
                    Boundaries: [])
            ],
            ZoneHourlyProfiles:
            [
                new MultiZoneZoneHourlyProfile("ZONE-A", 20.0, 900_000.0, [21.0], [26.0], [100.0], [0.0], [0.0]),
                new MultiZoneZoneHourlyProfile("ZONE-B", 20.0, 900_000.0, [21.0], [26.0], [100.0], [0.0], [0.0])
            ],
            ExteriorTemperatureProfileCelsius: [0.0]);

    private static MultiZoneNormalizedInputBuildRequest CreateTwoZoneOpposingInterZoneBoundariesRequest() =>
        new(
            BuildingId: "BLD-NORM-3",
            Zones:
            [
                new ThermalZoneDefinition(
                    ZoneId: "ZONE-A",
                    Name: "Zone A",
                    Kind: ThermalZoneKind.Conditioned,
                    FloorAreaSquareMeters: 25.0,
                    VolumeCubicMeters: 60.0,
                    HeatingSetpointProfileId: "SP-H-A",
                    CoolingSetpointProfileId: "SP-C-A",
                    Boundaries:
                    [
                        new ThermalBoundaryDefinition(
                            BoundaryId: "A-TO-B",
                            SourceZoneId: "ZONE-A",
                            AdjacentZoneId: "ZONE-B",
                            ExposureKind: BoundaryExposureKind.AdjacentConditionedZone,
                            ElementKind: BoundaryElementKind.InternalPartition,
                            AreaSquareMeters: 10.0,
                            UValueWPerSquareMeterKelvin: 0.5)
                    ]),
                new ThermalZoneDefinition(
                    ZoneId: "ZONE-B",
                    Name: "Zone B",
                    Kind: ThermalZoneKind.Conditioned,
                    FloorAreaSquareMeters: 25.0,
                    VolumeCubicMeters: 60.0,
                    HeatingSetpointProfileId: "SP-H-B",
                    CoolingSetpointProfileId: "SP-C-B",
                    Boundaries:
                    [
                        new ThermalBoundaryDefinition(
                            BoundaryId: "B-TO-A",
                            SourceZoneId: "ZONE-B",
                            AdjacentZoneId: "ZONE-A",
                            ExposureKind: BoundaryExposureKind.AdjacentConditionedZone,
                            ElementKind: BoundaryElementKind.InternalPartition,
                            AreaSquareMeters: 10.0,
                            UValueWPerSquareMeterKelvin: 0.5)
                    ])
            ],
            ZoneHourlyProfiles:
            [
                new MultiZoneZoneHourlyProfile("ZONE-A", 21.0, 900_000.0, [21.0], [26.0], [0.0], [0.0], [0.0]),
                new MultiZoneZoneHourlyProfile("ZONE-B", 19.0, 900_000.0, [21.0], [26.0], [0.0], [0.0], [0.0])
            ],
            ExteriorTemperatureProfileCelsius: [0.0]);

    private static NaturalVentilationZoneIntegrationResult CreateNaturalVentilationResult(
        params (string ZoneId, IReadOnlyList<double> HveProfile)[] profiles)
    {
        var profileDictionary = profiles.ToDictionary(
            item => item.ZoneId,
            item => item.HveProfile,
            StringComparer.Ordinal);
        var emptyProfileDictionary = profiles.ToDictionary(
            item => item.ZoneId,
            _ => (IReadOnlyList<double>)new[] { 0.0 },
            StringComparer.Ordinal);

        return new NaturalVentilationZoneIntegrationResult(
            CalculationId: "VENT-HANDOFF",
            HourlyZones: [],
            UnassignedRooms: [],
            UnassignedOpenings: [],
            ZoneAirflowCubicMetersPerHourProfiles: emptyProfileDictionary,
            ZoneVentilationHeatTransferCoefficientProfilesWPerKelvin: profileDictionary,
            ZoneSensibleVentilationLoadProfilesWatts: emptyProfileDictionary,
            ZoneAirChangesPerHourProfiles: emptyProfileDictionary,
            Disclosure: new StandardCalculationDisclosureFactory().CreateNaturalVentilationEn16798Disclosure(),
            Diagnostics: []);
    }
}
