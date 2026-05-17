using AssistantEngineer.Api.Contracts.Calculations;
using AssistantEngineer.Api.Security.Authorization;
using AssistantEngineer.Api.Services.Calculations.Persistence;
using AssistantEngineer.Modules.Identity.Application.Contracts.Access;
using AssistantEngineer.Modules.Identity.Application.Services.Access;
using AssistantEngineer.Modules.Identity.Domain.Enums;

namespace AssistantEngineer.Tests.Api;

public sealed class DefaultWorkflowAccessScopeResolverMetadataCoverageTests
{
    [Fact]
    public async Task ResolveScenarioScopeAsync_WithProjectId_ResolvesViaProjectScope()
    {
        var resolver = CreateResolver(
            scenarios: new Dictionary<string, EngineeringCalculationScenarioRecordDto>(StringComparer.Ordinal)
            {
                ["scenario-project"] = CreateScenario("scenario-project", projectId: 10, buildingId: null)
            },
            jobs: new Dictionary<string, EngineeringCalculationJobRecordDto>(StringComparer.Ordinal),
            projectScopes: new Dictionary<int, ProjectAccessScope?>
            {
                [10] = CreateProjectScope(10, 1001)
            },
            buildingScopes: new Dictionary<int, BuildingAccessScope?>());

        var scope = await resolver.ResolveScenarioScopeAsync("scenario-project", CancellationToken.None);

        Assert.NotNull(scope);
        Assert.Equal(10, scope.ProjectId);
        Assert.Null(scope.BuildingId);
        Assert.Equal(1001, scope.OrganizationId);
        Assert.True(scope.IsTenantScoped);
    }

    [Fact]
    public async Task ResolveScenarioScopeAsync_WithBuildingId_ResolvesViaBuildingScope()
    {
        var resolver = CreateResolver(
            scenarios: new Dictionary<string, EngineeringCalculationScenarioRecordDto>(StringComparer.Ordinal)
            {
                ["scenario-building"] = CreateScenario("scenario-building", projectId: 10, buildingId: 20)
            },
            jobs: new Dictionary<string, EngineeringCalculationJobRecordDto>(StringComparer.Ordinal),
            projectScopes: new Dictionary<int, ProjectAccessScope?>(),
            buildingScopes: new Dictionary<int, BuildingAccessScope?>
            {
                [20] = CreateBuildingScope(20, 10, 1001)
            });

        var scope = await resolver.ResolveScenarioScopeAsync("scenario-building", CancellationToken.None);

        Assert.NotNull(scope);
        Assert.Equal(10, scope.ProjectId);
        Assert.Equal(20, scope.BuildingId);
        Assert.Equal(1001, scope.OrganizationId);
        Assert.True(scope.IsTenantScoped);
    }

    [Fact]
    public async Task ResolveWorkflowScopeAsync_WithScenarioWorkflowId_ResolvesScenarioScope()
    {
        var resolver = CreateResolver(
            scenarios: new Dictionary<string, EngineeringCalculationScenarioRecordDto>(StringComparer.Ordinal)
            {
                ["workflow-scenario"] = CreateScenario("workflow-scenario", projectId: 10, buildingId: null)
            },
            jobs: new Dictionary<string, EngineeringCalculationJobRecordDto>(StringComparer.Ordinal),
            projectScopes: new Dictionary<int, ProjectAccessScope?>
            {
                [10] = CreateProjectScope(10, 1001)
            },
            buildingScopes: new Dictionary<int, BuildingAccessScope?>());

        var scope = await resolver.ResolveWorkflowScopeAsync("workflow-scenario", CancellationToken.None);

        Assert.NotNull(scope);
        Assert.Equal("workflow-scenario", scope.WorkflowId);
        Assert.Equal(10, scope.ProjectId);
        Assert.Equal(1001, scope.OrganizationId);
    }

