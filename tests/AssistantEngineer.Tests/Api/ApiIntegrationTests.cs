using System.Net.Http.Json;
using System.Net;
using System.Reflection;
using System.Text.Json;
using AssistantEngineer.Api.Contracts.Common;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Cooling;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Heating;
using AssistantEngineer.Modules.Benchmarks.Application.Abstractions;
using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Heating;
using AssistantEngineer.Modules.Equipment.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Benchmarks.Application.Contracts.Benchmarks;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Models.Heating;
using AssistantEngineer.Modules.Buildings.Application.Contracts.Responses;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Requests;
using AssistantEngineer.Modules.Equipment.Application.Contracts.Responses;
using AssistantEngineer.Modules.Equipment.Domain;
using AssistantEngineer.Modules.Buildings.Domain.Climate;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Buildings.Domain.Settings;
using AssistantEngineer.SharedKernel.Primitives;
using AssistantEngineer.SharedKernel.ValueObjects;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AssistantEngineer.Tests;

public class ApiIntegrationTests
{
    [Fact]
    public async Task UnversionedApiRouteIsNotMapped()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/reports/buildings/0/heating?method=En12831");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetV1HeatingReportReturnsReport()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/reports/buildings/0/heating?method=En12831");

        await EnsureSuccessWithBodyAsync(response);
        var report = await response.Content.ReadFromJsonAsync<BuildingHeatingReport>();
        Assert.NotNull(report);
        Assert.Equal("Integration project", report.ProjectName);
        Assert.Equal("Integration building", report.BuildingName);
        Assert.Equal(1, report.RoomsCount);
        Assert.True(report.TotalDesignHeatingLoadW > 0);
        Assert.True(response.Headers.TryGetValues("api-supported-versions", out var supportedVersions));
        Assert.Contains("1.0", supportedVersions);
    }

    [Fact]
    public async Task GetEnergyBalanceReturnsAnnualBalance()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/v1/buildings/0/load-calculations/energy-balance?coolingMethod=Simplified&heatingMethod=En12831");

        await EnsureSuccessWithBodyAsync(response);

        var balance = await response.Content.ReadFromJsonAsync<BuildingEnergyBalanceResult>();

        Assert.NotNull(balance);
        Assert.Equal("Integration building", balance.BuildingName);
        Assert.NotEmpty(balance.MonthlyBalances);
        Assert.True(balance.AnnualTotalDemandKWh > 0);
    }

    [Fact]
    public async Task GetIso52016BuildingCoolingLoadReturnsThermalZoneBreakdown()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/v1/buildings/0/load-calculations/cooling-load?method=Iso52016");

        await EnsureSuccessWithBodyAsync(response);

        var result = await response.Content.ReadFromJsonAsync<BuildingCalculationResult>();

        Assert.NotNull(result);
        Assert.Single(result.ThermalZones);
        Assert.Equal("Office zone", result.ThermalZones[0].ThermalZoneName);
        Assert.Equal(1, result.ThermalZones[0].RoomsCount);
    }

    [Fact]
    public async Task GetBuildingCalculationWithUndefinedMethodReturnsBadRequest()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/v1/buildings/0/load-calculations/cooling-load?method=999");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal("Validation failed", problem.Title);
        Assert.Equal("validation_failed", GetExtensionValue(problem, "code"));
        Assert.False(string.IsNullOrWhiteSpace(GetExtensionValue(problem, "correlationId")));
        Assert.Contains("method", problem.Errors.Keys);
    }

    [Fact]
    public async Task GetEnergyBalanceExcelReturnsWorkbook()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/v1/reports/buildings/0/energy-balance/excel?coolingMethod=Simplified&heatingMethod=En12831");

        await EnsureSuccessWithBodyAsync(response);
        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            response.Content.Headers.ContentType?.MediaType);
        var content = await response.Content.ReadAsByteArrayAsync();
        Assert.True(content.Length > 0);
    }

    [Fact]
    public async Task GetCoolingReportExcelReturnsWorkbook()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/v1/reports/buildings/0/cooling/excel?method=Simplified&systemType=Split&unitType=Wall");

        await EnsureSuccessWithBodyAsync(response);
        Assert.Equal(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            response.Content.Headers.ContentType?.MediaType);
        var content = await response.Content.ReadAsByteArrayAsync();
        Assert.True(content.Length > 0);
    }

    [Fact]
    public async Task PostRoomEquipmentSelectionReturnsSelectedCatalogItem()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/v1/rooms/101/equipment-selection?method=Simplified",
            new EquipmentSelectionRequest
            {
                SystemType = "Split",
                UnitType = "Wall"
            });

        await EnsureSuccessWithBodyAsync(response);
        var result = await response.Content.ReadFromJsonAsync<EquipmentSelectionResult>();

        Assert.NotNull(result);
        Assert.Equal(1, result.SelectedCatalogItemId);
        Assert.True(result.EquipmentSelected);
        Assert.True(result.RequiredCoolingCapacityW > 0);
        Assert.True(result.RequiredHeatingCapacityW > 0);
        Assert.NotEmpty(result.AcceptedCandidates);
        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Code == "EquipmentSizing.HeatingCapacityUnavailable");
        Assert.True(result.DesignCapacityKw > 0);
    }

    [Fact]
    public async Task PostEnergyPlusBenchmarkUsesRunnerStubWithoutFileSystemValidation()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();
        var request = new EnergyPlusBenchmarkRequest
        {
            ModelArtifactId = "fake-model.idf",
            WeatherArtifactId = "fake-weather.epw",
            RunName = "fake-output"
        };

        var response = await client.PostAsJsonAsync("/api/v1/benchmarks/energyplus", request);

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<EnergyPlusBenchmarkResult>();
        Assert.NotNull(result);
        Assert.True(result.Succeeded);
        Assert.Equal(0, result.ExitCode);
        Assert.Equal(request.RunName, result.RunArtifactId);
    }

    [Fact]
    public async Task PostEnergyPlusModelExportCreatesIdfFile()
    {
        var tempDirectory = CreateTempDirectory();
        try
        {
            await using var factory = new AssistantEngineerApiFactory();
            var client = factory.CreateClient();
            var response = await client.PostAsJsonAsync(
                "/api/v1/benchmarks/energyplus/buildings/0/model",
                new EnergyPlusModelExportRequest { RunName = "integration-building" });

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<EnergyPlusModelExportResult>();
            Assert.NotNull(result);
            Assert.Equal("Integration building", result.BuildingName);
            Assert.False(string.IsNullOrWhiteSpace(result.ModelArtifactId));

            var artifacts = factory.Services.GetRequiredService<IEnergyPlusArtifactStore>();
            var artifact = artifacts.GetModelArtifact(result.ModelArtifactId);
            Assert.True(artifact.IsSuccess, artifact.Error);
            Assert.True(File.Exists(artifact.Value.FileSystemPath));

            var idf = await File.ReadAllTextAsync(artifact.Value.FileSystemPath);
            Assert.Contains("Office_101", idf);
            Assert.Contains("ZoneHVAC:IdealLoadsAirSystem", idf);
        }
        finally
        {
            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task GetBuildingArchetypesUsesDedicatedResourceRoute()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/building-archetypes");

        response.EnsureSuccessStatusCode();
        var archetypes = await response.Content.ReadFromJsonAsync<PagedResponse<BuildingArchetypeSummary>>();
        Assert.NotNull(archetypes);
        Assert.NotEmpty(archetypes.Items);
        Assert.True(archetypes.TotalCount >= archetypes.Items.Count);
    }

    [Fact]
    public async Task GetBuildingsByProjectSupportsPaginationSearchAndSorting()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/projects/0/buildings?sortBy=name&page=2&pageSize=1");

        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<BuildingResponse>>();
        Assert.NotNull(page);
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(2, page.Page);
        Assert.Equal(1, page.PageSize);
        Assert.Single(page.Items);
        Assert.Equal("Integration building", page.Items[0].Name);

        var searchResponse = await client.GetAsync("/api/v1/projects/0/buildings?search=annex");

        searchResponse.EnsureSuccessStatusCode();
        var searchPage = await searchResponse.Content.ReadFromJsonAsync<PagedResponse<BuildingResponse>>();
        Assert.NotNull(searchPage);
        Assert.Single(searchPage.Items);
        Assert.Equal("Annex building", searchPage.Items[0].Name);
    }

    [Fact]
    public async Task GetThermalZonesByBuildingSupportsSearchAndSorting()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/buildings/1/thermal-zones?search=meeting&sortBy=name&sortDescending=true");

        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<ThermalZoneResponse>>();
        Assert.NotNull(page);
        Assert.Single(page.Items);
        Assert.Equal("Meeting zone", page.Items[0].Name);
    }

    [Fact]
    public async Task GetEquipmentCatalogSupportsFilteringSortingAndMaxPageSize()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/v1/equipment-catalog?systemType=Split&isActive=true&sortBy=nominalCoolingCapacityKw&sortDescending=true&pageSize=500");

        response.EnsureSuccessStatusCode();
        var page = await response.Content.ReadFromJsonAsync<PagedResponse<EquipmentCatalogItemResponse>>();
        Assert.NotNull(page);
        Assert.Equal(500, page.PageSize);
        Assert.Equal(2, page.TotalCount);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal("AeroMax 500", page.Items[0].ModelName);
        Assert.All(page.Items, item =>
        {
            Assert.Equal("Split", item.SystemType);
            Assert.True(item.IsActive);
        });
    }

    [Fact]
    public async Task PostProjectWithInvalidBodyReturnsValidationProblem()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/v1/projects", new { name = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Validation failed", problem.Title);
        Assert.Equal("validation_failed", GetExtensionValue(problem, "code"));
        Assert.False(string.IsNullOrWhiteSpace(GetExtensionValue(problem, "correlationId")));
        Assert.Contains("Name", problem.Errors.Keys);
    }

    [Fact]
    public async Task GetCoolingReportForUnknownBuildingReturnsNotFound()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/v1/reports/buildings/999/cooling?method=Simplified");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal("Not found", problem.Title);
        Assert.Equal("resource_not_found", GetExtensionValue(problem, "code"));
        Assert.False(string.IsNullOrWhiteSpace(GetExtensionValue(problem, "correlationId")));
    }

    [Fact]
    public async Task GetCoolingReportWithPartialEquipmentSelectionReturnsBadRequest()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync(
            "/api/v1/reports/buildings/0/cooling?method=Simplified&systemType=Split");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(problem);
        Assert.Contains("Both systemType and unitType", problem.Detail);
        Assert.False(string.IsNullOrWhiteSpace(GetExtensionValue(problem, "correlationId")));
    }

    [Fact]
    public async Task PostEpwImportWithoutFileReturnsValidationProblemContract()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();
        using var content = new MultipartFormDataContent
        {
            { new StringContent("2020"), "year" }
        };

        var response = await client.PostAsync("/api/v1/climate-zones/0/annual-climate-data/epw", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal("Validation failed", problem.Title);
        Assert.Equal("validation_failed", GetExtensionValue(problem, "code"));
        Assert.False(string.IsNullOrWhiteSpace(GetExtensionValue(problem, "correlationId")));
        Assert.Contains(problem.Errors.Keys, key => string.Equals(key, "sourceFile", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetEngineeringWorkflowStateReturnsDeterministicFoundationState()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/engineering-workflow/0/state?buildingId=0");

        await EnsureSuccessWithBodyAsync(response);
        var state = await response.Content.ReadFromJsonAsync<EngineeringWorkflowStateDto>();

        Assert.NotNull(state);
        Assert.Equal(0, state.ProjectId);
        Assert.Equal(0, state.BuildingId);
        Assert.Contains(state.AvailableModules, item => item == "Reporting");
        Assert.Contains(state.Steps, item => item.Kind == "Validation");
        Assert.NotEmpty(state.Diagnostics);
    }

    [Fact]
    public async Task PostEngineeringWorkflowValidateReturnsDiagnosticsAndStepStatuses()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();
        var stateResponse = await client.GetAsync("/api/v1/engineering-workflow/0/state?buildingId=0");
        stateResponse.EnsureSuccessStatusCode();
        var state = await stateResponse.Content.ReadFromJsonAsync<EngineeringWorkflowStateDto>();
        Assert.NotNull(state);

        var response = await client.PostAsJsonAsync(
            "/api/v1/engineering-workflow/validate",
            new EngineeringWorkflowValidationRequestDto(state));

        await EnsureSuccessWithBodyAsync(response);
        var payload = await response.Content.ReadFromJsonAsync<EngineeringWorkflowValidationResponseDto>();

        Assert.NotNull(payload);
        Assert.NotEmpty(payload.Diagnostics);
        Assert.Contains(payload.Steps, item => item.Kind == "Project");
    }

    [Fact]
    public async Task PostEngineeringWorkflowPrepareCalculationReturnsPreparedOrBlockedWithoutExecution()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();
        var stateResponse = await client.GetAsync("/api/v1/engineering-workflow/0/state?buildingId=0");
        stateResponse.EnsureSuccessStatusCode();
        var state = await stateResponse.Content.ReadFromJsonAsync<EngineeringWorkflowStateDto>();
        Assert.NotNull(state);

        var response = await client.PostAsJsonAsync(
            "/api/v1/engineering-workflow/prepare-calculation",
            new EngineeringWorkflowCalculationPreparationRequestDto(state, ExecuteCalculation: false));

        await EnsureSuccessWithBodyAsync(response);
        var payload = await response.Content.ReadFromJsonAsync<EngineeringWorkflowCalculationPreparationResponseDto>();

        Assert.NotNull(payload);
        Assert.False(payload.Executed);
        Assert.True(payload.Status is "prepared" or "blocked");
        Assert.NotEmpty(payload.RequestPreview);
    }

    [Fact]
    public async Task PostEngineeringWorkflowTracePreviewReturnsCompactTraceSummary()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();
        var stateResponse = await client.GetAsync("/api/v1/engineering-workflow/0/state?buildingId=0");
        stateResponse.EnsureSuccessStatusCode();
        var state = await stateResponse.Content.ReadFromJsonAsync<EngineeringWorkflowStateDto>();
        Assert.NotNull(state);

        var response = await client.PostAsJsonAsync(
            "/api/v1/engineering-workflow/trace-preview",
            new EngineeringWorkflowTracePreviewRequestDto(state, DetailLevel: "Summary"));

        await EnsureSuccessWithBodyAsync(response);
        var payload = await response.Content.ReadFromJsonAsync<EngineeringWorkflowTracePreviewResponseDto>();

        Assert.NotNull(payload);
        Assert.NotNull(payload.TraceDocument);
        Assert.NotEmpty(payload.TraceSummary.Steps);
        Assert.Equal("Summary", payload.TraceSummary.DetailLevel);
    }

    [Fact]
    public async Task PostEngineeringWorkflowReportReturnsReportDocument()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();
        var stateResponse = await client.GetAsync("/api/v1/engineering-workflow/0/state?buildingId=0");
        stateResponse.EnsureSuccessStatusCode();
        var state = await stateResponse.Content.ReadFromJsonAsync<EngineeringWorkflowStateDto>();
        Assert.NotNull(state);

        var response = await client.PostAsJsonAsync(
            "/api/v1/engineering-workflow/report",
            new EngineeringWorkflowReportRequestDto(state));

        await EnsureSuccessWithBodyAsync(response);
        var payload = await response.Content.ReadFromJsonAsync<EngineeringWorkflowReportResponseDto>();

        Assert.NotNull(payload);
        Assert.NotNull(payload.ReportDocument);
        Assert.NotEmpty(payload.Preview.Sections);
        Assert.Contains(payload.Preview.ExportFormatsAvailable, item => item == "Json");
    }

    [Fact]
    public async Task PostEngineeringWorkflowReportExportJsonAndMarkdownReturnContent()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();
        var stateResponse = await client.GetAsync("/api/v1/engineering-workflow/0/state?buildingId=0");
        stateResponse.EnsureSuccessStatusCode();
        var state = await stateResponse.Content.ReadFromJsonAsync<EngineeringWorkflowStateDto>();
        Assert.NotNull(state);

        var jsonResponse = await client.PostAsJsonAsync(
            "/api/v1/engineering-workflow/report/export/json",
            new EngineeringWorkflowReportExportRequestDto(new EngineeringWorkflowReportRequestDto(state)));

        await EnsureSuccessWithBodyAsync(jsonResponse);
        var jsonPayload = await jsonResponse.Content.ReadFromJsonAsync<EngineeringWorkflowReportExportResponseDto>();
        Assert.NotNull(jsonPayload);
        Assert.Equal("Json", jsonPayload.Format);
        Assert.Contains("\"schemaVersion\"", jsonPayload.Content, StringComparison.OrdinalIgnoreCase);

        var markdownResponse = await client.PostAsJsonAsync(
            "/api/v1/engineering-workflow/report/export/markdown",
            new EngineeringWorkflowReportExportRequestDto(new EngineeringWorkflowReportRequestDto(state)));

        await EnsureSuccessWithBodyAsync(markdownResponse);
        var markdownPayload = await markdownResponse.Content.ReadFromJsonAsync<EngineeringWorkflowReportExportResponseDto>();
        Assert.NotNull(markdownPayload);
        Assert.Equal("Markdown", markdownPayload.Format);
        Assert.Contains("#", markdownPayload.Content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PostEngineeringWorkflowRunCalculationReturnsScenarioResult()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();
        var stateResponse = await client.GetAsync("/api/v1/engineering-workflow/0/state?buildingId=0");
        stateResponse.EnsureSuccessStatusCode();
        var state = await stateResponse.Content.ReadFromJsonAsync<EngineeringWorkflowStateDto>();
        Assert.NotNull(state);

        var request = new EngineeringCalculationScenarioRequestDto(
            ScenarioId: "scenario-api-integration",
            ProjectId: state.ProjectId,
            BuildingId: state.BuildingId,
            ScenarioKind: EngineeringCalculationScenarioKind.FullEngineeringCore,
            ExecutionMode: EngineeringCalculationExecutionMode.ExecuteAvailableModules,
            State: state,
            RequestedModules: state.AvailableModules,
            DetailLevel: "Summary",
            IncludeTrace: true,
            IncludeReport: true,
            ReportFormats: ["Json", "Markdown"],
            DeterministicTimestampUtc: null,
            DiagnosticsMode: "Deterministic");

        var response = await client.PostAsJsonAsync("/api/v1/engineering-workflow/run-calculation", request);

        await EnsureSuccessWithBodyAsync(response);
        var payload = await response.Content.ReadFromJsonAsync<EngineeringCalculationScenarioResultDto>();

        Assert.NotNull(payload);
        Assert.Equal("scenario-api-integration", payload.ScenarioId);
        Assert.NotEmpty(payload.ModuleResults);
        Assert.Contains(payload.ModuleResults, item => item.ModuleKind == "ThermalTopology");
        Assert.Equal("InMemory", payload.Metadata["persistenceProvider"]);
        Assert.Equal("false", payload.Metadata["durablePersistenceEnabled"]);
        Assert.True(payload.Status is EngineeringCalculationExecutionStatus.FailedValidation or EngineeringCalculationExecutionStatus.PartiallyExecuted or EngineeringCalculationExecutionStatus.CompletedWithWarnings or EngineeringCalculationExecutionStatus.Completed);
    }

    [Fact]
    public async Task EngineeringCalculationJobSynchronousEndpointReturnsPersistedLifecycleResult()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();
        var stateResponse = await client.GetAsync("/api/v1/engineering-workflow/0/state?buildingId=0");
        stateResponse.EnsureSuccessStatusCode();
        var state = await stateResponse.Content.ReadFromJsonAsync<EngineeringWorkflowStateDto>();
        Assert.NotNull(state);

        var scenarioRequest = new EngineeringCalculationScenarioRequestDto(
            ScenarioId: "scenario-job-sync",
            ProjectId: state.ProjectId,
            BuildingId: state.BuildingId,
            ScenarioKind: EngineeringCalculationScenarioKind.FullEngineeringCore,
            ExecutionMode: EngineeringCalculationExecutionMode.ExecuteAvailableModules,
            State: state,
            RequestedModules: state.AvailableModules,
            DetailLevel: "Summary",
            IncludeTrace: true,
            IncludeReport: true,
            ReportFormats: ["Json", "Markdown"],
            DeterministicTimestampUtc: null,
            DiagnosticsMode: "Deterministic");

        var request = new EngineeringCalculationJobRequestDto(
            JobId: "job-api-sync",
            ProjectId: state.ProjectId,
            ScenarioId: "scenario-job-sync",
            ScenarioRequest: scenarioRequest,
            ExecutionMode: EngineeringCalculationJobExecutionMode.Synchronous,
            RequestedPriority: null,
            IncludeTrace: true,
            IncludeReport: true,
            RequestedReportFormats: ["Json", "Markdown"],
            DeterministicTimestampUtc: null);

        var response = await client.PostAsJsonAsync("/api/v1/engineering-workflow/jobs", request);
        await EnsureSuccessWithBodyAsync(response);
        var payload = await response.Content.ReadFromJsonAsync<EngineeringCalculationJobResultDto>();

        Assert.NotNull(payload);
        Assert.Equal("job-api-sync", payload.JobId);
        Assert.Equal("scenario-job-sync", payload.ScenarioId);
        Assert.NotNull(payload.ScenarioResultSummary);
        Assert.True(payload.Status is EngineeringCalculationJobStatus.Completed or EngineeringCalculationJobStatus.CompletedWithWarnings or EngineeringCalculationJobStatus.FailedValidation);

        var getJobResponse = await client.GetAsync("/api/v1/engineering-workflow/jobs/job-api-sync");
        await EnsureSuccessWithBodyAsync(getJobResponse);
        var persisted = await getJobResponse.Content.ReadFromJsonAsync<EngineeringCalculationJobResultDto>();
        Assert.NotNull(persisted);
        Assert.Equal("job-api-sync", persisted.JobId);
    }

    [Fact]
    public async Task EngineeringCalculationJobQueuedEndpointListsEventsAndSupportsCancel()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();
        var stateResponse = await client.GetAsync("/api/v1/engineering-workflow/0/state?buildingId=0");
        stateResponse.EnsureSuccessStatusCode();
        var state = await stateResponse.Content.ReadFromJsonAsync<EngineeringWorkflowStateDto>();
        Assert.NotNull(state);

        var scenarioRequest = new EngineeringCalculationScenarioRequestDto(
            ScenarioId: "scenario-job-queued",
            ProjectId: state.ProjectId,
            BuildingId: state.BuildingId,
            ScenarioKind: EngineeringCalculationScenarioKind.FullEngineeringCore,
            ExecutionMode: EngineeringCalculationExecutionMode.ExecuteAvailableModules,
            State: state,
            RequestedModules: state.AvailableModules,
            DetailLevel: "Summary",
            IncludeTrace: true,
            IncludeReport: true,
            ReportFormats: ["Json", "Markdown"],
            DeterministicTimestampUtc: null,
            DiagnosticsMode: "Deterministic");

        var request = new EngineeringCalculationJobRequestDto(
            JobId: "job-api-queued",
            ProjectId: state.ProjectId,
            ScenarioId: "scenario-job-queued",
            ScenarioRequest: scenarioRequest,
            ExecutionMode: EngineeringCalculationJobExecutionMode.Queued,
            RequestedPriority: null,
            IncludeTrace: true,
            IncludeReport: true,
            RequestedReportFormats: ["Json", "Markdown"],
            DeterministicTimestampUtc: null);

        var createResponse = await client.PostAsJsonAsync("/api/v1/engineering-workflow/jobs", request);
        await EnsureSuccessWithBodyAsync(createResponse);
        var created = await createResponse.Content.ReadFromJsonAsync<EngineeringCalculationJobResultDto>();
        Assert.NotNull(created);
        Assert.Equal(EngineeringCalculationJobStatus.Queued, created.Status);

        var eventsResponse = await client.GetAsync("/api/v1/engineering-workflow/jobs/job-api-queued/events");
        await EnsureSuccessWithBodyAsync(eventsResponse);
        var events = await eventsResponse.Content.ReadFromJsonAsync<IReadOnlyList<EngineeringCalculationJobEventDto>>();
        Assert.NotNull(events);
        Assert.NotEmpty(events);

        var cancelResponse = await client.PostAsync("/api/v1/engineering-workflow/jobs/job-api-queued/cancel", null);
        await EnsureSuccessWithBodyAsync(cancelResponse);
        var cancelled = await cancelResponse.Content.ReadFromJsonAsync<EngineeringCalculationJobResultDto>();
        Assert.NotNull(cancelled);
        Assert.Equal(EngineeringCalculationJobStatus.Cancelled, cancelled.Status);

        var projectJobsResponse = await client.GetAsync("/api/v1/engineering-workflow/0/jobs");
        await EnsureSuccessWithBodyAsync(projectJobsResponse);
        var jobsPage = await projectJobsResponse.Content.ReadFromJsonAsync<PagedResponse<EngineeringCalculationJobResultDto>>();
        Assert.NotNull(jobsPage);
        Assert.Equal(1, jobsPage.Page);
        Assert.True(jobsPage.PageSize <= 200);
        Assert.Contains(jobsPage.Items, item => item.JobId == "job-api-queued");
    }

    [Fact]
    public async Task EngineeringWorkflowPrepareAndRunPersistScenarioAndArtifacts()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var stateResponse = await client.GetAsync("/api/v1/engineering-workflow/0/state?buildingId=0");
        stateResponse.EnsureSuccessStatusCode();
        var state = await stateResponse.Content.ReadFromJsonAsync<EngineeringWorkflowStateDto>();
        Assert.NotNull(state);

        var prepareResponse = await client.PostAsJsonAsync(
            "/api/v1/engineering-workflow/prepare-calculation",
            new EngineeringWorkflowCalculationPreparationRequestDto(state, ExecuteCalculation: false));
        await EnsureSuccessWithBodyAsync(prepareResponse);
        var preparePayload = await prepareResponse.Content.ReadFromJsonAsync<EngineeringWorkflowCalculationPreparationResponseDto>();
        Assert.NotNull(preparePayload);

        var preparedScenarioResponse = await client.GetAsync($"/api/v1/engineering-workflow/scenarios/{preparePayload.RequestId}");
        await EnsureSuccessWithBodyAsync(preparedScenarioResponse);
        var preparedScenario = await preparedScenarioResponse.Content.ReadFromJsonAsync<EngineeringCalculationScenarioRecordDto>();
        Assert.NotNull(preparedScenario);
        Assert.Equal(preparePayload.RequestId, preparedScenario.ScenarioId);
        Assert.Equal(EngineeringCalculationExecutionMode.PrepareOnly, preparedScenario.ExecutionMode);

        var runRequest = new EngineeringCalculationScenarioRequestDto(
            ScenarioId: "scenario-persistence-integration",
            ProjectId: state.ProjectId,
            BuildingId: state.BuildingId,
            ScenarioKind: EngineeringCalculationScenarioKind.FullEngineeringCore,
            ExecutionMode: EngineeringCalculationExecutionMode.ExecuteAvailableModules,
            State: state,
            RequestedModules: state.AvailableModules,
            DetailLevel: "Summary",
            IncludeTrace: true,
            IncludeReport: true,
            ReportFormats: ["Json", "Markdown"],
            DeterministicTimestampUtc: null,
            DiagnosticsMode: "Deterministic");

        var runResponse = await client.PostAsJsonAsync("/api/v1/engineering-workflow/run-calculation", runRequest);
        await EnsureSuccessWithBodyAsync(runResponse);
        var runPayload = await runResponse.Content.ReadFromJsonAsync<EngineeringCalculationScenarioResultDto>();
        Assert.NotNull(runPayload);

        var persistedRunResponse = await client.GetAsync($"/api/v1/engineering-workflow/scenarios/{runPayload.ScenarioId}");
        await EnsureSuccessWithBodyAsync(persistedRunResponse);
        var persistedRun = await persistedRunResponse.Content.ReadFromJsonAsync<EngineeringCalculationScenarioRecordDto>();
        Assert.NotNull(persistedRun);
        Assert.Equal(runPayload.Status, persistedRun.Status);

        var artifactsResponse = await client.GetAsync($"/api/v1/engineering-workflow/scenarios/{runPayload.ScenarioId}/artifacts");
        await EnsureSuccessWithBodyAsync(artifactsResponse);
        var artifacts = await artifactsResponse.Content.ReadFromJsonAsync<IReadOnlyList<EngineeringCalculationArtifactRecordDto>>();
        Assert.NotNull(artifacts);
        Assert.Contains(artifacts, item => item.ArtifactKind == EngineeringCalculationArtifactKind.ScenarioResultJson);
        Assert.Contains(artifacts, item => item.ArtifactKind == EngineeringCalculationArtifactKind.ValidationDiagnostics);

        var scenarioResultArtifactResponse = await client.GetAsync(
            $"/api/v1/engineering-workflow/scenarios/{runPayload.ScenarioId}/artifacts/ScenarioResultJson");
        await EnsureSuccessWithBodyAsync(scenarioResultArtifactResponse);
        var scenarioResultArtifact = await scenarioResultArtifactResponse.Content.ReadFromJsonAsync<EngineeringCalculationArtifactRecordDto>();
        Assert.NotNull(scenarioResultArtifact);
        Assert.Equal(EngineeringCalculationArtifactKind.ScenarioResultJson, scenarioResultArtifact.ArtifactKind);
        Assert.Contains("\"scenarioId\"", scenarioResultArtifact.Content, StringComparison.OrdinalIgnoreCase);

        var projectScenariosResponse = await client.GetAsync("/api/v1/engineering-workflow/0/scenarios");
        await EnsureSuccessWithBodyAsync(projectScenariosResponse);
        var projectScenariosPage = await projectScenariosResponse.Content.ReadFromJsonAsync<PagedResponse<EngineeringCalculationScenarioRecordDto>>();
        Assert.NotNull(projectScenariosPage);
        Assert.Equal(1, projectScenariosPage.Page);
        Assert.True(projectScenariosPage.PageSize <= 200);
        Assert.Contains(projectScenariosPage.Items, item => item.ScenarioId == runPayload.ScenarioId);
        Assert.Contains(projectScenariosPage.Items, item => item.ScenarioId == preparePayload.RequestId);
    }

    [Fact]
    public async Task EngineeringWorkflowListEndpointsSupportPagingParameters()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var stateResponse = await client.GetAsync("/api/v1/engineering-workflow/0/state?buildingId=0");
        stateResponse.EnsureSuccessStatusCode();
        var state = await stateResponse.Content.ReadFromJsonAsync<EngineeringWorkflowStateDto>();
        Assert.NotNull(state);

        for (var index = 0; index < 3; index++)
        {
            var request = new EngineeringCalculationScenarioRequestDto(
                ScenarioId: $"scenario-page-{index}",
                ProjectId: state.ProjectId,
                BuildingId: state.BuildingId,
                ScenarioKind: EngineeringCalculationScenarioKind.FullEngineeringCore,
                ExecutionMode: EngineeringCalculationExecutionMode.PrepareOnly,
                State: state,
                RequestedModules: state.AvailableModules,
                DetailLevel: "Summary",
                IncludeTrace: false,
                IncludeReport: false,
                ReportFormats: ["Json"],
                DeterministicTimestampUtc: null,
                DiagnosticsMode: "Deterministic");

            var runResponse = await client.PostAsJsonAsync("/api/v1/engineering-workflow/run-calculation", request);
            await EnsureSuccessWithBodyAsync(runResponse);
        }

        var scenariosResponse = await client.GetAsync("/api/v1/engineering-workflow/0/scenarios?page=2&pageSize=1");
        await EnsureSuccessWithBodyAsync(scenariosResponse);
        var scenariosPage = await scenariosResponse.Content.ReadFromJsonAsync<PagedResponse<EngineeringCalculationScenarioRecordDto>>();
        Assert.NotNull(scenariosPage);
        Assert.Equal(2, scenariosPage.Page);
        Assert.Equal(1, scenariosPage.PageSize);
        Assert.True(scenariosPage.TotalCount >= 3);
        Assert.Single(scenariosPage.Items);

        var jobsResponse = await client.GetAsync("/api/v1/engineering-workflow/0/jobs?page=100&pageSize=1");
        await EnsureSuccessWithBodyAsync(jobsResponse);
        var jobsPage = await jobsResponse.Content.ReadFromJsonAsync<PagedResponse<EngineeringCalculationJobResultDto>>();
        Assert.NotNull(jobsPage);
        Assert.Equal(100, jobsPage.Page);
        Assert.Equal(1, jobsPage.PageSize);
        Assert.Empty(jobsPage.Items);

        var cappedResponse = await client.GetAsync("/api/v1/engineering-workflow/0/scenarios?page=1&pageSize=1000");
        await EnsureSuccessWithBodyAsync(cappedResponse);
        var cappedPage = await cappedResponse.Content.ReadFromJsonAsync<PagedResponse<EngineeringCalculationScenarioRecordDto>>();
        Assert.NotNull(cappedPage);
        Assert.Equal(200, cappedPage.PageSize);
    }

    [Fact]
    public async Task EngineeringWorkflowRunCalculationIdempotencyKeyReplaysResultForSameRequestAndConflictsForDifferentPayload()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var stateResponse = await client.GetAsync("/api/v1/engineering-workflow/0/state?buildingId=0");
        stateResponse.EnsureSuccessStatusCode();
        var state = await stateResponse.Content.ReadFromJsonAsync<EngineeringWorkflowStateDto>();
        Assert.NotNull(state);

        var request = new EngineeringCalculationScenarioRequestDto(
            ScenarioId: "scenario-idempotency-1",
            ProjectId: state.ProjectId,
            BuildingId: state.BuildingId,
            ScenarioKind: EngineeringCalculationScenarioKind.FullEngineeringCore,
            ExecutionMode: EngineeringCalculationExecutionMode.ExecuteAvailableModules,
            State: state,
            RequestedModules: state.AvailableModules,
            DetailLevel: "Summary",
            IncludeTrace: false,
            IncludeReport: false,
            ReportFormats: ["Json"],
            DeterministicTimestampUtc: null,
            DiagnosticsMode: "Deterministic");

        using var firstMessage = new HttpRequestMessage(HttpMethod.Post, "/api/v1/engineering-workflow/run-calculation")
        {
            Content = JsonContent.Create(request)
        };
        firstMessage.Headers.Add("Idempotency-Key", "idempotency-run-001");
        var firstResponse = await client.SendAsync(firstMessage);
        await EnsureSuccessWithBodyAsync(firstResponse);
        var firstPayload = await firstResponse.Content.ReadFromJsonAsync<EngineeringCalculationScenarioResultDto>();
        Assert.NotNull(firstPayload);

        using var secondMessage = new HttpRequestMessage(HttpMethod.Post, "/api/v1/engineering-workflow/run-calculation")
        {
            Content = JsonContent.Create(request)
        };
        secondMessage.Headers.Add("Idempotency-Key", "idempotency-run-001");
        var secondResponse = await client.SendAsync(secondMessage);
        await EnsureSuccessWithBodyAsync(secondResponse);
        var secondPayload = await secondResponse.Content.ReadFromJsonAsync<EngineeringCalculationScenarioResultDto>();
        Assert.NotNull(secondPayload);
        Assert.Equal(firstPayload.ScenarioId, secondPayload.ScenarioId);

        var mutatedRequest = request with { DetailLevel = "Detailed" };
        using var conflictMessage = new HttpRequestMessage(HttpMethod.Post, "/api/v1/engineering-workflow/run-calculation")
        {
            Content = JsonContent.Create(mutatedRequest)
        };
        conflictMessage.Headers.Add("Idempotency-Key", "idempotency-run-001");
        var conflictResponse = await client.SendAsync(conflictMessage);
        Assert.Equal(HttpStatusCode.Conflict, conflictResponse.StatusCode);
    }

    [Fact]
    public async Task EngineeringWorkflowScenarioArtifactEndpointReturnsNotFoundForMissingArtifact()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/v1/engineering-workflow/scenarios/unknown-scenario/artifacts/TraceJson");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task EngineeringWorkflowSqliteProviderPersistsScenarioAcrossRequests()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"assistant-engineer-stage12-api-{Guid.NewGuid():N}.db");
        try
        {
            await using var factory = new AssistantEngineerApiFactory(new Dictionary<string, string?>
            {
                ["EngineeringWorkflowPersistence:Provider"] = "SQLite",
                ["EngineeringWorkflowPersistence:EnsureCreatedOnStartup"] = "true",
                ["EngineeringWorkflowPersistence:SqliteConnectionString"] = $"Data Source={dbPath};Cache=Shared;Mode=ReadWriteCreate"
            });
            var client = factory.CreateClient();

            var stateResponse = await client.GetAsync("/api/v1/engineering-workflow/0/state?buildingId=0");
            stateResponse.EnsureSuccessStatusCode();
            var state = await stateResponse.Content.ReadFromJsonAsync<EngineeringWorkflowStateDto>();
            Assert.NotNull(state);

            var request = new EngineeringCalculationScenarioRequestDto(
                ScenarioId: "scenario-sqlite-integration",
                ProjectId: state.ProjectId,
                BuildingId: state.BuildingId,
                ScenarioKind: EngineeringCalculationScenarioKind.FullEngineeringCore,
                ExecutionMode: EngineeringCalculationExecutionMode.ExecuteAvailableModules,
                State: state,
                RequestedModules: state.AvailableModules,
                DetailLevel: "Summary",
                IncludeTrace: true,
                IncludeReport: true,
                ReportFormats: ["Json", "Markdown"],
                DeterministicTimestampUtc: null,
                DiagnosticsMode: "Deterministic");

            var runResponse = await client.PostAsJsonAsync("/api/v1/engineering-workflow/run-calculation", request);
            await EnsureSuccessWithBodyAsync(runResponse);
            var runPayload = await runResponse.Content.ReadFromJsonAsync<EngineeringCalculationScenarioResultDto>();
            Assert.NotNull(runPayload);
            Assert.Equal("SQLite", runPayload.Metadata["persistenceProvider"]);
            Assert.Equal("true", runPayload.Metadata["durablePersistenceEnabled"]);

            var scenarioResponse = await client.GetAsync("/api/v1/engineering-workflow/scenarios/scenario-sqlite-integration");
            await EnsureSuccessWithBodyAsync(scenarioResponse);
            var scenarioRecord = await scenarioResponse.Content.ReadFromJsonAsync<EngineeringCalculationScenarioRecordDto>();
            Assert.NotNull(scenarioRecord);
            Assert.Equal("scenario-sqlite-integration", scenarioRecord.ScenarioId);

            var stateAgainResponse = await client.GetAsync("/api/v1/engineering-workflow/0/state?buildingId=0");
            await EnsureSuccessWithBodyAsync(stateAgainResponse);
            var persistedState = await stateAgainResponse.Content.ReadFromJsonAsync<EngineeringWorkflowStateDto>();
            Assert.NotNull(persistedState);
            Assert.True(persistedState.Metadata.ContainsKey("persistenceProvider"));
            Assert.True(persistedState.Metadata.ContainsKey("durablePersistenceEnabled"));
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                try
                {
                    File.Delete(dbPath);
                }
                catch (IOException)
                {
                    // SQLite file cleanup is best-effort for integration tests.
                }
            }
        }
    }

    [Fact]
    public async Task EngineeringWorkflowSqliteProviderPersistsIdempotencyAcrossFactoryRestart()
    {
        var dbPath = Path.Combine(Path.GetTempPath(), $"assistant-engineer-stage12-idempotency-{Guid.NewGuid():N}.db");
        try
        {
            var overrides = new Dictionary<string, string?>
            {
                ["EngineeringWorkflowPersistence:Provider"] = "SQLite",
                ["EngineeringWorkflowPersistence:EnsureCreatedOnStartup"] = "true",
                ["EngineeringWorkflowPersistence:SqliteConnectionString"] = $"Data Source={dbPath};Cache=Shared;Mode=ReadWriteCreate"
            };

            EngineeringCalculationScenarioRequestDto request;
            string firstScenarioId;

            await using (var firstFactory = new AssistantEngineerApiFactory(overrides))
            {
                var firstClient = firstFactory.CreateClient();
                var stateResponse = await firstClient.GetAsync("/api/v1/engineering-workflow/0/state?buildingId=0");
                stateResponse.EnsureSuccessStatusCode();
                var state = await stateResponse.Content.ReadFromJsonAsync<EngineeringWorkflowStateDto>();
                Assert.NotNull(state);

                request = new EngineeringCalculationScenarioRequestDto(
                    ScenarioId: "scenario-idempotency-sqlite-restart",
                    ProjectId: state.ProjectId,
                    BuildingId: state.BuildingId,
                    ScenarioKind: EngineeringCalculationScenarioKind.FullEngineeringCore,
                    ExecutionMode: EngineeringCalculationExecutionMode.ExecuteAvailableModules,
                    State: state,
                    RequestedModules: state.AvailableModules,
                    DetailLevel: "Summary",
                    IncludeTrace: false,
                    IncludeReport: false,
                    ReportFormats: ["Json"],
                    DeterministicTimestampUtc: null,
                    DiagnosticsMode: "Deterministic");

                using var firstMessage = new HttpRequestMessage(HttpMethod.Post, "/api/v1/engineering-workflow/run-calculation")
                {
                    Content = JsonContent.Create(request)
                };
                firstMessage.Headers.Add("Idempotency-Key", "idempotency-sqlite-restart-001");
                var firstResponse = await firstClient.SendAsync(firstMessage);
                await EnsureSuccessWithBodyAsync(firstResponse);
                var firstPayload = await firstResponse.Content.ReadFromJsonAsync<EngineeringCalculationScenarioResultDto>();
                Assert.NotNull(firstPayload);
                firstScenarioId = firstPayload.ScenarioId;
            }

            await using (var secondFactory = new AssistantEngineerApiFactory(overrides))
            {
                var secondClient = secondFactory.CreateClient();
                using var secondMessage = new HttpRequestMessage(HttpMethod.Post, "/api/v1/engineering-workflow/run-calculation")
                {
                    Content = JsonContent.Create(request)
                };
                secondMessage.Headers.Add("Idempotency-Key", "idempotency-sqlite-restart-001");
                var secondResponse = await secondClient.SendAsync(secondMessage);
                await EnsureSuccessWithBodyAsync(secondResponse);
                var secondPayload = await secondResponse.Content.ReadFromJsonAsync<EngineeringCalculationScenarioResultDto>();
                Assert.NotNull(secondPayload);
                Assert.Equal(firstScenarioId, secondPayload.ScenarioId);
            }
        }
        finally
        {
            if (File.Exists(dbPath))
            {
                try
                {
                    File.Delete(dbPath);
                }
                catch (IOException)
                {
                    // SQLite file cleanup is best-effort for integration tests.
                }
            }
        }
    }

    [Fact]
    public async Task EngineeringCalculationScenarioValidateOnlyModeDoesNotExecuteModules()
    {
        await using var factory = new AssistantEngineerApiFactory();
        var client = factory.CreateClient();
        var stateResponse = await client.GetAsync("/api/v1/engineering-workflow/0/state?buildingId=0");
        stateResponse.EnsureSuccessStatusCode();
        var state = await stateResponse.Content.ReadFromJsonAsync<EngineeringWorkflowStateDto>();
        Assert.NotNull(state);

        var request = new EngineeringCalculationScenarioRequestDto(
            ScenarioId: "scenario-validate-only",
            ProjectId: state.ProjectId,
            BuildingId: state.BuildingId,
            ScenarioKind: EngineeringCalculationScenarioKind.ValidationOnly,
            ExecutionMode: EngineeringCalculationExecutionMode.ValidateOnly,
            State: state,
            RequestedModules: state.AvailableModules,
            DetailLevel: "Summary",
            IncludeTrace: false,
            IncludeReport: false,
            ReportFormats: ["Json"],
            DeterministicTimestampUtc: null,
            DiagnosticsMode: "Deterministic");

        var response = await client.PostAsJsonAsync("/api/v1/engineering-workflow/run-calculation", request);

        await EnsureSuccessWithBodyAsync(response);
        var payload = await response.Content.ReadFromJsonAsync<EngineeringCalculationScenarioResultDto>();

        Assert.NotNull(payload);
        Assert.True(payload.Status is EngineeringCalculationExecutionStatus.Prepared or EngineeringCalculationExecutionStatus.FailedValidation);
        Assert.Empty(payload.ModuleResults);
    }

    private static string? GetExtensionValue(ProblemDetails problem, string key) =>
        problem.Extensions.TryGetValue(key, out var value)
            ? value switch
            {
                string text => text,
                JsonElement json when json.ValueKind == JsonValueKind.String => json.GetString(),
                JsonElement json => json.ToString(),
                _ => value?.ToString()
            }
            : null;

    private static async Task EnsureSuccessWithBodyAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body = await response.Content.ReadAsStringAsync();
        throw new HttpRequestException(
            $"Response status code does not indicate success: {(int)response.StatusCode} ({response.ReasonPhrase}). Body: {body}",
            inner: null,
            response.StatusCode);
    }

    private sealed class AssistantEngineerApiFactory : WebApplicationFactory<Program>
    {
        private readonly Building _building;
        private readonly IReadOnlyList<Building> _buildings;
        private readonly IReadOnlyList<CoolingEquipmentCatalogItem> _equipmentCatalogItems;
        private readonly IReadOnlyList<ClimateData> _climateData;
        private readonly IReadOnlyDictionary<string, string?> _configurationOverrides;

        public AssistantEngineerApiFactory(IReadOnlyDictionary<string, string?>? configurationOverrides = null)
        {
            _configurationOverrides = configurationOverrides ?? new Dictionary<string, string?>();

            var project = DomainInvariantTests.CreateProject("Integration project");
            var climateZone = ClimateZone.Create(
                "Integration climate",
                Temperature.FromCelsius(35).Value,
                Temperature.FromCelsius(-12).Value).Value;
            _building = Building.Create("Integration building", project, climateZone).Value;
            SetEntityId(_building, 0);
            Assert.True(project.AddBuilding(_building).IsSuccess);

            var floor = _building.AddFloor("Level 1").Value;
            var room = floor.AddRoom(
                "Office 101",
                Area.FromSquareMeters(20).Value,
                3,
                Temperature.FromCelsius(22).Value,
                Temperature.FromCelsius(34).Value,
                peopleCount: 2,
                equipmentLoad: Power.FromWatts(400).Value,
                lightingLoad: Power.FromWatts(200).Value).Value;
            SetEntityId(room, 101);
            Assert.True(room.AddWall(
                Area.FromSquareMeters(12).Value,
                isExternal: true,
                ThermalTransmittance.FromValue(1.2).Value,
                CardinalDirection.South).IsSuccess);
            Assert.True(_building.AddThermalZone("Office zone", [room]).IsSuccess);
            Assert.True(room.AddWindow(
                Area.FromSquareMeters(3).Value,
                ThermalTransmittance.FromValue(2).Value,
                SolarHeatGainCoefficient.FromValue(0.5).Value,
                CardinalDirection.South).IsSuccess);
            var officeZone = _building.ThermalZones.Single();
            SetEntityId(officeZone, 100);

            var annexClimateZone = ClimateZone.Create(
                "Annex climate",
                Temperature.FromCelsius(33).Value,
                Temperature.FromCelsius(-8).Value).Value;
            var annexBuilding = Building.Create("Annex building", project, annexClimateZone).Value;
            SetEntityId(annexBuilding, 1);
            Assert.True(project.AddBuilding(annexBuilding).IsSuccess);
            var annexFloor = annexBuilding.AddFloor("Annex level").Value;
            var meetingRoom = annexFloor.AddRoom(
                "Meeting 201",
                Area.FromSquareMeters(16).Value,
                3,
                Temperature.FromCelsius(22).Value,
                Temperature.FromCelsius(32).Value,
                type: RoomType.MeetingRoom).Value;
            var supportRoom = annexFloor.AddRoom(
                "Support 202",
                Area.FromSquareMeters(12).Value,
                3,
                Temperature.FromCelsius(21).Value,
                Temperature.FromCelsius(32).Value,
                type: RoomType.Corridor).Value;
            SetEntityId(meetingRoom, 201);
            SetEntityId(supportRoom, 202);
            Assert.True(annexBuilding.AddThermalZone("Meeting zone", [meetingRoom]).IsSuccess);
            Assert.True(annexBuilding.AddThermalZone("Support zone", [supportRoom]).IsSuccess);
            SetEntityId(annexBuilding.ThermalZones.First(zone => zone.Name == "Meeting zone"), 301);
            SetEntityId(annexBuilding.ThermalZones.First(zone => zone.Name == "Support zone"), 302);

            _buildings = [_building, annexBuilding];
            _equipmentCatalogItems =
            [
                CreateCatalogItem(1, "Aero", "Split", "Wall", "AeroMax 500", 5.0, isActive: true),
                CreateCatalogItem(2, "Aero", "Split", "Cassette", "AeroLite 350", 3.5, isActive: true),
                CreateCatalogItem(3, "Ventis", "VRF", "Ducted", "Ventis Pro", 7.2, isActive: false)
            ];

            _climateData =
            [
                CreateClimateData(climateZone, month: 1),
                CreateClimateData(climateZone, month: 7)
            ];
        }

        private static void SetEntityId(object entity, int id)
        {
            var field = entity.GetType().GetField("<Id>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            field.SetValue(entity, id);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration(configuration =>
            {
                var values = new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=AssistantEngineerTests;Username=postgres"
                };

                foreach (var overrideItem in _configurationOverrides)
                {
                    values[overrideItem.Key] = overrideItem.Value;
                }

                configuration.AddInMemoryCollection(values);
            });

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IBuildingRepository>();
                services.RemoveAll<IFloorRepository>();
                services.RemoveAll<IRoomRepository>();
                services.RemoveAll<IBuildingHeatingReadModelRepository>();
                services.RemoveAll<ICalculationPreferencesRepository>();
                services.RemoveAll<IEquipmentCatalogRepository>();
                services.RemoveAll<IClimateDataRepository>();
                services.RemoveAll<IEnergyPlusBenchmarkRunner>();

                services.AddScoped<IBuildingRepository>(_ => new BuildingRepositoryStub(_buildings));
                services.AddScoped<IFloorRepository>(_ => new FloorRepositoryStub(
                    _buildings.SelectMany(building => building.Floors).ToArray()));
                services.AddScoped<IRoomRepository>(_ => new RoomRepositoryStub(
                    _buildings.SelectMany(building => building.Floors).SelectMany(floor => floor.Rooms).ToArray()));
                services.AddScoped<IBuildingHeatingReadModelRepository>(_ => new BuildingHeatingReadModelRepositoryStub(_building));
                services.AddScoped<ICalculationPreferencesRepository, EmptyPreferencesRepository>();
                services.AddScoped<IEquipmentCatalogRepository>(_ => new EquipmentCatalogRepositoryStub(_equipmentCatalogItems));
                services.AddScoped<IClimateDataRepository>(_ => new ClimateDataRepositoryStub(_climateData));
                services.AddScoped<IEnergyPlusBenchmarkRunner, EnergyPlusBenchmarkRunnerStub>();
            });
        }

        private static ClimateData CreateClimateData(ClimateZone climateZone, int month)
        {
            var climateData = ClimateData.Create(climateZone, month, dayOfMonth: 15, dailyTemperatureRange: 10).Value;
            for (var hour = 0; hour < 24; hour++)
            {
                Assert.True(climateData.AddHourlyData(
                    hour,
                    dryBulbTemp: 30,
                    directSolar: 100,
                    diffuseSolar: 20).IsSuccess);
            }

            return climateData;
        }

        private static CoolingEquipmentCatalogItem CreateCatalogItem(
            int id,
            string manufacturer,
            string systemType,
            string unitType,
            string modelName,
            double capacityKw,
            bool isActive)
        {
            var item = CoolingEquipmentCatalogItem.Create(
                manufacturer,
                systemType,
                unitType,
                modelName,
                Power.FromWatts(capacityKw * 1000).Value,
                isActive).Value;
            SetEntityId(item, id);
            return item;
        }
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"assistant-engineer-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private sealed class BuildingRepositoryStub : IBuildingRepository
    {
        private readonly IReadOnlyList<Building> _buildings;

        public BuildingRepositoryStub(IReadOnlyList<Building> buildings) => _buildings = buildings;

        public Task<Building?> GetByIdAsync(
            int id,
            bool includeClimateZone = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_buildings.FirstOrDefault(building => building.Id == id));

        public Task<Building?> GetWithFloorsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_buildings.FirstOrDefault(building => building.Id == id));

        public Task<Building?> GetWithThermalZonesAndRoomsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_buildings.FirstOrDefault(building => building.Id == id));

        public Task<Building?> GetByThermalZoneIdAsync(int thermalZoneId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_buildings.FirstOrDefault(
                building => building.ThermalZones.Any(zone => zone.Id == thermalZoneId)));

        public Task<Building?> GetForCalculationAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_buildings.FirstOrDefault(building => building.Id == id));

        public Task<Building?> GetForReportAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_buildings.FirstOrDefault(building => building.Id == id));

        public Task<Building?> GetForValidationAsync(
            int id,
            bool asTracking = false,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_buildings.FirstOrDefault(building => building.Id == id));

        public Task<IReadOnlyList<Building>> ListByProjectIdAsync(
            int projectId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Building>>(
                _buildings.Where(building => building.ProjectId == projectId).ToArray());

        public void Add(Building building) => throw new NotSupportedException();

        public void Remove(Building building) => throw new NotSupportedException();
    }

    private sealed class FloorRepositoryStub : IFloorRepository
    {
        private readonly IReadOnlyList<Floor> _floors;

        public FloorRepositoryStub(IReadOnlyList<Floor> floors) => _floors = floors;

        public Task<Floor?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_floors.FirstOrDefault(floor => floor.Id == id));

        public Task<Floor?> GetWithRoomsAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_floors.FirstOrDefault(floor => floor.Id == id));

        public Task<Floor?> GetForCalculationAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_floors.FirstOrDefault(floor => floor.Id == id));

        public Task<IReadOnlyList<Floor>> ListByBuildingIdAsync(
            int buildingId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Floor>>(
                _floors.Where(floor => floor.BuildingId == buildingId).ToArray());

        public void Add(Floor floor) => throw new NotSupportedException();

        public void Remove(Floor floor) => throw new NotSupportedException();
    }

    private sealed class RoomRepositoryStub : IRoomRepository
    {
        private readonly IReadOnlyList<Room> _rooms;

        public RoomRepositoryStub(IReadOnlyList<Room> rooms) => _rooms = rooms;

        public Task<Room?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_rooms.FirstOrDefault(room => room.Id == id));

        public Task<Room?> GetForCalculationAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_rooms.FirstOrDefault(room => room.Id == id));

        public Task<Room?> GetWithWindowsAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_rooms.FirstOrDefault(room => room.Id == id));

        public Task<Room?> GetWithWallsAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_rooms.FirstOrDefault(room => room.Id == id));

        public Task<Room?> GetWithWindowsAndWallsAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_rooms.FirstOrDefault(room => room.Id == id));

        public Task<Room?> GetWithVentilationAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_rooms.FirstOrDefault(room => room.Id == id));

        public Task<IReadOnlyList<Room>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_rooms);

        public Task<IReadOnlyList<Room>> ListByBuildingIdAsync(
            int buildingId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Room>>(
                _rooms.Where(room => room.Floor.BuildingId == buildingId).ToArray());

        public Task<IReadOnlyList<Room>> ListWithEngineeringInputsByBuildingIdAsync(
            int buildingId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Room>>(
                _rooms.Where(room => room.Floor.BuildingId == buildingId).ToArray());

        public Task<bool> ExistsAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_rooms.Any(room => room.Id == id));

        public Task<IReadOnlyList<Window>> ListWindowsAsync(
            int roomId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Window>>(
                _rooms.FirstOrDefault(room => room.Id == roomId)?.Windows.ToArray() ?? []);

        public Task<IReadOnlyList<Wall>> ListWallsAsync(
            int roomId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Wall>>(
                _rooms.FirstOrDefault(room => room.Id == roomId)?.Walls.ToArray() ?? []);

        public void Add(Room room) => throw new NotSupportedException();

        public void Remove(Room room) => throw new NotSupportedException();

        public void RemoveWindow(Window window) => throw new NotSupportedException();

        public void RemoveWall(Wall wall) => throw new NotSupportedException();
    }

    private sealed class EmptyPreferencesRepository : ICalculationPreferencesRepository
    {
        public Task<CalculationPreferences?> GetByProjectIdAsync(
            int projectId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<CalculationPreferences?>(null);
    }

    private sealed class EquipmentCatalogRepositoryStub : IEquipmentCatalogRepository
    {
        private readonly IReadOnlyList<CoolingEquipmentCatalogItem> _items;

        public EquipmentCatalogRepositoryStub(IReadOnlyList<CoolingEquipmentCatalogItem> items)
        {
            _items = items;
        }

        public Task<IReadOnlyList<CoolingEquipmentCatalogItem>> ListActiveByTypeAsync(
            string systemType,
            string unitType,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<CoolingEquipmentCatalogItem>>(
                _items.Where(item => item.IsActive && item.SystemType == systemType && item.UnitType == unitType).ToArray());

        public Task<CoolingEquipmentCatalogItem?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items.FirstOrDefault(item => item.Id == id));

        public Task<IReadOnlyList<CoolingEquipmentCatalogItem>> ListAsync(
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_items);

        public void Add(CoolingEquipmentCatalogItem item) => throw new NotSupportedException();
    }

    private sealed class BuildingHeatingReadModelRepositoryStub : IBuildingHeatingReadModelRepository
    {
        private readonly BuildingHeatingReadModel _building;

        public BuildingHeatingReadModelRepositoryStub(Building building)
        {
            _building = new BuildingHeatingReadModel(
                building.Id,
                building.Name,
                building.ProjectId,
                building.Project.Name,
                building.ClimateZone?.WinterDesignTemperature.Celsius,
                building.Floors
                    .SelectMany(floor => floor.Rooms)
                    .Select(room => new RoomHeatingReadModel(
                        room.Id,
                        room.Name,
                        room.Area.SquareMeters,
                        room.HeightM,
                        room.IndoorTemperature.Celsius,
                        room.OutdoorTemperatureOverride?.Celsius,
                        room.VentilationParameters is null
                            ? null
                            : new HeatingVentilationReadModel(
                                room.VentilationParameters.AirChangesPerHour,
                                room.VentilationParameters.HeatRecoveryEfficiency,
                                room.VentilationParameters.InfiltrationAirChangesPerHour,
                                room.VentilationParameters.StackCoefficient),
                        room.Windows
                            .Select(window => new WindowHeatingReadModel(
                                window.Area.SquareMeters,
                                window.UValue.Value))
                            .ToList(),
                        room.Walls
                            .Select(wall => new WallHeatingReadModel(
                                wall.Area.SquareMeters,
                                wall.IsExternal,
                                wall.UValue.Value,
                                wall.ConstructionAssembly?.Layers
                                    .Select(layer => new ConstructionLayerHeatingReadModel(
                                        layer.ThicknessM,
                                        layer.Material.ThermalConductivityWPerMK))
                                    .ToList() ??
                                []))
                            .ToList()))
                    .ToList());
        }

        public Task<BuildingHeatingReadModel?> GetByIdAsync(int buildingId, CancellationToken cancellationToken = default) =>
            Task.FromResult<BuildingHeatingReadModel?>(buildingId == _building.BuildingId ? _building : null);
    }

    private sealed class ClimateDataRepositoryStub : IClimateDataRepository
    {
        private readonly IReadOnlyList<ClimateData> _climateData;

        public ClimateDataRepositoryStub(IReadOnlyList<ClimateData> climateData) => _climateData = climateData;

        public Task<ClimateData?> GetForClimateZoneAsync(
            int climateZoneId,
            int month,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(_climateData.FirstOrDefault(data =>
                climateZoneId == data.ClimateZoneId && month == data.Month));

        public Task<IReadOnlyList<int>> GetAvailableMonthsForClimateZoneAsync(
            int climateZoneId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<int>>(
                _climateData
                    .Where(data => data.ClimateZoneId == climateZoneId)
                    .Select(data => data.Month)
                    .OrderBy(month => month)
                    .ToArray());
    }

    private sealed class EnergyPlusBenchmarkRunnerStub : IEnergyPlusBenchmarkRunner
    {
        public Task<Result<EnergyPlusBenchmarkResult>> RunAsync(
            EnergyPlusBenchmarkRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Result<EnergyPlusBenchmarkResult>.Success(new EnergyPlusBenchmarkResult
            {
                Succeeded = true,
                ExitCode = 0,
                RunArtifactId = request.RunName ?? "run-artifact",
                StandardOutput = "EnergyPlus validation completed.",
                StandardError = string.Empty
            }));
    }
}

