using AssistantEngineer.Api.Security.TenantIsolation;
using AssistantEngineer.Api.Services.Calculations;
using AssistantEngineer.Api.Services.Calculations.Persistence;
using AssistantEngineer.Api.Security.Authorization;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;
using AssistantEngineer.Modules.Buildings.Application.Abstractions.Repositories;
using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Identity.Application.Contracts.Access;
using AssistantEngineer.Modules.Identity.Application.Services.Access;
using AssistantEngineer.Modules.Identity.Domain.Enums;

namespace AssistantEngineer.Tests.Api.Security.TenantIsolation;

public sealed class TenantScopedReadServiceMatrixTests
{
    [Fact]
    public async Task ProjectTenantScopedReadService_Matrix()
    {
        var tenantAProject = CreateProject(30, "Matrix tenant A project", TenantIsolationScenario.TenantAOrganizationId);
        var tenantBProject = CreateProject(31, "Matrix tenant B project", TenantIsolationScenario.TenantBOrganizationId);
        var service = new ProjectTenantScopedReadService(
            new StubProjectRepository([tenantAProject, tenantBProject]),
            new TenantQueryIsolationPolicy());

        var sameTenant = await service.GetProjectForTenantAsync(tenantAProject.Id, ProjectReadContext(), CancellationToken.None);
        var crossTenant = await service.GetProjectForTenantAsync(tenantBProject.Id, ProjectReadContext(), CancellationToken.None);
        var list = await service.ListProjectsForTenantAsync(ProjectReadContext(), CancellationToken.None);

        Assert.True(sameTenant.IsSuccess);
        Assert.True(crossTenant.IsFailure);
        Assert.True(list.IsSuccess);
        Assert.DoesNotContain(list.Value, project => project.OrganizationId == TenantIsolationScenario.TenantBOrganizationId);
    }

    [Fact]
    public async Task BuildingTenantScopedReadService_Matrix()
    {
        var (tenantAProject, tenantABuilding) = CreateBuilding(40, 32, "Matrix tenant A building project", "Matrix tenant A building", TenantIsolationScenario.TenantAOrganizationId);
        var (tenantBProject, tenantBBuilding) = CreateBuilding(41, 33, "Matrix tenant B building project", "Matrix tenant B building", TenantIsolationScenario.TenantBOrganizationId);
        var service = new BuildingTenantScopedReadService(
            new StubBuildingRepository([tenantABuilding, tenantBBuilding]),
            new StubProjectRepository([tenantAProject, tenantBProject]),
            new TenantQueryIsolationPolicy());

        var sameTenant = await service.GetBuildingForTenantAsync(tenantABuilding.Id, BuildingReadContext(), CancellationToken.None);
        var crossTenant = await service.GetBuildingForTenantAsync(tenantBBuilding.Id, BuildingReadContext(), CancellationToken.None);

        Assert.True(sameTenant.IsSuccess);
        Assert.True(crossTenant.IsFailure);
    }

    [Fact]
    public async Task UnscopedTransitionResourcesRemainExplicitCompatibilityBehavior()
    {
        var unscopedProject = CreateProject(34, "Matrix transition project", organizationId: null);
        var service = new ProjectTenantScopedReadService(
            new StubProjectRepository([unscopedProject]),
            new TenantQueryIsolationPolicy());

        var allowed = await service.GetProjectForTenantAsync(
            unscopedProject.Id,
            ProjectReadContext(allowUnscopedResourcesDuringTransition: true),
            CancellationToken.None);
        var denied = await service.GetProjectForTenantAsync(
            unscopedProject.Id,
            ProjectReadContext(allowUnscopedResourcesDuringTransition: false),
            CancellationToken.None);

        Assert.True(allowed.IsSuccess);
        Assert.True(denied.IsFailure);
    }

