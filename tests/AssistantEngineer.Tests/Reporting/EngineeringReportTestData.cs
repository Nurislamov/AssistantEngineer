using AssistantEngineer.Modules.Calculations.Application.Contracts.AnnualEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Diagnostics;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.MultiZone;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Engineering;

namespace AssistantEngineer.Tests.Reporting;

internal static class EngineeringReportTestData
{
    public static readonly DateTimeOffset FixedTimestamp = new(2026, 5, 9, 10, 0, 0, TimeSpan.Zero);

    public static EngineeringReportGenerationRequest CreateMinimalRequest() =>
        new(
            ReportKind: EngineeringReportKind.CalculationSummary,
            RequestedFormat: EngineeringReportFormat.Json,
            ReportTitle: "Minimal report",
            ProjectId: "project-min",
            BuildingId: "building-min",
            ValidationDiagnostics:
            [
                new CalculationDiagnostic(CalculationDiagnosticSeverity.Warning, "AE-MIN-WARN", "Minimal warning", "Minimal")
            ],
            Assumptions: ["Minimal assumption"],
            Warnings: ["Minimal warning"],
            IncludeLimitations: true,
            IncludeTraceAppendix: false,
            DeterministicTimestampUtc: FixedTimestamp,
            SourceCalculationIds: ["calc-min-001"]);

    public static EngineeringReportGenerationRequest CreateHeatingCoolingRequest() =>
        CreateMinimalRequest() with
        {
            ReportKind = EngineeringReportKind.HeatingCoolingLoad,
            ReportTitle = "Heating and cooling report",
            HeatingCoolingSummary = CreateBuildingEnergyBalanceResult()
        };

    public static EngineeringReportGenerationRequest CreateDhwRequest() =>
        CreateMinimalRequest() with
        {
            ReportKind = EngineeringReportKind.DomesticHotWater,
            ReportTitle = "DHW report",
            DomesticHotWaterSummary = CreateDomesticHotWaterResult()
        };

    public static EngineeringReportGenerationRequest CreateSystemEnergyRequest() =>
        CreateMinimalRequest() with
        {
            ReportKind = EngineeringReportKind.SystemEnergy,
            ReportTitle = "System energy report",
            SystemEnergySummary = CreateSystemEnergySummary()
        };

    public static EngineeringReportGenerationRequest CreateFullRequest(bool includeTrace = true) =>
        CreateMinimalRequest() with
        {
            ReportKind = EngineeringReportKind.FullEngineeringCore,
            ReportTitle = "Full engineering core report",
            HeatingCoolingSummary = CreateBuildingEnergyBalanceResult(),
            MultiZoneSummary = new MultiZoneAnnualSummary(
                AnnualHeatingEnergyByZoneKWh: new Dictionary<string, double> { ["Zone-A"] = 1200d },
                AnnualCoolingEnergyByZoneKWh: new Dictionary<string, double> { ["Zone-A"] = 800d }),
            NaturalVentilationSummary = CreateNaturalVentilationSummary(),
            GroundSummary = CreateGroundSummary(),
            DomesticHotWaterSummary = CreateDomesticHotWaterResult(),
            SystemEnergySummary = CreateSystemEnergySummary(),
            CalculationTrace = includeTrace ? CreateTraceDocument() : null,
            IncludeTraceAppendix = includeTrace,
            DetailLevel = EngineeringReportDetailLevel.Standard,
            SourceCalculationIds = ["calc-iso52016-001", "calc-dhw-001", "calc-system-energy-001"]
        };

