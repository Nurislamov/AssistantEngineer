using AssistantEngineer.Api.Contracts.Calculations;
using AssistantEngineer.Api.Security.Authorization;
using AssistantEngineer.Api.Services.Calculations.Persistence;
using AssistantEngineer.Modules.Identity.Application.Contracts.Access;
using AssistantEngineer.Modules.Identity.Application.Services.Access;

namespace AssistantEngineer.Tests.Api;

public sealed class DefaultWorkflowAccessScopeResolverTests
{
    [Fact]
    public async Task ResolveWorkflowScopeAsync_UsesScenarioMapping_WhenScenarioExists()
    {
        var resolver = CreateResolver(
            scenarios: new Dictionary<string, EngineeringCalculationScenarioRecordDto>(StringComparer.Ordinal)
            {
                ["wf-1"] = CreateScenarioRecord("wf-1", projectId: 10, buildingId: 20)
            },
            jobs: new Dictionary<string, EngineeringCalculationJobRecordDto>(StringComparer.Ordinal),
            projectScope: null,
            buildingScope: CreateBuildingScope(organizationId: 2001));

        var scope = await resolver.ResolveWorkflowScopeAsync("wf-1", CancellationToken.None);

        Assert.NotNull(scope);
        Assert.Equal("wf-1", scope.WorkflowId);
        Assert.Equal(10, scope.ProjectId);
        Assert.Equal(20, scope.BuildingId);
        Assert.Equal(2001, scope.OrganizationId);
        Assert.True(scope.IsTenantScoped);
    }

    [Fact]
    public async Task ResolveScenarioScopeAsync_UsesProjectScopeFallback_WhenBuildingMissing()
    {
        var resolver = CreateResolver(
            scenarios: new Dictionary<string, EngineeringCalculationScenarioRecordDto>(StringComparer.Ordinal)
            {
                ["sc-1"] = CreateScenarioRecord("sc-1", projectId: 10, buildingId: null)
            },
            jobs: new Dictionary<string, EngineeringCalculationJobRecordDto>(StringComparer.Ordinal),
            projectScope: CreateProjectScope(organizationId: 2001),
            buildingScope: null);

        var scope = await resolver.ResolveScenarioScopeAsync("sc-1", CancellationToken.None);

        Assert.NotNull(scope);
        Assert.Equal("sc-1", scope.WorkflowId);
        Assert.Equal(10, scope.ProjectId);
        Assert.Null(scope.BuildingId);
        Assert.Equal(2001, scope.OrganizationId);
    }

    [Fact]
    public async Task ResolveJobScopeAsync_ResolvesJobAndParentScenarioScope()
    {
        var resolver = CreateResolver(
            scenarios: new Dictionary<string, EngineeringCalculationScenarioRecordDto>(StringComparer.Ordinal)
            {
                ["sc-2"] = CreateScenarioRecord("sc-2", projectId: 10, buildingId: 20)
            },
            jobs: new Dictionary<string, EngineeringCalculationJobRecordDto>(StringComparer.Ordinal)
            {
                ["job-2"] = CreateJobRecord("job-2", projectId: 10, scenarioId: "sc-2")
            },
            projectScope: null,
            buildingScope: CreateBuildingScope(organizationId: 2001));

        var scope = await resolver.ResolveJobScopeAsync("job-2", CancellationToken.None);

        Assert.NotNull(scope);
        Assert.Equal("job-2", scope.WorkflowId);
        Assert.Equal(10, scope.ProjectId);
        Assert.Equal(20, scope.BuildingId);
        Assert.Equal(2001, scope.OrganizationId);
    }

    [Fact]
    public async Task MissingIdentifiers_ReturnNull()
    {
        var resolver = CreateResolver(
            scenarios: new Dictionary<string, EngineeringCalculationScenarioRecordDto>(StringComparer.Ordinal),
            jobs: new Dictionary<string, EngineeringCalculationJobRecordDto>(StringComparer.Ordinal),
            projectScope: null,
            buildingScope: null);

        Assert.Null(await resolver.ResolveWorkflowScopeAsync("missing", CancellationToken.None));
        Assert.Null(await resolver.ResolveScenarioScopeAsync("missing", CancellationToken.None));
        Assert.Null(await resolver.ResolveJobScopeAsync("missing", CancellationToken.None));
    }

