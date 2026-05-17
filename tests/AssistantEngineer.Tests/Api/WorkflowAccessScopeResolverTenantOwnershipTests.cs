using AssistantEngineer.Api.Contracts.Calculations;
using AssistantEngineer.Api.Security.Authorization;
using AssistantEngineer.Api.Services.Calculations.Persistence;
using AssistantEngineer.Modules.Identity.Application.Contracts.Access;
using AssistantEngineer.Modules.Identity.Application.Services.Access;

namespace AssistantEngineer.Tests.Api;

public sealed class WorkflowAccessScopeResolverTenantOwnershipTests
{
    [Fact]
    public async Task ScenarioWithProjectAndBuildingMetadata_ResolvesOrganizationFromProjectBackedBuildingScope()
    {
        var resolver = CreateResolver(
            scenarios: new Dictionary<string, EngineeringCalculationScenarioRecordDto>(StringComparer.Ordinal)
            {
                ["scenario-tenant"] = CreateScenarioRecord("scenario-tenant", projectId: 10, buildingId: 20)
            },
            jobs: new Dictionary<string, EngineeringCalculationJobRecordDto>(StringComparer.Ordinal),
            projectScope: CreateProjectScope(organizationId: 1001, ownerUserId: 2001),
            buildingScope: CreateBuildingScope(organizationId: 1001, ownerUserId: 2001));

        var scope = await resolver.ResolveScenarioScopeAsync("scenario-tenant", CancellationToken.None);

        Assert.NotNull(scope);
        Assert.Equal("scenario-tenant", scope.WorkflowId);
        Assert.Equal(10, scope.ProjectId);
        Assert.Equal(20, scope.BuildingId);
        Assert.Equal(1001, scope.OrganizationId);
        Assert.Equal(2001, scope.OwnerUserId);
        Assert.True(scope.IsTenantScoped);
    }

    [Fact]
    public async Task JobWithProjectMetadata_ResolvesOrganizationFromProjectScope()
    {
        var resolver = CreateResolver(
            scenarios: new Dictionary<string, EngineeringCalculationScenarioRecordDto>(StringComparer.Ordinal),
            jobs: new Dictionary<string, EngineeringCalculationJobRecordDto>(StringComparer.Ordinal)
            {
                ["job-tenant"] = CreateJobRecord("job-tenant", projectId: 10, scenarioId: string.Empty)
            },
            projectScope: CreateProjectScope(organizationId: 1001, ownerUserId: 2001),
            buildingScope: null);

        var scope = await resolver.ResolveJobScopeAsync("job-tenant", CancellationToken.None);

        Assert.NotNull(scope);
        Assert.Equal("job-tenant", scope.WorkflowId);
        Assert.Equal(10, scope.ProjectId);
        Assert.Null(scope.BuildingId);
        Assert.Equal(1001, scope.OrganizationId);
        Assert.Equal(2001, scope.OwnerUserId);
        Assert.True(scope.IsTenantScoped);
    }

    [Fact]
    public async Task MissingProjectOwnership_ReturnsStagedUnscopedScope()
    {
        var resolver = CreateResolver(
            scenarios: new Dictionary<string, EngineeringCalculationScenarioRecordDto>(StringComparer.Ordinal)
            {
                ["scenario-unscoped"] = CreateScenarioRecord("scenario-unscoped", projectId: 10, buildingId: null)
            },
            jobs: new Dictionary<string, EngineeringCalculationJobRecordDto>(StringComparer.Ordinal),
            projectScope: CreateProjectScope(organizationId: null, ownerUserId: null),
            buildingScope: null);

        var scope = await resolver.ResolveScenarioScopeAsync("scenario-unscoped", CancellationToken.None);

        Assert.NotNull(scope);
        Assert.Equal("scenario-unscoped", scope.WorkflowId);
        Assert.Equal(10, scope.ProjectId);
        Assert.Null(scope.OrganizationId);
        Assert.Null(scope.OwnerUserId);
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

    private static EngineeringCalculationScenarioRecordDto CreateScenarioRecord(string scenarioId, int projectId, int? buildingId)
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

    private static EngineeringCalculationJobRecordDto CreateJobRecord(string jobId, int projectId, string scenarioId)
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

    private static ProjectAccessScope CreateProjectScope(int? organizationId, int? ownerUserId)
    {
        return ProjectTenantAccessScopeFactory.CreateProjectScope(
            projectId: 10,
            organizationId: organizationId,
            ownerUserId: ownerUserId,
            isTenantScoped: organizationId.HasValue,
            tenantScope: organizationId.HasValue
                ? new TenantScope(organizationId.Value, $"org-{organizationId.Value}", IsActive: true)
                : null);
    }

    private static BuildingAccessScope CreateBuildingScope(int organizationId, int ownerUserId)
    {
        return ProjectTenantAccessScopeFactory.CreateBuildingScope(
            buildingId: 20,
            projectId: 10,
            organizationId: organizationId,
            ownerUserId: ownerUserId,
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
            throw new NotSupportedException();

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