    public static CalculationTraceDocument CreateTraceDocument()
    {
        var weatherStep = new CalculationTraceStep(
            StepId: "weather-step",
            ModuleKind: CalculationTraceModuleKind.Weather,
            StepName: "Weather Source",
            Sequence: 1,
            InputValues: [],
            IntermediateValues: [],
            OutputValues:
            [
                new CalculationTraceValue("weather_source", "Weather source", "EPW", null, CalculationTraceValueKind.Output)
            ],
            FormulaOrConventionLabel: null,
            Assumptions: ["Weather normalization to 8760 records"],
            Warnings: [],
            Diagnostics: [],
            ChildSteps: []);

        var systemStep = new CalculationTraceStep(
            StepId: "system-step",
            ModuleKind: CalculationTraceModuleKind.SystemEnergy,
            StepName: "System energy summary",
            Sequence: 2,
            InputValues: [],
            IntermediateValues: [],
            OutputValues:
            [
                new CalculationTraceValue("final_energy_total", "Final energy total", 5100d, new CalculationTraceUnit("kWh"), CalculationTraceValueKind.Output)
            ],
            FormulaOrConventionLabel: "EN15316-inspired chain",
            Assumptions: ["No double counting ownership policy"],
            Warnings: [],
            Diagnostics:
            [
                new CalculationTraceDiagnostic(
                    CalculationTraceSeverity.Warning,
                    "AE-TRACE-FACTOR-MISSING",
                    "Fallback factor applied.",
                    CalculationTraceModuleKind.SystemEnergy,
                    "SystemEnergy")
            ],
            ChildSteps: []);

        return new CalculationTraceDocument(
            TraceId: "trace-report-001",
            CalculationId: "calc-report-001",
            CalculationType: "FullEngineeringCore",
            CreatedTimestampUtc: FixedTimestamp,
            RootModule: CalculationTraceModuleKind.Reporting,
            Steps: [weatherStep, systemStep],
            Summary: new CalculationTraceSummary(
                StepCount: 2,
                DiagnosticCount: 1,
                WarningCount: 1,
                AssumptionCount: 2,
                Modules: [CalculationTraceModuleKind.Weather, CalculationTraceModuleKind.SystemEnergy]),
            Assumptions: ["Trace assumption"],
            Warnings: ["Trace warning"],
            Diagnostics:
            [
                new CalculationTraceDiagnostic(
                    CalculationTraceSeverity.Warning,
                    "AE-TRACE-WARN",
                    "Trace warning",
                    CalculationTraceModuleKind.Reporting)
            ],
            Metadata: new Dictionary<string, string> { ["trace_profile"] = "deterministic" },
            SchemaVersion: "1.0");
    }

    public static BuildingEnergyBalanceResult CreateBuildingEnergyBalanceResult() =>
        new()
        {
            BuildingId = 101,
            BuildingName = "Building-A",
            CoolingCalculationMethod = "Iso52016",
            HeatingCalculationMethod = "Iso52016",
            RequestedCoolingMethod = "Iso52016",
            RequestedHeatingMethod = "Iso52016",
            ActualMethod = "Iso52016.MultiZone",
            CalculationMethodLabel = "Iso52016MultiZone",
            EnergyDataSource = "TrueHourlySimulation",
            IsTrueHourly8760 = true,
            HourlyRecordCount = 8760,
            AnnualCoolingDemandKWh = 820.2,
            AnnualHeatingDemandKWh = 1250.4,
            AnnualTotalDemandKWh = 2070.6,
            PeakHeatingW = 4800,
            PeakCoolingW = 3900,
            ComponentBreakdown = new AnnualEnergyComponentBreakdown(
                TransmissionKWh: 720,
                VentilationKWh: 210,
                InfiltrationKWh: 80,
                SolarGainsKWh: -310,
                InternalGainsKWh: -220,
                GroundKWh: 95),
            MonthlyBalances =
            [
                new MonthlyEnergyBalance { Month = 1, HeatingDemandKWh = 180, CoolingDemandKWh = 10 },
                new MonthlyEnergyBalance { Month = 7, HeatingDemandKWh = 15, CoolingDemandKWh = 210 }
            ],
            Diagnostics =
            [
                new CalculationDiagnostic(CalculationDiagnosticSeverity.Info, "AE-HC-INFO", "Heating/cooling summary generated.")
            ],
            Assumptions = ["8760 weather profile used"]
        };