    [Fact]
    public async Task WorkflowTenantScopedReadService_Matrix()
    {
        var scenarioId = "matrix-scenario-a";
        var service = new WorkflowTenantScopedReadService(
            new MatrixWorkflowPersistenceService(
                states: new Dictionary<int, EngineeringWorkflowStateDto?>
                {
                    [TenantAProjectId] = CreateState(TenantAProjectId)
                },
                scenarios: new Dictionary<string, EngineeringCalculationScenarioRecordDto?>
                {
                    [scenarioId] = CreateScenario(scenarioId, TenantAProjectId)
                }),
            new MatrixJobService(
                jobs: new Dictionary<string, EngineeringCalculationJobResultDto?>
                {
                    [JobId] = CreateJob(JobId, TenantAProjectId, scenarioId)
                }),
            new MatrixProjectScopeResolver(new Dictionary<int, ProjectAccessScope?>
            {
                [TenantAProjectId] = CreateProjectScope(TenantAProjectId, TenantIsolationScenario.TenantAOrganizationId),
                [TenantBProjectId] = CreateProjectScope(TenantBProjectId, TenantIsolationScenario.TenantBOrganizationId)
            }),
            new MatrixWorkflowScopeResolver(
                scenarioScopes: new Dictionary<string, WorkflowAccessScope?>
                {
                    [scenarioId] = CreateWorkflowScope(scenarioId, TenantAProjectId, TenantIsolationScenario.TenantAOrganizationId)
                },
                jobScopes: new Dictionary<string, WorkflowAccessScope?>
                {
                    [JobId] = CreateWorkflowScope(JobId, TenantAProjectId, TenantIsolationScenario.TenantAOrganizationId)
                }),
            new TenantQueryIsolationPolicy());

        var sameTenantState = await service.GetWorkflowStateForTenantAsync(TenantAProjectId, null, WorkflowReadContext(), CancellationToken.None);
        var crossTenantState = await service.GetWorkflowStateForTenantAsync(TenantBProjectId, null, WorkflowReadContext(), CancellationToken.None);
        var scenario = await service.GetScenarioForTenantAsync(scenarioId, WorkflowReadContext(), CancellationToken.None);
        var job = await service.GetJobForTenantAsync(JobId, WorkflowReadContext(), CancellationToken.None);
        var jobEvents = await service.GetJobEventsForTenantAsync(JobId, WorkflowReadContext(), CancellationToken.None);
        var scenariosForProject = await service.ListScenariosForProjectForTenantAsync(TenantAProjectId, WorkflowReadContext(), CancellationToken.None);
        var jobsForProject = await service.ListJobsForProjectForTenantAsync(TenantAProjectId, WorkflowReadContext(), CancellationToken.None);

        Assert.True(sameTenantState.IsSuccess);
        Assert.True(crossTenantState.IsFailure);
        Assert.Equal(TenantQueryFailureReasons.TenantMismatch, crossTenantState.Error);
        Assert.True(scenario.IsSuccess);
        Assert.True(job.IsSuccess);
        Assert.True(jobEvents.IsSuccess);
        Assert.True(scenariosForProject.IsSuccess);
        Assert.True(jobsForProject.IsSuccess);
    }

    private static Project CreateProject(int id, string name, int? organizationId)
    {
        var project = Project.Create(name).Value;
        if (organizationId.HasValue)
        {
            Assert.True(project.AssignOrganization(organizationId.Value).IsSuccess);
        }

        SetPrivateField(project, "<Id>k__BackingField", id);
        return project;
    }

    private static (Project Project, Building Building) CreateBuilding(
        int buildingId,
        int projectId,
        string projectName,
        string buildingName,
        int? organizationId)
    {
        var project = CreateProject(projectId, projectName, organizationId);
        var building = Building.Create(buildingName, project).Value;
        SetPrivateField(building, "<Id>k__BackingField", buildingId);
        SetPrivateField(building, "<ProjectId>k__BackingField", projectId);
        return (project, building);
    }

    private static TenantQueryContext ProjectReadContext(bool allowUnscopedResourcesDuringTransition = true) =>
        CreateContext(Permission.ProjectsRead, allowUnscopedResourcesDuringTransition);

    private static TenantQueryContext BuildingReadContext() =>
        CreateContext(Permission.BuildingsRead, allowUnscopedResourcesDuringTransition: true);

    private static TenantQueryContext WorkflowReadContext() =>
        CreateContext(Permission.WorkflowsRead, allowUnscopedResourcesDuringTransition: true);

    private static TenantQueryContext CreateContext(
        Permission permission,
        bool allowUnscopedResourcesDuringTransition) =>
        new(
            UserId: TenantIsolationScenario.TenantAUserId,
            OrganizationId: TenantIsolationScenario.TenantAOrganizationId,
            IsAuthenticated: true,
            Permissions: new HashSet<string>([permission.ToString()], StringComparer.OrdinalIgnoreCase),
            AllowUnscopedResourcesDuringTransition: allowUnscopedResourcesDuringTransition,
            StrictTenantMatch: true);

