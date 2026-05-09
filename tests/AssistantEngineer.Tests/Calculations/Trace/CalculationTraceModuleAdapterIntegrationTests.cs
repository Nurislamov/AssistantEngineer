using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Solar;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Topology;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation.Iso16798;
using AssistantEngineer.Modules.Calculations.Application.Services.Trace;

namespace AssistantEngineer.Tests.Calculations.Trace;

public sealed class CalculationTraceModuleAdapterIntegrationTests
{
    private readonly CalculationTraceModuleAdapter _adapter = new(
        new CalculationTraceDiagnosticMapper(),
        new CalculationTraceSanitizer());

    [Fact]
    public void SolarWeatherTraceHasRequiredSteps()
    {
        var fixture = CalculationTraceFixtureLoader.Load("solar-trace-fixture.json");
        var trace = _adapter.BuildWeatherSolarTrace(CreateWeatherSolarSource(), CalculationTraceDetailLevel.Detailed);
        AssertTraceMatchesFixture(trace, fixture);
    }

    [Fact]
    public void ThermalTopologyTraceHasClassificationSummary()
    {
        var fixture = CalculationTraceFixtureLoader.Load("thermal-topology-trace-fixture.json");
        var trace = _adapter.BuildThermalTopologyTrace(CreateTopologySource(), CalculationTraceDetailLevel.Standard);
        AssertTraceMatchesFixture(trace, fixture);
    }

    [Fact]
    public void Iso52016MultiZoneTraceHasZoneLoadSummary()
    {
        var fixture = CalculationTraceFixtureLoader.Load("iso52016-multizone-trace-fixture.json");
        var trace = _adapter.BuildIso52016MultiZoneTrace(CreateMultiZoneSource(), CalculationTraceDetailLevel.Detailed);
        AssertTraceMatchesFixture(trace, fixture);
    }

    [Fact]
    public void NaturalVentilationTraceHasControlAndAirflowSummary()
    {
        var fixture = CalculationTraceFixtureLoader.Load("natural-ventilation-trace-fixture.json");
        var trace = _adapter.BuildNaturalVentilationTrace(CreateVentilationSource(), CalculationTraceDetailLevel.Standard);
        AssertTraceMatchesFixture(trace, fixture);
    }

    [Fact]
    public void GroundTraceHasProfileAndHGroundSummary()
    {
        var fixture = CalculationTraceFixtureLoader.Load("ground-trace-fixture.json");
        var trace = _adapter.BuildGroundTrace(CreateGroundSource(), CalculationTraceDetailLevel.Detailed);
        AssertTraceMatchesFixture(trace, fixture);
    }

    [Fact]
    public void DhwTraceHasUsefulLossSystemSummary()
    {
        var fixture = CalculationTraceFixtureLoader.Load("dhw-trace-fixture.json");
        var trace = _adapter.BuildDomesticHotWaterTrace(CreateDhwSource(), CalculationTraceDetailLevel.Standard);
        AssertTraceMatchesFixture(trace, fixture);
    }

    [Fact]
    public void SystemEnergyTraceHasFinalPrimaryAndCo2Summary()
    {
        var fixture = CalculationTraceFixtureLoader.Load("system-energy-trace-fixture.json");
        var trace = _adapter.BuildSystemEnergyTrace(CreateSystemEnergySource(), CalculationTraceDetailLevel.Standard);
        AssertTraceMatchesFixture(trace, fixture);
    }

    [Fact]
    public void CombinedTraceCanMergeSubmoduleTracesDeterministically()
    {
        var weatherTrace = _adapter.BuildWeatherSolarTrace(CreateWeatherSolarSource(), CalculationTraceDetailLevel.Standard);
        var systemTrace = _adapter.BuildSystemEnergyTrace(CreateSystemEnergySource(), CalculationTraceDetailLevel.Standard);
        var mergedA = _adapter.Merge(
            "trace-combined",
            "CombinedTrace",
            CalculationTraceModuleKind.Generic,
            [weatherTrace, systemTrace],
            calculationId: "combined-1");
        var mergedB = _adapter.Merge(
            "trace-combined",
            "CombinedTrace",
            CalculationTraceModuleKind.Generic,
            [weatherTrace, systemTrace],
            calculationId: "combined-1");

        var exporter = new CalculationTraceJsonExporter();
        Assert.Equal(exporter.Export(mergedA), exporter.Export(mergedB));
        Assert.Contains(mergedA.Summary.Modules, module => module == CalculationTraceModuleKind.Weather);
        Assert.Contains(mergedA.Summary.Modules, module => module == CalculationTraceModuleKind.SystemEnergy);
    }

