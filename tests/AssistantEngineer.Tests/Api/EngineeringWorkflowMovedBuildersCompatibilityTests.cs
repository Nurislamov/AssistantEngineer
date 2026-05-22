using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Workflow;
using AssistantEngineer.Modules.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;
using AssistantEngineer.Modules.Reporting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.Api;

public sealed class EngineeringWorkflowMovedBuildersCompatibilityTests
{
    [Fact]
    public void TracePreviewBuilderProducesDeterministicSummaryForFixtureState()
    {
        using var provider = BuildProvider();
        var service = provider.GetRequiredService<IEngineeringWorkflowTracePreviewService>();

        var state = CreateFixtureState();
        var diagnostics = CreateFixtureDiagnostics();

        var trace = service.BuildTraceDocument(
            state,
            CalculationTraceDetailLevel.Summary,
            diagnostics);

        var summary = service.BuildTraceSummary(trace, "Summary");

        Assert.Equal("workflow-trace-42-7", trace.TraceId);
        Assert.Equal(3, trace.Steps.Count);
        Assert.Equal(3, summary.Steps.Count);
        Assert.Equal("Project and building context", summary.Steps[0].StepName);
        Assert.Equal("Validation diagnostics aggregation", summary.Steps[1].StepName);
        Assert.Equal("Report preview readiness", summary.Steps[2].StepName);
        Assert.Equal(["Reporting", "Validation"], summary.Modules);
    }

    [Fact]
    public void ReportPreviewBuilderProducesStablePreviewEnvelopeForFixtureState()
    {
        using var provider = BuildProvider();
        var service = provider.GetRequiredService<IEngineeringWorkflowReportPreviewService>();

        var state = CreateFixtureState();
        var diagnostics = CreateFixtureDiagnostics();
        var request = new EngineeringWorkflowReportRequestDto(
            State: state,
            ReportKind: "FullEngineeringCore",
            RequestedFormat: "Json",
            DetailLevel: "Summary",
            IncludeTraceAppendix: true,
            IncludeLimitations: true);

        var report = service.BuildReportDocument(request, diagnostics);
        var preview = service.BuildReportPreview(report);

        Assert.Equal("Engineering workflow report - Fixture project", report.Title);
        Assert.Equal(report.ReportKind.ToString(), preview.ReportKind);
        Assert.Equal(["Json", "Markdown"], preview.ExportFormatsAvailable);
        Assert.Equal(report.Diagnostics.Count, preview.DiagnosticsCount);
        Assert.Equal(report.Warnings.Count, preview.WarningsCount);
        Assert.NotEmpty(preview.Sections);
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        services.AddCalculationsModule(configuration);
        services.AddReportingModule();
        services.AddScoped<IEngineeringWorkflowTracePreviewService, EngineeringWorkflowTracePreviewService>();
        services.AddScoped<IEngineeringWorkflowReportPreviewService, EngineeringWorkflowReportPreviewService>();

        return services.BuildServiceProvider();
    }

    private static EngineeringWorkflowStateDto CreateFixtureState()
    {
        return new EngineeringWorkflowStateDto(
            ProjectId: 42,
            ProjectName: "Fixture project",
            BuildingId: 7,
            CurrentStep: "Review",
            Steps: [],
            AvailableModules: ["Weather", "Solar", "Reporting"],
            BuildingMetadata: new EngineeringWorkflowBuildingMetadataDto(
                BuildingName: "Fixture building",
                LocationText: "Tashkent",
                FloorAreaM2: 120.5,
                VolumeM3: 360.0,
                NumberOfZones: 1,
                Notes: "Fixture metadata"),
            Zones:
            [
                new EngineeringWorkflowZoneDto(
                    ZoneId: "zone-1",
                    Name: "Zone 1",
                    ZoneKind: "Single-room",
                    FloorAreaM2: 120.5,
                    AirVolumeM3: 360.0,
                    Status: "valid")
            ],
            Boundaries:
            [
                new EngineeringWorkflowBoundaryDto(
                    BoundaryId: "b-1",
                    ZoneOrRoomName: "Room A",
                    ExposureKind: "External",
                    AreaM2: 25.0,
                    UValue: 0.35,
                    AdjacentZoneReference: null,
                    Indicator: "exterior",
                    ValidationStatus: "valid")
            ],
            WeatherSolarSettings: new EngineeringWorkflowWeatherSolarSettingsDto("Ready", "UTC+5", "Ready"),
            VentilationSettings: new EngineeringWorkflowVentilationSettingsDto(1, "Balanced", "Configured", []),
            GroundSettings: new EngineeringWorkflowGroundSettingsDto(1, "Configured", "valid"),
            DomesticHotWaterSettings: new EngineeringWorkflowDomesticHotWaterSettingsDto("Configured", "Configured", "Configured", "Configured"),
            SystemEnergySettings: new EngineeringWorkflowSystemEnergySettingsDto("Configured", "Configured", "Configured"),
            Diagnostics: [],
            Assumptions: ["Fixture assumption"],
            Links: [],
            CalculationTraceSummary: null,
            ReportSummary: null,
            Metadata: new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["mode"] = "api",
                ["stage"] = "foundation"
            });
    }

    private static IReadOnlyList<EngineeringWorkflowDiagnosticDto> CreateFixtureDiagnostics()
    {
        return
        [
            new EngineeringWorkflowDiagnosticDto(
                Severity: "warning",
                Code: "FIXTURE_WARNING",
                Message: "Fixture warning",
                SourceStep: "Ventilation",
                SourceModule: "Fixture"),
            new EngineeringWorkflowDiagnosticDto(
                Severity: "error",
                Code: "FIXTURE_ERROR",
                Message: "Fixture error",
                SourceStep: "Validation",
                SourceModule: "Fixture")
        ];
    }
}