    [Fact]
    public async Task ResolveScenarioScopeAsync_MissingOwnershipMetadata_ReturnsNull()
    {
        var resolver = CreateResolver(
            scenarios: new Dictionary<string, EngineeringCalculationScenarioRecordDto>(StringComparer.Ordinal)
            {
                ["scenario-missing"] = CreateScenario("scenario-missing", projectId: 0, buildingId: null)
            },
            jobs: new Dictionary<string, EngineeringCalculationJobRecordDto>(StringComparer.Ordinal),
            projectScopes: new Dictionary<int, ProjectAccessScope?>(),
            buildingScopes: new Dictionary<int, BuildingAccessScope?>());

        var scope = await resolver.ResolveScenarioScopeAsync("scenario-missing", CancellationToken.None);

        Assert.Null(scope);
    }

    [Fact]
    public async Task ResolveJobScopeAsync_WithProjectId_ResolvesViaProjectScope()
    {
        var resolver = CreateResolver(
            scenarios: new Dictionary<string, EngineeringCalculationScenarioRecordDto>(StringComparer.Ordinal),
            jobs: new Dictionary<string, EngineeringCalculationJobRecordDto>(StringComparer.Ordinal)
            {
                ["job-project"] = CreateJob("job-project", projectId: 10, scenarioId: string.Empty)
            },
            projectScopes: new Dictionary<int, ProjectAccessScope?>
            {
                [10] = CreateProjectScope(10, 1001)
            },
            buildingScopes: new Dictionary<int, BuildingAccessScope?>());

        var scope = await resolver.ResolveJobScopeAsync("job-project", CancellationToken.None);

        Assert.NotNull(scope);
        Assert.Equal(10, scope.ProjectId);
        Assert.Equal(1001, scope.OrganizationId);
    }

    [Fact]
    public async Task ResolveJobScopeAsync_WithScenarioBuildingId_ResolvesViaScenarioAndBuildingScope()
    {
        var resolver = CreateResolver(
            scenarios: new Dictionary<string, EngineeringCalculationScenarioRecordDto>(StringComparer.Ordinal)
            {
                ["scenario-job"] = CreateScenario("scenario-job", projectId: 10, buildingId: 20)
            },
            jobs: new Dictionary<string, EngineeringCalculationJobRecordDto>(StringComparer.Ordinal)
            {
                ["job-building"] = CreateJob("job-building", projectId: 10, scenarioId: "scenario-job")
            },
            projectScopes: new Dictionary<int, ProjectAccessScope?>(),
            buildingScopes: new Dictionary<int, BuildingAccessScope?>
            {
                [20] = CreateBuildingScope(20, 10, 1001)
            });

        var scope = await resolver.ResolveJobScopeAsync("job-building", CancellationToken.None);

        Assert.NotNull(scope);
        Assert.Equal(10, scope.ProjectId);
        Assert.Equal(20, scope.BuildingId);
        Assert.Equal(1001, scope.OrganizationId);
    }

    [Fact]
    public async Task ResolveJobScopeAsync_WithScenarioId_UsesScenarioMetadata()
    {
        var resolver = CreateResolver(
            scenarios: new Dictionary<string, EngineeringCalculationScenarioRecordDto>(StringComparer.Ordinal)
            {
                ["scenario-project-11"] = CreateScenario("scenario-project-11", projectId: 11, buildingId: null)
            },
            jobs: new Dictionary<string, EngineeringCalculationJobRecordDto>(StringComparer.Ordinal)
            {
                ["job-scenario"] = CreateJob("job-scenario", projectId: 10, scenarioId: "scenario-project-11")
            },
            projectScopes: new Dictionary<int, ProjectAccessScope?>
            {
                [11] = CreateProjectScope(11, 1002)
            },
            buildingScopes: new Dictionary<int, BuildingAccessScope?>());

        var scope = await resolver.ResolveJobScopeAsync("job-scenario", CancellationToken.None);

        Assert.NotNull(scope);
        Assert.Equal(11, scope.ProjectId);
        Assert.Equal(1002, scope.OrganizationId);
    }