    private static void AssertTraceMatchesFixture(
        CalculationTraceDocument trace,
        CalculationTraceFixture fixture)
    {
        var moduleNames = trace.Summary.Modules.Select(module => module.ToString()).ToArray();
        foreach (var expectedModule in fixture.ExpectedModules)
        {
            Assert.Contains(expectedModule, moduleNames);
        }

        var steps = FlattenSteps(trace.Steps).ToArray();
        foreach (var expectedStep in fixture.ExpectedStepNames)
        {
            Assert.Contains(steps, step => string.Equals(step.StepName, expectedStep, StringComparison.Ordinal));
        }

        var keys = steps
            .SelectMany(step => step.InputValues.Concat(step.IntermediateValues).Concat(step.OutputValues))
            .Select(value => value.Key)
            .ToArray();

        foreach (var expectedKey in fixture.ExpectedValueKeys)
        {
            Assert.Contains(expectedKey, keys);
        }
    }

    private static WeatherSolarTraceSource CreateWeatherSolarSource()
    {
        var hour0 = new Iso52016HourlyWeatherSolarRecord(
            HourOfYear: 0,
            Month: 1,
            Day: 1,
            Hour: 0,
            OutdoorTemperatureC: 2,
            GroundBoundaryTemperatureC: 8,
            SolarAltitudeDegrees: -6,
            SolarAzimuthDegrees: 180,
            DirectNormalIrradianceWm2: 0,
            DiffuseHorizontalIrradianceWm2: 10,
            GlobalHorizontalIrradianceWm2: 10,
            SurfaceIrradiance:
            [
                new Iso52016SurfaceWeatherSolarRecord("S", SurfaceOrientation.SouthVertical, 80, 0, 8, 2, 10)
            ]);

        var hour1 = hour0 with
        {
            HourOfYear = 1,
            SolarAltitudeDegrees = 12,
            DirectNormalIrradianceWm2 = 220,
            DiffuseHorizontalIrradianceWm2 = 60,
            GlobalHorizontalIrradianceWm2 = 180,
            SurfaceIrradiance =
            [
                new Iso52016SurfaceWeatherSolarRecord("S", SurfaceOrientation.SouthVertical, 45, 110, 60, 10, 180)
            ]
        };

        var context = new Iso52016WeatherSolarContext(
            Year: 2024,
            TimeZoneOffset: TimeSpan.FromHours(5),
            LatitudeDegrees: 41.3,
            LongitudeDegrees: 69.3,
            Hours: [hour0, hour1])
        {
            Diagnostics =
            [
                new CalculationDiagnostic(
                    CalculationDiagnosticSeverity.Info,
                    "AE-WTH-001",
                    "Weather source accepted")
            ]
        };

        return new WeatherSolarTraceSource(
            Context: context,
            WeatherSource: "DeterministicFixtureWeather",
            Assumptions: ["Local solar-time conversion uses fixed timezone offset."]);
    }