    [Fact]
    public async Task IncompleteMetadata_ReturnsStagedUnscopedScopeWithoutThrow()
    {
        var resolver = CreateResolver(
            scenarios: new Dictionary<string, EngineeringCalculationScenarioRecordDto>(StringComparer.Ordinal)
            {
                ["sc-no-tenant"] = CreateScenarioRecord("sc-no-tenant", projectId: 10, buildingId: null)
            },
            jobs: new Dictionary<string, EngineeringCalculationJobRecordDto>(StringComparer.Ordinal),
            projectScope: null,
            buildingScope: null);

        var scope = await resolver.ResolveScenarioScopeAsync("sc-no-tenant", CancellationToken.None);

        Assert.NotNull(scope);
        Assert.Equal("sc-no-tenant", scope.WorkflowId);
        Assert.Equal(10, scope.ProjectId);
        Assert.Null(scope.OrganizationId);
        Assert.False(scope.IsTenantScoped);
    }

    private static DefaultWorkflowAccessScopeResolver CreateResolver(
        IReadOnlyDictionary<string, EngineeringCalculationScenarioRecordDto> scenarios,
        IReadOnlyDictionary<string, EngineeringCalculationJobRecordDto> jobs,
        ProjectAccessScope? projectScope,
        BuildingAccessScope? buildingScope)
    {
        return new DefaultWorkflowAccessScopeResolver(
            new StubWorkflowPersistenceService(scenarios),
            new StubJobRepository(jobs),
            new StubProjectScopeResolver(projectScope),
            new StubBuildingScopeResolver(buildingScope));
    }

    private static EngineeringCalculationScenarioRecordDto CreateScenarioRecord(
        string scenarioId,
        int projectId,
        int? buildingId)
    {
        return new EngineeringCalculationScenarioRecordDto(
            ScenarioId: scenarioId,
            ProjectId: projectId,
            BuildingId: buildingId,
            ScenarioKind: EngineeringCalculationScenarioKind.FullEngineeringCore,
            ExecutionMode: EngineeringCalculationExecutionMode.PrepareOnly,
            Status: EngineeringCalculationExecutionStatus.Prepared,
            RequestJson: "{}",
            ResultSummaryJson: null,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            StartedAtUtc: null,
            CompletedAtUtc: null,
            DurationMilliseconds: null,
            DiagnosticsJson: null);
    }

    private static EngineeringCalculationJobRecordDto CreateJobRecord(
        string jobId,
        int projectId,
        string scenarioId)
    {
        return new EngineeringCalculationJobRecordDto(
            JobId: jobId,
            ProjectId: projectId,
            ScenarioId: scenarioId,
            Status: EngineeringCalculationJobStatus.Queued,
            ExecutionMode: EngineeringCalculationJobExecutionMode.Queued,
            RequestJson: "{}",
            ResultSummaryJson: null,
            DiagnosticsJson: null,
            ProgressPercent: 0,
            CurrentStep: "Queued",
            CreatedAtUtc: DateTimeOffset.UtcNow,
            QueuedAtUtc: DateTimeOffset.UtcNow,
            StartedAtUtc: null,
            CompletedAtUtc: null,
            UpdatedAtUtc: DateTimeOffset.UtcNow,
            DurationMilliseconds: null,
            RetryCount: 0,
            CancellationRequested: false);
    }

    private static ProjectAccessScope CreateProjectScope(int organizationId)
    {
        return ProjectTenantAccessScopeFactory.CreateProjectScope(
            projectId: 10,
            organizationId: organizationId,
            ownerUserId: null,
            isTenantScoped: true,
            tenantScope: new TenantScope(organizationId, $"org-{organizationId}", IsActive: true));
    }

    private static BuildingAccessScope CreateBuildingScope(int organizationId)
    {
        return ProjectTenantAccessScopeFactory.CreateBuildingScope(
            buildingId: 20,
            projectId: 10,
            organizationId: organizationId,
            ownerUserId: null,
            isTenantScoped: true,
            tenantScope: new TenantScope(organizationId, $"org-{organizationId}", IsActive: true));
    }