    public static NaturalVentilationCalculationResult CreateNaturalVentilationSummary() =>
        new(
            CalculationId: "nv-001",
            FlowConfiguration: NaturalVentilationFlowConfiguration.CrossVentilation,
            TotalAirflowCubicMetersPerSecond: 0.22,
            TotalAirflowCubicMetersPerHour: 792d,
            TotalAirflowKilogramsPerSecond: 0.264,
            Openings:
            [
                new NaturalVentilationOpeningResult(
                    OpeningId: "opening-1",
                    RoomId: "room-1",
                    ZoneId: "zone-1",
                    SurfaceId: "surface-1",
                    EffectiveOpeningAreaSquareMeters: 1.2,
                    DischargeCoefficient: 0.62,
                    WindPressureDifferencePa: 12,
                    StackPressureDifferencePa: 6,
                    CombinedPressureDifferencePa: 18,
                    AirflowCubicMetersPerSecond: 0.22,
                    AirflowCubicMetersPerHour: 792,
                    AirflowKilogramsPerSecond: 0.264,
                    Diagnostics: [])
            ],
            Disclosure: CreateDisclosure(StandardCalculationFamily.EN16798, StandardCalculationStage.Ventilation),
            Diagnostics:
            [
                new StandardCalculationDiagnostic(
                    CalculationDiagnosticSeverity.Warning,
                    "AE-NV-LOCKOUT",
                    "Night lockout assumption applied.",
                    "NaturalVentilation")
            ]);

    public static BuildingGroundBoundaryCalculationResult CreateGroundSummary()
    {
        var surfaceResult = new GroundSurfaceBoundaryCalculationResult(
            SurfaceId: "surface-g-1",
            BuildingId: "building-1",
            ZoneId: "zone-1",
            RoomId: "room-1",
            ContactKind: GroundContactKind.SlabOnGround,
            EquivalentUValueWPerSquareMeterKelvin: 0.38,
            HeatTransferCoefficientWPerKelvin: 55,
            MonthlyGroundBoundaryTemperaturesCelsius: Enumerable.Repeat(16d, 12).ToArray(),
            HourlyGroundBoundaryTemperaturesCelsius: Enumerable.Repeat(16d, 24).ToArray(),
            GroundResult: new GroundBoundaryCalculationResult(
                BoundaryId: "gb-1",
                BuildingId: "building-1",
                ZoneId: "zone-1",
                RoomId: "room-1",
                SurfaceId: "surface-g-1",
                ContactKind: GroundContactKind.SlabOnGround,
                EquivalentUValueWPerSquareMeterKelvin: 0.38,
                HeatTransferCoefficientWPerKelvin: 55,
                CharacteristicDimensionMeters: 4.5,
                MonthlyGroundBoundaryTemperaturesCelsius: Enumerable.Repeat(16d, 12).ToArray(),
                HourlyGroundBoundaryTemperaturesCelsius: Enumerable.Repeat(16d, 24).ToArray(),
                Disclosure: CreateDisclosure(StandardCalculationFamily.ISO13370, StandardCalculationStage.BoundaryCondition),
                Diagnostics: []),
            Diagnostics: []);

        return new BuildingGroundBoundaryCalculationResult(
            BuildingId: "building-1",
            GroundSurfaces: [surfaceResult],
            SurfaceHeatTransferCoefficientsWPerKelvin: new Dictionary<string, double> { ["surface-g-1"] = 55d },
            SurfaceHourlyGroundTemperaturesCelsius: new Dictionary<string, IReadOnlyList<double>> { ["surface-g-1"] = Enumerable.Repeat(16d, 24).ToArray() },
            SurfaceMonthlyGroundTemperaturesCelsius: new Dictionary<string, IReadOnlyList<double>> { ["surface-g-1"] = Enumerable.Repeat(16d, 12).ToArray() },
            TotalGroundHeatTransferCoefficientWPerKelvin: 55d,
            Disclosure: CreateDisclosure(StandardCalculationFamily.ISO13370, StandardCalculationStage.Reporting),
            Diagnostics:
            [
                new StandardCalculationDiagnostic(
                    CalculationDiagnosticSeverity.Info,
                    "AE-GROUND-INFO",
                    "Ground profile synthesized from deterministic fixture.")
            ]);
    }