    [Fact]
    public async Task ResolveWorkflowScopeAsync_WithJobWorkflowId_ResolvesJobScope()
    {
        var resolver = CreateResolver(
            scenarios: new Dictionary<string, EngineeringCalculationScenarioRecordDto>(StringComparer.Ordinal),
            jobs: new Dictionary<string, EngineeringCalculationJobRecordDto>(StringComparer.Ordinal)
            {
                ["workflow-job"] = CreateJob("workflow-job", projectId: 10, scenarioId: string.Empty)
            },
            projectScopes: new Dictionary<int, ProjectAccessScope?>
            {
                [10] = CreateProjectScope(10, 1001)
            },
            buildingScopes: new Dictionary<int, BuildingAccessScope?>());

        var scope = await resolver.ResolveWorkflowScopeAsync("workflow-job", CancellationToken.None);

        Assert.NotNull(scope);
        Assert.Equal("workflow-job", scope.WorkflowId);
        Assert.Equal(10, scope.ProjectId);
    }

    [Fact]
    public async Task ResolveJobScopeAsync_MissingOwnershipMetadata_ReturnsNull()
    {
        var resolver = CreateResolver(
            scenarios: new Dictionary<string, EngineeringCalculationScenarioRecordDto>(StringComparer.Ordinal),
            jobs: new Dictionary<string, EngineeringCalculationJobRecordDto>(StringComparer.Ordinal)
            {
                ["job-missing"] = CreateJob("job-missing", projectId: 0, scenarioId: string.Empty)
            },
            projectScopes: new Dictionary<int, ProjectAccessScope?>(),
            buildingScopes: new Dictionary<int, BuildingAccessScope?>());

        var scope = await resolver.ResolveJobScopeAsync("job-missing", CancellationToken.None);

        Assert.Null(scope);
    }

    [Fact]
    public async Task ResolveScenarioScopeAsync_CrossTenantMetadata_ProducesPolicyTenantMismatch()
    {
        var resolver = CreateResolver(
            scenarios: new Dictionary<string, EngineeringCalculationScenarioRecordDto>(StringComparer.Ordinal)
            {
                ["scenario-cross"] = CreateScenario("scenario-cross", projectId: 10, buildingId: null)
            },
            jobs: new Dictionary<string, EngineeringCalculationJobRecordDto>(StringComparer.Ordinal),
            projectScopes: new Dictionary<int, ProjectAccessScope?>
            {
                [10] = CreateProjectScope(10, 1002)
            },
            buildingScopes: new Dictionary<int, BuildingAccessScope?>());

        var scope = await resolver.ResolveScenarioScopeAsync("scenario-cross", CancellationToken.None);
        Assert.NotNull(scope);

        var decision = new TenantQueryIsolationPolicy().CanReadResource(
            new TenantQueryContext(
                UserId: 2001,
                OrganizationId: 1001,
                IsAuthenticated: true,
                Permissions: new HashSet<string>([Permission.WorkflowsRead.ToString()], StringComparer.OrdinalIgnoreCase),
                AllowUnscopedResourcesDuringTransition: true,
                StrictTenantMatch: true),
            scope.OrganizationId,
            Permission.WorkflowsRead.ToString());

        Assert.False(decision.Allowed);
        Assert.Equal(TenantQueryFailureReasons.TenantMismatch, decision.FailureReason);
    }

    [Fact]
    public async Task LegacyRecordsWithMissingOwnershipMetadata_RemainNonThrowing()
    {
        var resolver = CreateResolver(
            scenarios: new Dictionary<string, EngineeringCalculationScenarioRecordDto>(StringComparer.Ordinal)
            {
                ["legacy-scenario"] = CreateScenario("legacy-scenario", projectId: 0, buildingId: null)
            },
            jobs: new Dictionary<string, EngineeringCalculationJobRecordDto>(StringComparer.Ordinal)
            {
                ["legacy-job"] = CreateJob("legacy-job", projectId: 0, scenarioId: string.Empty)
            },
            projectScopes: new Dictionary<int, ProjectAccessScope?>(),
            buildingScopes: new Dictionary<int, BuildingAccessScope?>());

        var scenarioScope = await resolver.ResolveScenarioScopeAsync("legacy-scenario", CancellationToken.None);
        var jobScope = await resolver.ResolveJobScopeAsync("legacy-job", CancellationToken.None);

        Assert.Null(scenarioScope);
        Assert.Null(jobScope);
    }