    private static ThermalTopologyTraceSource CreateTopologySource()
    {
        var diagnostics = new[]
        {
            new StandardCalculationDiagnostic(
                CalculationDiagnosticSeverity.Warning,
                "AE-TOPO-ADJ-SAMEUSE",
                "Same-use adjacent boundary treated as adiabatic.",
                Source: "ThermalTopology"),
            new StandardCalculationDiagnostic(
                CalculationDiagnosticSeverity.Error,
                "AE-TOPO-INVALID-LINK",
                "Invalid boundary link found.",
                Source: "ThermalTopology")
        };

        var surfaces = new[]
        {
            new ThermalTopologySurface(
                "surf-ext",
                "room-1",
                "zone-1",
                ThermalBoundaryKind.Outdoor,
                10,
                0.3,
                null,
                null,
                "fixture",
                diagnostics),
            new ThermalTopologySurface(
                "surf-adj",
                "room-1",
                "zone-1",
                ThermalBoundaryKind.AdjacentConditionedZone,
                8,
                0.4,
                "zone-2",
                "room-2",
                "fixture",
                diagnostics)
        };

        var disclosure = CreateDisclosure(
            family: StandardCalculationFamily.InternalEngineering,
            stage: StandardCalculationStage.Foundation,
            assumptions: ["Topology boundaries are deterministic for fixture anchor."]);

        var topology = new BuildingThermalTopology(
            BuildingId: "building-trace-topology",
            Zones:
            [
                new ThermalTopologyZone("zone-1", "Zone 1", ["room-1"], diagnostics),
                new ThermalTopologyZone("zone-2", "Zone 2", ["room-2"], diagnostics)
            ],
            Rooms:
            [
                new ThermalTopologyRoom("room-1", "zone-1", 120, 50, surfaces, diagnostics),
                new ThermalTopologyRoom("room-2", "zone-2", 80, 35, surfaces, diagnostics)
            ],
            Surfaces: surfaces,
            Disclosure: disclosure,
            Diagnostics: diagnostics);

        return new ThermalTopologyTraceSource(
            Topology: topology,
            AdjacentBoundaryPolicy: "same-use-adiabatic");
    }

    private static Iso52016MultiZoneTraceSource CreateMultiZoneSource()
    {
        var diagnostics = new[]
        {
            new StandardCalculationDiagnostic(
                CalculationDiagnosticSeverity.Info,
                "AE-MZ-001",
                "Multi-zone lane aggregation is deterministic.",
                Source: "Iso52016MultiZone")
        };

        var result = new MultiZoneCalculationResult(
            BuildingId: "building-mz",
            Zones:
            [
                new ThermalZoneNode("zone-1", "Zone 1", 50, 120, ["bnd-1"]),
                new ThermalZoneNode("zone-2", "Zone 2", 35, 80, ["bnd-2"])
            ],
            BoundaryLinks:
            [
                new ThermalZoneBoundaryLink("bnd-1", MultiZoneBoundaryLinkType.ExternalBoundary, "zone-1", "S1", 20, 8),
                new ThermalZoneBoundaryLink("bnd-2", MultiZoneBoundaryLinkType.InterZoneBoundary, "zone-2", "S2", 15, 5, "zone-1")
            ],
            InterZoneConductanceLinks:
            [
                new InterZoneConductanceLink("c1", "zone-1", "zone-2", 12)
            ],
            InterZoneAirflowLinks:
            [
                new InterZoneAirflowLink("a1", "zone-1", "zone-2", 0.02)
            ],
            HourlyResults:
            [
                new MultiZoneHourlyResult(0, new Dictionary<string, double> { ["zone-1"] = 20, ["zone-2"] = 21 }, new Dictionary<string, double> { ["zone-1"] = 110 }, new Dictionary<string, double> { ["zone-2"] = 20 }, 110, 20),
                new MultiZoneHourlyResult(1, new Dictionary<string, double> { ["zone-1"] = 19, ["zone-2"] = 21 }, new Dictionary<string, double> { ["zone-1"] = 90 }, new Dictionary<string, double> { ["zone-2"] = 25 }, 90, 25)
            ],
            AnnualSummary: new MultiZoneAnnualSummary(
                new Dictionary<string, double> { ["zone-1"] = 1000, ["zone-2"] = 600 },
                new Dictionary<string, double> { ["zone-1"] = 220, ["zone-2"] = 260 }),
            Diagnostics: diagnostics);

        return new Iso52016MultiZoneTraceSource(
            Result: result,
            CalculationMode: "Hourly",
            TimeStepCount: result.HourlyResults.Count,
            Assumptions: ["Transmission/ventilation/solar/internal lanes are summarized from current public multi-zone outputs."]);
    }