    private static void SetPrivateField(object entity, string fieldName, object value)
    {
        var field = entity.GetType().GetField(fieldName, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        Assert.NotNull(field);
        field.SetValue(entity, value);
    }

    private static ProjectAccessScope CreateProjectScope(int projectId, int organizationId) =>
        ProjectTenantAccessScopeFactory.CreateProjectScope(
            projectId,
            organizationId,
            ownerUserId: null,
            isTenantScoped: true,
            tenantScope: new TenantScope(organizationId, $"org-{organizationId}", IsActive: true));

    private static WorkflowAccessScope CreateWorkflowScope(string workflowId, int projectId, int organizationId) =>
        ProjectTenantAccessScopeFactory.CreateWorkflowScope(
            workflowId,
            projectId,
            buildingId: null,
            organizationId: organizationId,
            ownerUserId: null,
            isTenantScoped: true,
            tenantScope: new TenantScope(organizationId, $"org-{organizationId}", IsActive: true));

    private static EngineeringWorkflowStateDto CreateState(int projectId) =>
        new(
            ProjectId: projectId,
            ProjectName: $"Matrix project {projectId}",
            BuildingId: null,
            CurrentStep: "Validation",
            Steps: [new EngineeringWorkflowStepDto("Project", "Completed", true)],
            AvailableModules: ["ThermalTopology"],
            BuildingMetadata: new EngineeringWorkflowBuildingMetadataDto(null, null, null, null, null, null),
            Zones: [],
            Boundaries: [],
            WeatherSolarSettings: new EngineeringWorkflowWeatherSolarSettingsDto("Unknown", "Unknown", "Unknown"),
            VentilationSettings: new EngineeringWorkflowVentilationSettingsDto(0, "Unknown", "Unknown", []),
            GroundSettings: new EngineeringWorkflowGroundSettingsDto(0, "Unknown", "Unknown"),
            DomesticHotWaterSettings: new EngineeringWorkflowDomesticHotWaterSettingsDto("Unknown", "Unknown", "Unknown", "Unknown"),
            SystemEnergySettings: new EngineeringWorkflowSystemEnergySettingsDto("Unknown", "Unknown", "Unknown"),
            Diagnostics: [],
            Assumptions: [],
            Links: [],
            CalculationTraceSummary: null,
            ReportSummary: null,
            Metadata: new Dictionary<string, string>(StringComparer.Ordinal));

    private static EngineeringCalculationScenarioRecordDto CreateScenario(string scenarioId, int projectId) =>
        new(
            ScenarioId: scenarioId,
            ProjectId: projectId,
            BuildingId: null,
            ScenarioKind: EngineeringCalculationScenarioKind.FullEngineeringCore,
            ExecutionMode: EngineeringCalculationExecutionMode.ExecuteFullRequired,
            Status: EngineeringCalculationExecutionStatus.Completed,
            RequestJson: "{}",
            ResultSummaryJson: "{}",
            CreatedAtUtc: DateTimeOffset.UtcNow,
            StartedAtUtc: DateTimeOffset.UtcNow,
            CompletedAtUtc: DateTimeOffset.UtcNow,
            DurationMilliseconds: 10,
            DiagnosticsJson: null);

    private static EngineeringCalculationJobResultDto CreateJob(string jobId, int projectId, string scenarioId) =>
        new(
            JobId: jobId,
            ProjectId: projectId,
            ScenarioId: scenarioId,
            Status: EngineeringCalculationJobStatus.Completed,
            ProgressPercent: 100,
            CurrentStep: "Completed",
            QueuedAtUtc: DateTimeOffset.UtcNow,
            StartedAtUtc: DateTimeOffset.UtcNow,
            CompletedAtUtc: DateTimeOffset.UtcNow,
            DurationMilliseconds: 10,
            ScenarioResultSummary: null,
            Diagnostics: [],
            Assumptions: [],
            Warnings: [],
            PersistedArtifactReferences: [],
            HistoryEvents: [],
            Metadata: new Dictionary<string, string>(StringComparer.Ordinal));

    private const int TenantAProjectId = 30;
    private const int TenantBProjectId = 31;
    private const string JobId = "matrix-job-a";

    private sealed class StubProjectRepository : IProjectRepository
    {
        private readonly IReadOnlyList<Project> _projects;

        public StubProjectRepository(IReadOnlyList<Project> projects)
        {
            _projects = projects;
        }

        public Task<Project?> GetByIdAsync(int id, bool includeBuildings = false, CancellationToken cancellationToken = default)
        {
            _ = includeBuildings;
            _ = cancellationToken;
            return Task.FromResult(_projects.SingleOrDefault(project => project.Id == id));
        }

        public Task<IReadOnlyList<Project>> ListAsync(CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            return Task.FromResult(_projects);
        }

        public void Add(Project project) => throw new NotSupportedException();

        public void Remove(Project project) => throw new NotSupportedException();
    }

    private sealed class StubBuildingRepository : IBuildingRepository
    {
        private readonly IReadOnlyList<Building> _buildings;

        public StubBuildingRepository(IReadOnlyList<Building> buildings)
        {
            _buildings = buildings;
        }

        public Task<Building?> GetByIdAsync(int id, bool includeClimateZone = false, CancellationToken cancellationToken = default)
        {
            _ = includeClimateZone;
            _ = cancellationToken;
            return Task.FromResult(_buildings.SingleOrDefault(building => building.Id == id));
        }

        public Task<IReadOnlyList<Building>> ListByProjectIdAsync(int projectId, CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            return Task.FromResult<IReadOnlyList<Building>>(
                _buildings.Where(building => building.ProjectId == projectId).OrderBy(building => building.Id).ToArray());
        }

        public Task<Building?> GetWithFloorsAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Building?> GetWithThermalZonesAndRoomsAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Building?> GetByThermalZoneIdAsync(int thermalZoneId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Building?> GetForCalculationAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Building?> GetForReportAsync(int id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Building?> GetForValidationAsync(int id, bool asTracking = false, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Add(Building building) => throw new NotSupportedException();
        public void Remove(Building building) => throw new NotSupportedException();
    }

    private sealed class MatrixProjectScopeResolver : IProjectReadAccessScopeResolver
    {
        private readonly IReadOnlyDictionary<int, ProjectAccessScope?> _scopes;

        public MatrixProjectScopeResolver(IReadOnlyDictionary<int, ProjectAccessScope?> scopes)
        {
            _scopes = scopes;
        }

        public Task<ProjectAccessScope?> ResolveProjectScopeAsync(int projectId, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            _scopes.TryGetValue(projectId, out var scope);
            return Task.FromResult(scope);
        }
    }

    private sealed class MatrixWorkflowScopeResolver : IWorkflowAccessScopeResolver
    {
        private readonly IReadOnlyDictionary<string, WorkflowAccessScope?> _scenarioScopes;
        private readonly IReadOnlyDictionary<string, WorkflowAccessScope?> _jobScopes;

        public MatrixWorkflowScopeResolver(
            IReadOnlyDictionary<string, WorkflowAccessScope?> scenarioScopes,
            IReadOnlyDictionary<string, WorkflowAccessScope?> jobScopes)
        {
            _scenarioScopes = scenarioScopes;
            _jobScopes = jobScopes;
        }

        public Task<WorkflowAccessScope?> ResolveWorkflowScopeAsync(string workflowId, CancellationToken cancellationToken)
        {
            _ = workflowId;
            _ = cancellationToken;
            return Task.FromResult<WorkflowAccessScope?>(null);
        }

        public Task<WorkflowAccessScope?> ResolveScenarioScopeAsync(string scenarioId, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            _scenarioScopes.TryGetValue(scenarioId, out var scope);
            return Task.FromResult(scope);
        }

        public Task<WorkflowAccessScope?> ResolveJobScopeAsync(string jobId, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            _jobScopes.TryGetValue(jobId, out var scope);
            return Task.FromResult(scope);
        }
    }

    private sealed class MatrixWorkflowPersistenceService : IEngineeringWorkflowPersistenceService
    {
        private readonly IReadOnlyDictionary<int, EngineeringWorkflowStateDto?> _states;
        private readonly IReadOnlyDictionary<string, EngineeringCalculationScenarioRecordDto?> _scenarios;

        public MatrixWorkflowPersistenceService(
            IReadOnlyDictionary<int, EngineeringWorkflowStateDto?> states,
            IReadOnlyDictionary<string, EngineeringCalculationScenarioRecordDto?> scenarios)
        {
            _states = states;
            _scenarios = scenarios;
        }

        public EngineeringWorkflowPersistenceProviderInfo GetProviderInfo() =>
            new(EngineeringWorkflowPersistenceProvider.InMemory, DurableEnabled: false, ProviderLabel: "InMemory");

        public Task<EngineeringWorkflowStateDto?> GetLatestWorkflowStateAsync(int projectId, int? buildingId, CancellationToken cancellationToken)
        {
            _ = buildingId;
            _ = cancellationToken;
            _states.TryGetValue(projectId, out var state);
            return Task.FromResult(state);
        }

        public Task<EngineeringCalculationScenarioRecordDto?> GetScenarioAsync(string scenarioId, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            _scenarios.TryGetValue(scenarioId, out var scenario);
            return Task.FromResult(scenario);
        }

        public Task<IReadOnlyList<EngineeringCalculationScenarioRecordDto>> ListProjectScenariosAsync(int projectId, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            return Task.FromResult<IReadOnlyList<EngineeringCalculationScenarioRecordDto>>(
                _scenarios.Values.Where(scenario => scenario is not null && scenario.ProjectId == projectId).Cast<EngineeringCalculationScenarioRecordDto>().ToArray());
        }

        public Task<EngineeringWorkflowStateRecordDto> SaveWorkflowStateAsync(EngineeringWorkflowStateDto state, IReadOnlyList<EngineeringWorkflowDiagnosticDto>? validationDiagnostics, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<EngineeringCalculationScenarioRecordDto> SavePreparedScenarioAsync(EngineeringCalculationScenarioRequestDto scenarioRequest, EngineeringCalculationScenarioResultDto scenarioResult, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<EngineeringCalculationScenarioRecordDto> SaveRunScenarioAsync(EngineeringCalculationScenarioRequestDto scenarioRequest, EngineeringCalculationScenarioResultDto scenarioResult, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<EngineeringCalculationArtifactRecordDto>> ListScenarioArtifactsAsync(string scenarioId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<EngineeringCalculationArtifactRecordDto?> GetScenarioArtifactAsync(string scenarioId, EngineeringCalculationArtifactKind artifactKind, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class MatrixJobService : IEngineeringCalculationJobService
    {
        private readonly IReadOnlyDictionary<string, EngineeringCalculationJobResultDto?> _jobs;

        public MatrixJobService(IReadOnlyDictionary<string, EngineeringCalculationJobResultDto?> jobs)
        {
            _jobs = jobs;
        }

        public Task<EngineeringCalculationJobResultDto?> GetJobAsync(string jobId, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            _jobs.TryGetValue(jobId, out var job);
            return Task.FromResult(job);
        }

        public Task<IReadOnlyList<EngineeringCalculationJobResultDto>> ListProjectJobsAsync(int projectId, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            return Task.FromResult<IReadOnlyList<EngineeringCalculationJobResultDto>>(
                _jobs.Values.Where(job => job is not null && job.ProjectId == projectId).Cast<EngineeringCalculationJobResultDto>().ToArray());
        }

        public Task<IReadOnlyList<EngineeringCalculationJobEventDto>> ListJobEventsAsync(string jobId, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            return Task.FromResult<IReadOnlyList<EngineeringCalculationJobEventDto>>(
            [
                new EngineeringCalculationJobEventDto(
                    EventId: $"{jobId}-event",
                    JobId: jobId,
                    ScenarioId: "matrix-scenario-a",
                    Status: EngineeringCalculationJobStatus.Completed,
                    Message: "Completed",
                    ModuleKind: null,
                    ProgressPercent: 100,
                    Diagnostics: [],
                    CreatedAtUtc: DateTimeOffset.UtcNow)
            ]);
        }

        public Task<EngineeringCalculationJobResultDto> CreateOrRunJobAsync(EngineeringCalculationJobRequestDto request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<EngineeringCalculationJobResultDto?> ExecuteQueuedJobAsync(string jobId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<EngineeringCalculationJobResultDto?> ExecuteClaimedJobAsync(string jobId, string workerId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<EngineeringCalculationJobResultDto?> CancelJobAsync(string jobId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