    public static DomesticHotWaterSystemLoadResult CreateDomesticHotWaterResult()
    {
        var useful = new DomesticHotWaterUsefulDemandResult(
            CalculationId: "dhw-useful-001",
            BuildingId: "building-1",
            ZoneId: "zone-1",
            RoomId: "room-1",
            DemandBasis: DomesticHotWaterDemandBasis.PerPerson,
            UseCategory: DomesticHotWaterUseCategory.Residential,
            DailyVolumeLiters: 210,
            AnnualVolumeLiters: 76650,
            MonthlyVolumeLiters: Enumerable.Repeat(6387.5d, 12).ToArray(),
            HourlyVolumeLiters8760: Enumerable.Repeat(8.75d, 24).ToArray(),
            TemperatureRiseKelvin: 35,
            DailyUsefulEnergyKWh: 8.5,
            AnnualUsefulEnergyKWh: 3102.5,
            MonthlyUsefulEnergyKWh: Enumerable.Repeat(258.5d, 12).ToArray(),
            HourlyUsefulEnergyKWh8760: Enumerable.Repeat(0.35d, 24).ToArray(),
            Disclosure: CreateDisclosure(StandardCalculationFamily.ISO12831, StandardCalculationStage.DomesticHotWater),
            Diagnostics: []);

        var loss = new DomesticHotWaterLossComponentResult(
            ComponentKind: DomesticHotWaterLossComponentKind.Storage,
            AnnualLossKWh: 220,
            MonthlyLossKWh: Enumerable.Repeat(18.33d, 12).ToArray(),
            HourlyLossKWh8760: Enumerable.Repeat(0.025d, 24).ToArray(),
            AnnualRecoverableLossKWh: 44,
            AnnualNonRecoverableLossKWh: 176,
            HourlyRecoverableLossKWh8760: Enumerable.Repeat(0.005d, 24).ToArray(),
            HourlyNonRecoverableLossKWh8760: Enumerable.Repeat(0.02d, 24).ToArray(),
            RecoveryMode: DomesticHotWaterLossRecoveryMode.PartiallyRecoverable,
            Diagnostics: []);

        var handoff = new DomesticHotWaterEn15316Handoff(
            CalculationId: "dhw-handoff-001",
            EndUse: "DomesticHotWater",
            UsefulEnergySource: "ISO12831",
            AnnualUsefulDhwEnergyKWh: 3102.5,
            AnnualDhwSystemHeatRequirementKWh: 3600,
            AnnualDhwAuxiliaryElectricityKWh: 120,
            HourlyUsefulDhwEnergyKWh8760: Enumerable.Repeat(0.35d, 24).ToArray(),
            HourlyDhwSystemHeatRequirementKWh8760: Enumerable.Repeat(0.41d, 24).ToArray(),
            HourlyDhwAuxiliaryElectricityKWh8760: Enumerable.Repeat(0.014d, 24).ToArray(),
            HourlyRecoverableLossKWh8760: Enumerable.Repeat(0.005d, 24).ToArray(),
            HourlyNonRecoverableLossKWh8760: Enumerable.Repeat(0.02d, 24).ToArray(),
            Diagnostics: [],
            LossOwnershipPolicy: DomesticHotWaterLossOwnershipPolicy.NoDoubleCounting);

        return new DomesticHotWaterSystemLoadResult(
            CalculationId: "dhw-system-001",
            BuildingId: "building-1",
            ZoneId: "zone-1",
            RoomId: "room-1",
            UsefulDemand: useful,
            LossComponents: [loss],
            AnnualUsefulEnergyKWh: 3102.5,
            AnnualStorageLossKWh: 220,
            AnnualDistributionLossKWh: 150,
            AnnualCirculationLossKWh: 127.5,
            AnnualAuxiliaryElectricityKWh: 120,
            AnnualRecoverableLossKWh: 80,
            AnnualNonRecoverableLossKWh: 417.5,
            AnnualSystemHeatRequirementKWh: 3600,
            MonthlySystemHeatRequirementKWh: Enumerable.Repeat(300d, 12).ToArray(),
            HourlySystemHeatRequirementKWh8760: Enumerable.Repeat(0.41d, 24).ToArray(),
            HourlyRecoverableLossKWh8760: Enumerable.Repeat(0.005d, 24).ToArray(),
            HourlyNonRecoverableLossKWh8760: Enumerable.Repeat(0.02d, 24).ToArray(),
            HourlyAuxiliaryElectricityKWh8760: Enumerable.Repeat(0.014d, 24).ToArray(),
            En15316Handoff: handoff,
            Disclosure: CreateDisclosure(StandardCalculationFamily.EN15316, StandardCalculationStage.DomesticHotWater),
            Diagnostics:
            [
                new StandardCalculationDiagnostic(
                    CalculationDiagnosticSeverity.Info,
                    "AE-DHW-INFO",
                    "Deterministic DHW summary fixture.")
            ]);
    }