    private sealed class StubWorkflowPersistenceService : IEngineeringWorkflowPersistenceService
    {
        private readonly IReadOnlyDictionary<string, EngineeringCalculationScenarioRecordDto> _scenarios;

        public StubWorkflowPersistenceService(IReadOnlyDictionary<string, EngineeringCalculationScenarioRecordDto> scenarios)
        {
            _scenarios = scenarios;
        }

        public EngineeringWorkflowPersistenceProviderInfo GetProviderInfo() =>
            new(EngineeringWorkflowPersistenceProvider.InMemory, DurableEnabled: false, ProviderLabel: "in-memory");

        public Task<EngineeringWorkflowStateDto?> GetLatestWorkflowStateAsync(int projectId, int? buildingId, CancellationToken cancellationToken) =>
            Task.FromResult<EngineeringWorkflowStateDto?>(null);

        public Task<EngineeringWorkflowStateRecordDto> SaveWorkflowStateAsync(EngineeringWorkflowStateDto state, IReadOnlyList<EngineeringWorkflowDiagnosticDto>? validationDiagnostics, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<EngineeringCalculationScenarioRecordDto> SavePreparedScenarioAsync(EngineeringCalculationScenarioRequestDto scenarioRequest, EngineeringCalculationScenarioResultDto scenarioResult, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<EngineeringCalculationScenarioRecordDto> SaveRunScenarioAsync(EngineeringCalculationScenarioRequestDto scenarioRequest, EngineeringCalculationScenarioResultDto scenarioResult, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<EngineeringCalculationScenarioRecordDto?> GetScenarioAsync(string scenarioId, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            _scenarios.TryGetValue(scenarioId, out var scenario);
            return Task.FromResult(scenario);
        }

        public Task<IReadOnlyList<EngineeringCalculationScenarioRecordDto>> ListProjectScenariosAsync(int projectId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<EngineeringCalculationArtifactRecordDto>> ListScenarioArtifactsAsync(string scenarioId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<EngineeringCalculationArtifactRecordDto?> GetScenarioArtifactAsync(string scenarioId, EngineeringCalculationArtifactKind artifactKind, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class StubJobRepository : IEngineeringCalculationJobRepository
    {
        private readonly IReadOnlyDictionary<string, EngineeringCalculationJobRecordDto> _jobs;

        public StubJobRepository(IReadOnlyDictionary<string, EngineeringCalculationJobRecordDto> jobs)
        {
            _jobs = jobs;
        }

        public Task<EngineeringCalculationJobRecordDto> CreateAsync(EngineeringCalculationJobRecordDto job, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<EngineeringCalculationJobRecordDto> UpdateAsync(EngineeringCalculationJobRecordDto job, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<EngineeringCalculationJobRecordDto>> ListQueuedAsync(int maxCount, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<EngineeringCalculationJobRecordDto?> TryClaimQueuedJobAsync(string jobId, string workerId, TimeSpan leaseDuration, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task<EngineeringCalculationJobRecordDto?> GetByIdAsync(string jobId, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            _jobs.TryGetValue(jobId, out var job);
            return Task.FromResult(job);
        }

        public Task<IReadOnlyList<EngineeringCalculationJobRecordDto>> ListByProjectIdAsync(int projectId, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class StubProjectScopeResolver : IProjectReadAccessScopeResolver
    {
        private readonly ProjectAccessScope? _scope;

        public StubProjectScopeResolver(ProjectAccessScope? scope)
        {
            _scope = scope;
        }

        public Task<ProjectAccessScope?> ResolveProjectScopeAsync(int projectId, CancellationToken cancellationToken)
        {
            _ = projectId;
            _ = cancellationToken;
            return Task.FromResult(_scope);
        }
    }

    private sealed class StubBuildingScopeResolver : IBuildingReadAccessScopeResolver
    {
        private readonly BuildingAccessScope? _scope;

        public StubBuildingScopeResolver(BuildingAccessScope? scope)
        {
            _scope = scope;
        }

        public Task<BuildingAccessScope?> ResolveBuildingScopeAsync(int buildingId, CancellationToken cancellationToken)
        {
            _ = buildingId;
            _ = cancellationToken;
            return Task.FromResult(_scope);
        }
    }
}
