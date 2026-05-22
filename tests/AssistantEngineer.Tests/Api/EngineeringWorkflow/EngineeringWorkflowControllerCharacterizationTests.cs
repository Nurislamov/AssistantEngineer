using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AssistantEngineer.Api.Contracts.Common;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;

namespace AssistantEngineer.Tests.Api.EngineeringWorkflow;

public sealed class EngineeringWorkflowControllerCharacterizationTests
{
    [Fact]
    public async Task ExecutionEndpoints_CurrentStatusAndShapeAreCharacterized()
    {
        await using var factory = new EngineeringWorkflowControllerCharacterizationFactory(
            new EngineeringWorkflowControllerCharacterizationOptions());
        var client = factory.CreateClient();

        var state = await GetStateAsync(client);

        var prepareResponse = await client.PostAsJsonAsync(
            "/api/v1/engineering-workflow/prepare-calculation",
            new EngineeringWorkflowCalculationPreparationRequestDto(state, ExecuteCalculation: false));
        Assert.Equal(HttpStatusCode.OK, prepareResponse.StatusCode);
        var preparePayload = await prepareResponse.Content.ReadFromJsonAsync<EngineeringWorkflowCalculationPreparationResponseDto>();
        Assert.NotNull(preparePayload);
        Assert.True(preparePayload.Status is "prepared" or "blocked");
        Assert.False(preparePayload.Executed);
        Assert.NotEmpty(preparePayload.RequestPreview);

        var runRequest = new EngineeringCalculationScenarioRequestDto(
            ScenarioId: "p8-03d-run",
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

        var runResponse = await client.PostAsJsonAsync("/api/v1/engineering-workflow/run-calculation", runRequest);
        Assert.Equal(HttpStatusCode.OK, runResponse.StatusCode);
        var runPayload = await runResponse.Content.ReadFromJsonAsync<EngineeringCalculationScenarioResultDto>();
        Assert.NotNull(runPayload);
        Assert.Equal("p8-03d-run", runPayload.ScenarioId);

        var jobRequest = new EngineeringCalculationJobRequestDto(
            JobId: "p8-03d-job",
            ProjectId: state.ProjectId,
            ScenarioId: "p8-03d-job-scenario",
            ScenarioRequest: runRequest with { ScenarioId = "p8-03d-job-scenario" },
            ExecutionMode: EngineeringCalculationJobExecutionMode.Synchronous);
        var jobResponse = await client.PostAsJsonAsync("/api/v1/engineering-workflow/jobs", jobRequest);
        Assert.Equal(HttpStatusCode.OK, jobResponse.StatusCode);
        var jobPayload = await jobResponse.Content.ReadFromJsonAsync<EngineeringCalculationJobResultDto>();
        Assert.NotNull(jobPayload);
        Assert.Equal("p8-03d-job", jobPayload.JobId);

        var cancelResponse = await client.PostAsync("/api/v1/engineering-workflow/jobs/non-existent-job-id/cancel", null);
        Assert.Equal(HttpStatusCode.NotFound, cancelResponse.StatusCode);
        using var cancelBody = JsonDocument.Parse(await cancelResponse.Content.ReadAsStringAsync());
        Assert.Equal("CALCULATION_JOB_NOT_FOUND", cancelBody.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task ReadHistoryEndpoints_CurrentStatusAndShapeAreCharacterized()
    {
        await using var factory = new EngineeringWorkflowControllerCharacterizationFactory(
            new EngineeringWorkflowControllerCharacterizationOptions());
        var client = factory.CreateClient();

        var stateResponse = await client.GetAsync("/api/v1/engineering-workflow/1/state?buildingId=11");
        Assert.Equal(HttpStatusCode.OK, stateResponse.StatusCode);
        var state = await stateResponse.Content.ReadFromJsonAsync<EngineeringWorkflowStateDto>();
        Assert.NotNull(state);
        Assert.Equal(1, state.ProjectId);
        Assert.Equal(11, state.BuildingId);

        var missingScenario = await client.GetAsync("/api/v1/engineering-workflow/scenarios/non-existent-scenario-id");
        Assert.Equal(HttpStatusCode.NotFound, missingScenario.StatusCode);
        using var scenarioBody = JsonDocument.Parse(await missingScenario.Content.ReadAsStringAsync());
        Assert.Equal("WORKFLOW_SCENARIO_NOT_FOUND", scenarioBody.RootElement.GetProperty("code").GetString());

        var missingJob = await client.GetAsync("/api/v1/engineering-workflow/jobs/non-existent-job-id");
        Assert.Equal(HttpStatusCode.NotFound, missingJob.StatusCode);
        using var jobBody = JsonDocument.Parse(await missingJob.Content.ReadAsStringAsync());
        Assert.Equal("CALCULATION_JOB_NOT_FOUND", jobBody.RootElement.GetProperty("code").GetString());

        var listScenariosResponse = await client.GetAsync("/api/v1/engineering-workflow/1/scenarios?page=1&pageSize=5");
        Assert.Equal(HttpStatusCode.OK, listScenariosResponse.StatusCode);
        var scenariosPage = await listScenariosResponse.Content.ReadFromJsonAsync<PagedResponse<EngineeringCalculationScenarioRecordDto>>();
        Assert.NotNull(scenariosPage);
        Assert.Equal(1, scenariosPage.Page);
        Assert.Equal(5, scenariosPage.PageSize);

        var listJobsResponse = await client.GetAsync("/api/v1/engineering-workflow/1/jobs?page=1&pageSize=5");
        Assert.Equal(HttpStatusCode.OK, listJobsResponse.StatusCode);
        var jobsPage = await listJobsResponse.Content.ReadFromJsonAsync<PagedResponse<EngineeringCalculationJobResultDto>>();
        Assert.NotNull(jobsPage);
        Assert.Equal(1, jobsPage.Page);
        Assert.Equal(5, jobsPage.PageSize);
    }

    [Fact]
    public async Task ReportAndArtifactEndpoints_CurrentStatusAndShapeAreCharacterized()
    {
        await using var factory = new EngineeringWorkflowControllerCharacterizationFactory(
            new EngineeringWorkflowControllerCharacterizationOptions());
        var client = factory.CreateClient();
        var state = await GetStateAsync(client);

        var traceResponse = await client.PostAsJsonAsync(
            "/api/v1/engineering-workflow/trace-preview",
            new EngineeringWorkflowTracePreviewRequestDto(state, DetailLevel: "Summary"));
        Assert.Equal(HttpStatusCode.OK, traceResponse.StatusCode);
        var tracePayload = await traceResponse.Content.ReadFromJsonAsync<EngineeringWorkflowTracePreviewResponseDto>();
        Assert.NotNull(tracePayload);
        Assert.NotNull(tracePayload.TraceSummary);
        Assert.NotNull(tracePayload.TraceDocument);

        var reportResponse = await client.PostAsJsonAsync(
            "/api/v1/engineering-workflow/report",
            new EngineeringWorkflowReportRequestDto(state));
        Assert.Equal(HttpStatusCode.OK, reportResponse.StatusCode);
        var reportPayload = await reportResponse.Content.ReadFromJsonAsync<EngineeringWorkflowReportResponseDto>();
        Assert.NotNull(reportPayload);
        Assert.NotNull(reportPayload.ReportDocument);
        Assert.NotNull(reportPayload.Preview);

        var exportJsonResponse = await client.PostAsJsonAsync(
            "/api/v1/engineering-workflow/report/export/json",
            new EngineeringWorkflowReportExportRequestDto(new EngineeringWorkflowReportRequestDto(state)));
        Assert.Equal(HttpStatusCode.OK, exportJsonResponse.StatusCode);
        var exportJsonPayload = await exportJsonResponse.Content.ReadFromJsonAsync<EngineeringWorkflowReportExportResponseDto>();
        Assert.NotNull(exportJsonPayload);
        Assert.Equal("Json", exportJsonPayload.Format);
        Assert.Contains("{", exportJsonPayload.Content, StringComparison.Ordinal);

        var exportMarkdownResponse = await client.PostAsJsonAsync(
            "/api/v1/engineering-workflow/report/export/markdown",
            new EngineeringWorkflowReportExportRequestDto(new EngineeringWorkflowReportRequestDto(state)));
        Assert.Equal(HttpStatusCode.OK, exportMarkdownResponse.StatusCode);
        var exportMarkdownPayload = await exportMarkdownResponse.Content.ReadFromJsonAsync<EngineeringWorkflowReportExportResponseDto>();
        Assert.NotNull(exportMarkdownPayload);
        Assert.Equal("Markdown", exportMarkdownPayload.Format);
        Assert.False(string.IsNullOrWhiteSpace(exportMarkdownPayload.Content));

        var artifactsResponse = await client.GetAsync("/api/v1/engineering-workflow/scenarios/non-existent/artifacts");
        Assert.Equal(HttpStatusCode.OK, artifactsResponse.StatusCode);
        var artifactsPayload = await artifactsResponse.Content.ReadFromJsonAsync<IReadOnlyList<EngineeringCalculationArtifactRecordDto>>();
        Assert.NotNull(artifactsPayload);
        Assert.Empty(artifactsPayload);

        var invalidKindResponse = await client.GetAsync("/api/v1/engineering-workflow/scenarios/non-existent/artifacts/invalid-kind");
        Assert.Equal(HttpStatusCode.BadRequest, invalidKindResponse.StatusCode);
        using var invalidKindBody = JsonDocument.Parse(await invalidKindResponse.Content.ReadAsStringAsync());
        Assert.Equal("WORKFLOW_ARTIFACT_KIND_INVALID", invalidKindBody.RootElement.GetProperty("code").GetString());

        var missingArtifactResponse = await client.GetAsync("/api/v1/engineering-workflow/scenarios/non-existent/artifacts/TraceJson");
        Assert.Equal(HttpStatusCode.NotFound, missingArtifactResponse.StatusCode);
        using var missingArtifactBody = JsonDocument.Parse(await missingArtifactResponse.Content.ReadAsStringAsync());
        Assert.Equal("WORKFLOW_ARTIFACT_NOT_FOUND", missingArtifactBody.RootElement.GetProperty("code").GetString());
    }

    private static async Task<EngineeringWorkflowStateDto> GetStateAsync(HttpClient client)
    {
        var response = await client.GetAsync("/api/v1/engineering-workflow/1/state?buildingId=11");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var state = await response.Content.ReadFromJsonAsync<EngineeringWorkflowStateDto>();
        Assert.NotNull(state);
        return state;
    }
}