    private static NaturalVentilationTraceSource CreateVentilationSource()
    {
        var result = new Iso16798NaturalVentilationResult(
            CalculationMode: Iso16798NaturalVentilationCalculationMode.StackAndWind,
            EffectiveOpeningAreaM2: 1.5,
            StackAirflowM3PerS: 0.12,
            WindAirflowM3PerS: 0.08,
            TotalAirflowM3PerS: 0.2,
            TotalAirflowM3PerH: 720,
            AirChangesPerHour: 1.8,
            ClampedAirChangesPerHour: 1.5,
            HeatTransferCoefficientWPerK: 65,
            Diagnostics:
            [
                new Iso16798NaturalVentilationDiagnostics("AE-VENT-LOCKOUT", "Lockout active below outdoor threshold.")
            ],
            SelectedBranch: "StackAndWind",
            ControlReason: "Control schedule allows opening.",
            ClampReason: "ACH clamped to configured maximum.");

        return new NaturalVentilationTraceSource(
            Result: result,
            OpeningCount: 3,
            ControlMode: "Schedule",
            Assumptions: ["Openings are treated as symmetric and discharge coefficients are deterministic in fixture context."]);
    }

    private static GroundTraceSource CreateGroundSource()
    {
        var result = new GroundHeatTransferResult(
            GroundTemperatureProfileCelsius: [10d, 10.5, 11.1, 11.8],
            EquivalentGroundHeatTransferCoefficientWPerKelvin: 12.5,
            HeatFlowProfileWatts: [100, 80, 60, 40],
            AnnualHeatLossKiloWattHours: 520,
            AnnualHeatGainKiloWattHours: 120,
            Assumptions: ["Constant annual mean boundary temperature anchor."],
            Warnings: ["Ground profile fallback was not needed."],
            Diagnostics:
            [
                new StandardCalculationDiagnostic(
                    CalculationDiagnosticSeverity.Info,
                    "AE-GROUND-TRACE-001",
                    "Ground profile computed.",
                    Source: "Ground")
            ]);

        return new GroundTraceSource(
            Result: result,
            GroundBoundaryCount: 1,
            ProfileMode: "ConstantAnnualMean",
            HeatTransferConvention: "Equivalent H_ground");
    }