    private static DefaultWorkflowAccessScopeResolver CreateResolver(
        IReadOnlyDictionary<string, EngineeringCalculationScenarioRecordDto> scenarios,
        IReadOnlyDictionary<string, EngineeringCalculationJobRecordDto> jobs,
        IReadOnlyDictionary<int, ProjectAccessScope?> projectScopes,
        IReadOnlyDictionary<int, BuildingAccessScope?> buildingScopes)
    {
        return new DefaultWorkflowAccessScopeResolver(
            new StubWorkflowPersistenceService(scenarios),
            new StubJobRepository(jobs),
            new StubProjectScopeResolver(projectScopes),
            new StubBuildingScopeResolver(buildingScopes));
    }

    private static EngineeringCalculationScenarioRecordDto CreateScenario(string scenarioId, int projectId, int? buildingId)
    {
        return new EngineeringCalculationScenarioRecordDto(
            ScenarioId: scenarioId,
            ProjectId: projectId,
            BuildingId: buildingId,
            ScenarioKind: EngineeringCalculationScenarioKind.FullEngineeringCore,
            ExecutionMode: EngineeringCalculationExecutionMode.ExecuteAvailableModules,
            Status: EngineeringCalculationExecutionStatus.Completed,
            RequestJson: "{}",
            ResultSummaryJson: "{}",
            CreatedAtUtc: DateTimeOffset.UtcNow,
            StartedAtUtc: DateTimeOffset.UtcNow,
            CompletedAtUtc: DateTimeOffset.UtcNow,
            DurationMilliseconds: 1,
            DiagnosticsJson: null);
    }

    private static EngineeringCalculationJobRecordDto CreateJob(string jobId, int projectId, string scenarioId)
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

    private static ProjectAccessScope CreateProjectScope(int projectId, int organizationId)
    {
        return ProjectTenantAccessScopeFactory.CreateProjectScope(
            projectId: projectId,
            organizationId: organizationId,
            ownerUserId: null,
            isTenantScoped: true,
            tenantScope: new TenantScope(organizationId, $"org-{organizationId}", IsActive: true));
    }

    private static BuildingAccessScope CreateBuildingScope(int buildingId, int projectId, int organizationId)
    {
        return ProjectTenantAccessScopeFactory.CreateBuildingScope(
            buildingId: buildingId,
            projectId: projectId,
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

        public Task<EngineeringCalculationJobRecordDto> CreateAsync(EngineeringCalculationJobRecordDto job, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<EngineeringCalculationJobRecordDto> UpdateAsync(EngineeringCalculationJobRecordDto job, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<EngineeringCalculationJobRecordDto>> ListQueuedAsync(int maxCount, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<EngineeringCalculationJobRecordDto?> TryClaimQueuedJobAsync(string jobId, string workerId, TimeSpan leaseDuration, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<EngineeringCalculationJobRecordDto?> GetByIdAsync(string jobId, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            _jobs.TryGetValue(jobId, out var job);
            return Task.FromResult(job);
        }

        public Task<IReadOnlyList<EngineeringCalculationJobRecordDto>> ListByProjectIdAsync(int projectId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class StubProjectScopeResolver : IProjectReadAccessScopeResolver
    {
        private readonly IReadOnlyDictionary<int, ProjectAccessScope?> _scopes;

        public StubProjectScopeResolver(IReadOnlyDictionary<int, ProjectAccessScope?> scopes)
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

    private sealed class StubBuildingScopeResolver : IBuildingReadAccessScopeResolver
    {
        private readonly IReadOnlyDictionary<int, BuildingAccessScope?> _scopes;

        public StubBuildingScopeResolver(IReadOnlyDictionary<int, BuildingAccessScope?> scopes)
        {
            _scopes = scopes;
        }

        public Task<BuildingAccessScope?> ResolveBuildingScopeAsync(int buildingId, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            _scopes.TryGetValue(buildingId, out var scope);
            return Task.FromResult(scope);
        }
    }
}