    public static SystemEnergyCalculationSummary CreateSystemEnergySummary()
    {
        var carrier = new SystemEnergyCarrierSummary(
            Carrier: SystemEnergyCarrier.Electricity,
            AnnualFinalEnergyKWh: 5100,
            AnnualRenewablePrimaryEnergyKWh: 610,
            AnnualNonRenewablePrimaryEnergyKWh: 7010,
            AnnualTotalPrimaryEnergyKWh: 7620,
            AnnualEmissionsKg: 1220,
            MonthlyFinalEnergyKWh: Enumerable.Repeat(425d, 12).ToArray(),
            MonthlyTotalPrimaryEnergyKWh: Enumerable.Repeat(635d, 12).ToArray(),
            Diagnostics: []);

        var endUse = new SystemEnergyEndUseSummary(
            EndUse: SystemEnergyEndUse.SpaceHeating,
            AnnualFinalEnergyKWh: 4300,
            AnnualRenewablePrimaryEnergyKWh: 510,
            AnnualNonRenewablePrimaryEnergyKWh: 5920,
            AnnualTotalPrimaryEnergyKWh: 6430,
            AnnualEmissionsKg: 980,
            AnnualFinalEnergyByCarrierKWh: new Dictionary<SystemEnergyCarrier, double>
            {
                [SystemEnergyCarrier.Electricity] = 4300
            },
            Diagnostics: []);

        return new SystemEnergyCalculationSummary(
            CalculationId: "system-energy-001",
            AnnualTotalFinalEnergyKWh: 5100,
            AnnualTotalRenewablePrimaryEnergyKWh: 610,
            AnnualTotalNonRenewablePrimaryEnergyKWh: 7010,
            AnnualTotalPrimaryEnergyKWh: 7620,
            AnnualTotalEmissionsKg: 1220,
            Carriers: [carrier],
            EndUses: [endUse],
            DisclosureSummary: new SystemEnergyDisclosureSummary(
                Status: SystemEnergyDisclosureStatus.InternalEngineering,
                AllowedClaims: ["Internal engineering implementation"],
                ForbiddenClaims: ["Full standard compliance"],
                Assumptions: ["Ownership/no-double-counting policy applied."],
                Limitations: ["Validation anchors only."],
                Diagnostics: []),
            Diagnostics:
            [
                new StandardCalculationDiagnostic(
                    CalculationDiagnosticSeverity.Warning,
                    "AE-SYS-FACTOR-FALLBACK",
                    "Missing factor fallback applied.",
                    "SystemEnergy",
                    "SystemEnergySummaryBuilder",
                    StandardCalculationFamily.EN15316,
                    StandardCalculationStage.SystemEnergy)
            ]);
    }

    private static StandardCalculationDisclosure CreateDisclosure(
        StandardCalculationFamily family,
        StandardCalculationStage stage) =>
        new(
            Family: family,
            Stage: stage,
            Mode: StandardCalculationMode.InternalEngineering,
            CalculationPath: $"{family}.{stage}",
            IsFallback: false,
            UsesExternalValidation: false,
            ClaimBoundary: new StandardClaimBoundary(
                AllowedClaims: ["Internal engineering implementation", "Deterministic fixtures", "Validation anchors only"],
                ForbiddenClaims: ["Full compliance", "Exact EnergyPlus equivalence"],
                Limitations: ["No legal compliance certificate"],
                Assumptions: ["Deterministic fixture baseline"]),
            Diagnostics: []);
}