    private static DomesticHotWaterTraceSource CreateDhwSource()
    {
        var disclosure = CreateDisclosure(
            StandardCalculationFamily.ISO12831,
            StandardCalculationStage.DomesticHotWater,
            ["DHW fixture uses deterministic per-person basis."]);
        var diagnostics = new[]
        {
            new StandardCalculationDiagnostic(
                CalculationDiagnosticSeverity.Info,
                "AE-DHW-TRACE-001",
                "DHW deterministic trace fixture.",
                Source: "DomesticHotWater")
        };

        var useful = new DomesticHotWaterUsefulDemandResult(
            CalculationId: "dhw-calc-1",
            BuildingId: "building-1",
            ZoneId: "zone-1",
            RoomId: "room-1",
            DemandBasis: DomesticHotWaterDemandBasis.PerPerson,
            UseCategory: DomesticHotWaterUseCategory.Residential,
            DailyVolumeLiters: 120,
            AnnualVolumeLiters: 43800,
            MonthlyVolumeLiters: Enumerable.Repeat(3650d, 12).ToArray(),
            HourlyVolumeLiters8760: Enumerable.Repeat(5d, 24).ToArray(),
            TemperatureRiseKelvin: 35,
            DailyUsefulEnergyKWh: 4.8,
            AnnualUsefulEnergyKWh: 1752,
            MonthlyUsefulEnergyKWh: Enumerable.Repeat(146d, 12).ToArray(),
            HourlyUsefulEnergyKWh8760: Enumerable.Repeat(0.2d, 24).ToArray(),
            Disclosure: disclosure,
            Diagnostics: diagnostics);

        var lossComponent = new DomesticHotWaterLossComponentResult(
            ComponentKind: DomesticHotWaterLossComponentKind.Storage,
            AnnualLossKWh: 120,
            MonthlyLossKWh: Enumerable.Repeat(10d, 12).ToArray(),
            HourlyLossKWh8760: Enumerable.Repeat(0.013d, 24).ToArray(),
            AnnualRecoverableLossKWh: 40,
            AnnualNonRecoverableLossKWh: 80,
            HourlyRecoverableLossKWh8760: Enumerable.Repeat(0.004d, 24).ToArray(),
            HourlyNonRecoverableLossKWh8760: Enumerable.Repeat(0.009d, 24).ToArray(),
            RecoveryMode: DomesticHotWaterLossRecoveryMode.PartiallyRecoverable,
            Diagnostics: diagnostics);

        var handoff = new DomesticHotWaterEn15316Handoff(
            CalculationId: "dhw-calc-1",
            EndUse: "DomesticHotWater",
            UsefulEnergySource: "DHW",
            AnnualUsefulDhwEnergyKWh: 1752,
            AnnualDhwSystemHeatRequirementKWh: 1940,
            AnnualDhwAuxiliaryElectricityKWh: 55,
            HourlyUsefulDhwEnergyKWh8760: Enumerable.Repeat(0.2d, 24).ToArray(),
            HourlyDhwSystemHeatRequirementKWh8760: Enumerable.Repeat(0.22d, 24).ToArray(),
            HourlyDhwAuxiliaryElectricityKWh8760: Enumerable.Repeat(0.006d, 24).ToArray(),
            HourlyRecoverableLossKWh8760: Enumerable.Repeat(0.004d, 24).ToArray(),
            HourlyNonRecoverableLossKWh8760: Enumerable.Repeat(0.009d, 24).ToArray(),
            Diagnostics: diagnostics);

        var result = new DomesticHotWaterSystemLoadResult(
            CalculationId: "dhw-calc-1",
            BuildingId: "building-1",
            ZoneId: "zone-1",
            RoomId: "room-1",
            UsefulDemand: useful,
            LossComponents: [lossComponent],
            AnnualUsefulEnergyKWh: 1752,
            AnnualStorageLossKWh: 120,
            AnnualDistributionLossKWh: 40,
            AnnualCirculationLossKWh: 28,
            AnnualAuxiliaryElectricityKWh: 55,
            AnnualRecoverableLossKWh: 60,
            AnnualNonRecoverableLossKWh: 128,
            AnnualSystemHeatRequirementKWh: 1940,
            MonthlySystemHeatRequirementKWh: Enumerable.Repeat(161.7d, 12).ToArray(),
            HourlySystemHeatRequirementKWh8760: Enumerable.Repeat(0.22d, 24).ToArray(),
            HourlyRecoverableLossKWh8760: Enumerable.Repeat(0.004d, 24).ToArray(),
            HourlyNonRecoverableLossKWh8760: Enumerable.Repeat(0.009d, 24).ToArray(),
            HourlyAuxiliaryElectricityKWh8760: Enumerable.Repeat(0.006d, 24).ToArray(),
            En15316Handoff: handoff,
            Disclosure: disclosure,
            Diagnostics: diagnostics);

        return new DomesticHotWaterTraceSource(
            Result: result,
            DemandBasis: "PerPerson",
            DrawOffProfileMode: "ResidentialDeterministic");
    }

    private static SystemEnergyTraceSource CreateSystemEnergySource()
    {
        var diagnostics = new[]
        {
            new StandardCalculationDiagnostic(
                CalculationDiagnosticSeverity.Warning,
                "AE-SYS-FACTOR-FALLBACK",
                "Fallback factor used for one auxiliary lane.",
                Source: "SystemEnergy")
        };

        var result = new SystemEnergyCalculationResult(
            UsefulEnergyByUseKWh: new Dictionary<SystemEnergyUseKind, IReadOnlyList<double>>
            {
                [SystemEnergyUseKind.SpaceHeating] = [100, 110, 90],
                [SystemEnergyUseKind.DomesticHotWater] = [40, 45, 42]
            },
            SystemLoadByUseKWh: new Dictionary<SystemEnergyUseKind, IReadOnlyList<double>>
            {
                [SystemEnergyUseKind.SpaceHeating] = [120, 132, 108]
            },
            EmissionLossesByUseKWh: new Dictionary<SystemEnergyUseKind, IReadOnlyList<double>>
            {
                [SystemEnergyUseKind.SpaceHeating] = [10, 11, 9]
            },
            DistributionLossesByUseKWh: new Dictionary<SystemEnergyUseKind, IReadOnlyList<double>>
            {
                [SystemEnergyUseKind.SpaceHeating] = [5, 6, 5]
            },
            StorageLossesByUseKWh: new Dictionary<SystemEnergyUseKind, IReadOnlyList<double>>
            {
                [SystemEnergyUseKind.SpaceHeating] = [2, 2, 2]
            },
            GenerationLossesByUseKWh: new Dictionary<SystemEnergyUseKind, IReadOnlyList<double>>
            {
                [SystemEnergyUseKind.SpaceHeating] = [15, 16, 14]
            },
            RecoveredLossesByUseKWh: new Dictionary<SystemEnergyUseKind, IReadOnlyList<double>>
            {
                [SystemEnergyUseKind.SpaceHeating] = [3, 3, 3]
            },
            AuxiliaryEnergyByUseKWh: new Dictionary<SystemEnergyUseKind, IReadOnlyList<double>>
            {
                [SystemEnergyUseKind.SpaceHeating] = [4, 4, 4]
            },
            FinalEnergyByCarrierKWh: new Dictionary<SystemEnergyCarrierKind, IReadOnlyList<double>>
            {
                [SystemEnergyCarrierKind.NaturalGas] = [140, 150, 130],
                [SystemEnergyCarrierKind.Electricity] = [24, 25, 23]
            },
            PrimaryEnergyByCarrierKWh: new Dictionary<SystemEnergyCarrierKind, IReadOnlyList<double>>
            {
                [SystemEnergyCarrierKind.NaturalGas] = [154, 165, 143],
                [SystemEnergyCarrierKind.Electricity] = [60, 63, 58]
            },
            Co2ByCarrierKg: new Dictionary<SystemEnergyCarrierKind, IReadOnlyList<double>>
            {
                [SystemEnergyCarrierKind.NaturalGas] = [26, 28, 24],
                [SystemEnergyCarrierKind.Electricity] = [8, 9, 8]
            },
            MonthlyFinalEnergyKWh: Enumerable.Repeat(30d, 12).ToArray(),
            AnnualSummary: new SystemEnergyAnnualSummary(
                UsefulEnergyKWh: 427,
                SystemLoadKWh: 500,
                EmissionLossesKWh: 30,
                DistributionLossesKWh: 16,
                StorageLossesKWh: 6,
                GenerationLossesKWh: 45,
                RecoveredLossesKWh: 9,
                AuxiliaryEnergyKWh: 12,
                FinalEnergyKWh: 492,
                PrimaryEnergyKWh: 643,
                Co2Kg: 103),
            Assumptions: ["No double counting is enforced via ownership policy."],
            Warnings: ["Fallback factor used for one auxiliary lane."],
            Diagnostics: diagnostics);

        return new SystemEnergyTraceSource(
            Result: result,
            IntakeUses: ["SpaceHeating", "SpaceCooling", "DomesticHotWater"],
            StageChain: "Emission->Distribution->Storage->Generation->Final->Primary->CO2",
            OwnershipDecision: "Ownership policy prevents double counting between DHW and HVAC modules.");
    }

    private static StandardCalculationDisclosure CreateDisclosure(
        StandardCalculationFamily family,
        StandardCalculationStage stage,
        IReadOnlyList<string> assumptions) =>
        new(
            Family: family,
            Stage: stage,
            Mode: StandardCalculationMode.InternalEngineering,
            CalculationPath: "internal-engineering",
            IsFallback: false,
            UsesExternalValidation: false,
            ClaimBoundary: new StandardClaimBoundary(
                AllowedClaims: ["Internal engineering implementation"],
                ForbiddenClaims:
                [
                    "Full ISO compliance",
                    "Full EN compliance",
                    "StandardReference equivalence",
                    "EnergyPlus comparison workflow",
                    "ASHRAE 140 / BESTEST-style validation anchor"
                ],
                Limitations: ["Validation anchors only."],
                Assumptions: assumptions),
            Diagnostics: []);

    private static IEnumerable<CalculationTraceStep> FlattenSteps(
        IEnumerable<CalculationTraceStep> steps)
    {
        foreach (var step in steps)
        {
            yield return step;

            foreach (var child in FlattenSteps(step.ChildSteps))
            {
                yield return child;
            }
        }
    }
}
