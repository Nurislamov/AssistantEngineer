using AssistantEngineer.Api.Contracts.Calculations;
using AssistantEngineer.Api.Security.Authorization;
using AssistantEngineer.Api.Security.TenantIsolation;
using AssistantEngineer.Api.Services.Calculations;
using AssistantEngineer.Api.Services.Calculations.Persistence;
using AssistantEngineer.Modules.Identity.Application.Contracts.Access;
using AssistantEngineer.Modules.Identity.Application.Services.Access;
using AssistantEngineer.Modules.Identity.Domain.Enums;

namespace AssistantEngineer.Tests.Api.Security.TenantIsolation;

public sealed class WorkflowTenantScopedReadServiceMetadataCoverageTests
{
    [Fact]
    public async Task Scenario_SameTenantWithProjectMetadata_IsAllowed()
    {
        var service = CreateService(
            workflowPersistence: new StubWorkflowPersistenceService(
                scenarios: new Dictionary<string, EngineeringCalculationScenarioRecordDto?>
                {
                    ["scenario-a"] = CreateScenario("scenario-a", projectId: 10)
                }),
            workflowScopes: new Dictionary<string, WorkflowAccessScope?>
            {
                ["scenario-a"] = CreateWorkflowScope("scenario-a", projectId: 10, organizationId: 1001)
            },
            projectScopes: new Dictionary<int, ProjectAccessScope?>
            {
                [10] = CreateProjectScope(10, 1001)
            });

        var result = await service.GetScenarioForTenantAsync("scenario-a", TenantContext(organizationId: 1001), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("scenario-a", result.Value.ScenarioId);
    }

    [Fact]
    public async Task Scenario_CrossTenantWithProjectMetadata_IsDenied()
    {
        var service = CreateService(
            workflowPersistence: new StubWorkflowPersistenceService(
                scenarios: new Dictionary<string, EngineeringCalculationScenarioRecordDto?>
                {
                    ["scenario-b"] = CreateScenario("scenario-b", projectId: 11)
                }),
            workflowScopes: new Dictionary<string, WorkflowAccessScope?>
            {
                ["scenario-b"] = CreateWorkflowScope("scenario-b", projectId: 11, organizationId: 1002)
            },
            projectScopes: new Dictionary<int, ProjectAccessScope?>
            {
                [11] = CreateProjectScope(11, 1002)
            });

        var result = await service.GetScenarioForTenantAsync("scenario-b", TenantContext(organizationId: 1001), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(TenantQueryFailureReasons.TenantMismatch, result.Error);
    }

    [Fact]
    public async Task Scenario_MissingOwnershipMetadata_StrictMode_Denies()
    {
        var service = CreateService(
            workflowPersistence: new StubWorkflowPersistenceService(
                scenarios: new Dictionary<string, EngineeringCalculationScenarioRecordDto?>
                {
                    ["scenario-missing"] = CreateScenario("scenario-missing", projectId: 0)
                }));

        var result = await service.GetScenarioForTenantAsync(
            "scenario-missing",
            TenantContext(organizationId: 1001, strictTenantMatch: true, allowUnscoped: true),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(TenantQueryFailureReasons.MissingOrganization, result.Error);
    }

    [Fact]
    public async Task Scenario_MissingOwnershipMetadata_CompatibilityMode_FollowsOption()
    {
        var service = CreateService(
            workflowPersistence: new StubWorkflowPersistenceService(
                scenarios: new Dictionary<string, EngineeringCalculationScenarioRecordDto?>
                {
                    ["scenario-missing"] = CreateScenario("scenario-missing", projectId: 0)
                }));

        var allowed = await service.GetScenarioForTenantAsync(
            "scenario-missing",
            TenantContext(organizationId: 1001, strictTenantMatch: false, allowUnscoped: true),
            CancellationToken.None);
        var denied = await service.GetScenarioForTenantAsync(
            "scenario-missing",
            TenantContext(organizationId: 1001, strictTenantMatch: false, allowUnscoped: false),
            CancellationToken.None);

        Assert.True(allowed.IsSuccess);
        Assert.True(denied.IsFailure);
        Assert.Equal(TenantQueryFailureReasons.UnscopedResourceDenied, denied.Error);
    }

    [Fact]
    public async Task Job_SameTenantWithProjectAndScenarioMetadata_IsAllowed()
    {
        var service = CreateService(
            jobService: new StubJobService(
                jobs: new Dictionary<string, EngineeringCalculationJobResultDto?>
                {
                    ["job-a"] = CreateJob("job-a", projectId: 10, scenarioId: "scenario-a")
                }),
            workflowScopes: new Dictionary<string, WorkflowAccessScope?>
            {
                ["job-a"] = CreateWorkflowScope("job-a", projectId: 10, organizationId: 1001)
            },
            projectScopes: new Dictionary<int, ProjectAccessScope?>
            {
                [10] = CreateProjectScope(10, 1001)
            });

        var result = await service.GetJobForTenantAsync("job-a", TenantContext(organizationId: 1001), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("job-a", result.Value.JobId);
    }

    [Fact]
    public async Task Job_CrossTenantWithProjectMetadata_IsDenied()
    {
        var service = CreateService(
            jobService: new StubJobService(
                jobs: new Dictionary<string, EngineeringCalculationJobResultDto?>
                {
                    ["job-b"] = CreateJob("job-b", projectId: 11, scenarioId: "scenario-b")
                }),
            workflowScopes: new Dictionary<string, WorkflowAccessScope?>
            {
                ["job-b"] = CreateWorkflowScope("job-b", projectId: 11, organizationId: 1002)
            },
            projectScopes: new Dictionary<int, ProjectAccessScope?>
            {
                [11] = CreateProjectScope(11, 1002)
            });

        var result = await service.GetJobForTenantAsync("job-b", TenantContext(organizationId: 1001), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(TenantQueryFailureReasons.TenantMismatch, result.Error);
    }

    [Fact]
    public async Task Job_MissingOwnershipMetadata_StrictMode_Denies()
    {
        var service = CreateService(
            jobService: new StubJobService(
                jobs: new Dictionary<string, EngineeringCalculationJobResultDto?>
                {
                    ["job-missing"] = CreateJob("job-missing", projectId: 0, scenarioId: "")
                }));

        var result = await service.GetJobForTenantAsync(
            "job-missing",
            TenantContext(organizationId: 1001, strictTenantMatch: true, allowUnscoped: true),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(TenantQueryFailureReasons.MissingOrganization, result.Error);
    }

    [Fact]
    public async Task JobEvents_AuthorizeThroughJobMetadata()
    {
        var service = CreateService(
            jobService: new StubJobService(
                jobs: new Dictionary<string, EngineeringCalculationJobResultDto?>
                {
                    ["job-events"] = CreateJob("job-events", projectId: 11, scenarioId: "scenario-b")
                },
                eventsByJobId: new Dictionary<string, IReadOnlyList<EngineeringCalculationJobEventDto>>
                {
                    ["job-events"] = [CreateEvent("job-events", "scenario-b")]
                }),
            workflowScopes: new Dictionary<string, WorkflowAccessScope?>
            {
                ["job-events"] = CreateWorkflowScope("job-events", projectId: 11, organizationId: 1002)
            },
            projectScopes: new Dictionary<int, ProjectAccessScope?>
            {
                [11] = CreateProjectScope(11, 1002)
            });

        var denied = await service.GetJobEventsForTenantAsync("job-events", TenantContext(organizationId: 1001), CancellationToken.None);
        var allowed = await service.GetJobEventsForTenantAsync("job-events", TenantContext(organizationId: 1002), CancellationToken.None);

        Assert.True(denied.IsFailure);
        Assert.Equal(TenantQueryFailureReasons.TenantMismatch, denied.Error);
        Assert.True(allowed.IsSuccess);
        Assert.Single(allowed.Value);
    }

    private static WorkflowTenantScopedReadService CreateService(
        StubWorkflowPersistenceService? workflowPersistence = null,
        StubJobService? jobService = null,
        IReadOnlyDictionary<int, ProjectAccessScope?>? projectScopes = null,
        IReadOnlyDictionary<string, WorkflowAccessScope?>? workflowScopes = null)
    {
        return new WorkflowTenantScopedReadService(
            workflowPersistence ?? new StubWorkflowPersistenceService(),
            jobService ?? new StubJobService(),
            new StubProjectScopeResolver(projectScopes ?? new Dictionary<int, ProjectAccessScope?>()),
            new StubWorkflowScopeResolver(
                scenarioScopes: workflowScopes ?? new Dictionary<string, WorkflowAccessScope?>(),
                jobScopes: workflowScopes ?? new Dictionary<string, WorkflowAccessScope?>()),
            new TenantQueryIsolationPolicy());
    }

    private static TenantQueryContext TenantContext(
        int organizationId,
        bool strictTenantMatch = true,
        bool allowUnscoped = true)
    {
        return new TenantQueryContext(
            UserId: 2001,
            OrganizationId: organizationId,
            IsAuthenticated: true,
            Permissions: new HashSet<string>([Permission.WorkflowsRead.ToString()], StringComparer.OrdinalIgnoreCase),
            AllowUnscopedResourcesDuringTransition: allowUnscoped,
            StrictTenantMatch: strictTenantMatch,
            ReturnNotFoundForTenantMismatch: false);
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

    private static WorkflowAccessScope CreateWorkflowScope(string workflowId, int projectId, int organizationId)
    {
        return ProjectTenantAccessScopeFactory.CreateWorkflowScope(
            workflowId: workflowId,
            projectId: projectId,
            buildingId: null,
            organizationId: organizationId,
            ownerUserId: null,
            isTenantScoped: true,
            tenantScope: new TenantScope(organizationId, $"org-{organizationId}", IsActive: true));
    }

    private static EngineeringCalculationScenarioRecordDto CreateScenario(string scenarioId, int projectId)
    {
        return new EngineeringCalculationScenarioRecordDto(
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
    }

    private static EngineeringCalculationJobResultDto CreateJob(string jobId, int projectId, string scenarioId)
    {
        return new EngineeringCalculationJobResultDto(
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
    }

    private static EngineeringCalculationJobEventDto CreateEvent(string jobId, string scenarioId)
    {
        return new EngineeringCalculationJobEventDto(
            EventId: $"{jobId}-event",
            JobId: jobId,
            ScenarioId: scenarioId,
            Status: EngineeringCalculationJobStatus.Completed,
            Message: "Completed",
            ModuleKind: null,
            ProgressPercent: 100,
            Diagnostics: [],
            CreatedAtUtc: DateTimeOffset.UtcNow);
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

    private sealed class StubWorkflowScopeResolver : IWorkflowAccessScopeResolver
    {
        private readonly IReadOnlyDictionary<string, WorkflowAccessScope?> _scenarioScopes;
        private readonly IReadOnlyDictionary<string, WorkflowAccessScope?> _jobScopes;

        public StubWorkflowScopeResolver(
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

    private sealed class StubWorkflowPersistenceService : IEngineeringWorkflowPersistenceService
    {
        private readonly IReadOnlyDictionary<int, EngineeringWorkflowStateDto?> _states;
        private readonly IReadOnlyDictionary<string, EngineeringCalculationScenarioRecordDto?> _scenarios;

        public StubWorkflowPersistenceService(
            IReadOnlyDictionary<int, EngineeringWorkflowStateDto?>? states = null,
            IReadOnlyDictionary<string, EngineeringCalculationScenarioRecordDto?>? scenarios = null)
        {
            _states = states ?? new Dictionary<int, EngineeringWorkflowStateDto?>();
            _scenarios = scenarios ?? new Dictionary<string, EngineeringCalculationScenarioRecordDto?>();
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

        public Task<IReadOnlyList<EngineeringCalculationScenarioRecordDto>> ListProjectScenariosAsync(int projectId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<EngineeringCalculationScenarioRecordDto>>(Array.Empty<EngineeringCalculationScenarioRecordDto>());

        public Task<EngineeringWorkflowStateRecordDto> SaveWorkflowStateAsync(EngineeringWorkflowStateDto state, IReadOnlyList<EngineeringWorkflowDiagnosticDto>? validationDiagnostics, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<EngineeringCalculationScenarioRecordDto> SavePreparedScenarioAsync(EngineeringCalculationScenarioRequestDto scenarioRequest, EngineeringCalculationScenarioResultDto scenarioResult, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<EngineeringCalculationScenarioRecordDto> SaveRunScenarioAsync(EngineeringCalculationScenarioRequestDto scenarioRequest, EngineeringCalculationScenarioResultDto scenarioResult, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<EngineeringCalculationArtifactRecordDto>> ListScenarioArtifactsAsync(string scenarioId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<EngineeringCalculationArtifactRecordDto?> GetScenarioArtifactAsync(string scenarioId, EngineeringCalculationArtifactKind artifactKind, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class StubJobService : IEngineeringCalculationJobService
    {
        private readonly IReadOnlyDictionary<string, EngineeringCalculationJobResultDto?> _jobs;
        private readonly IReadOnlyDictionary<string, IReadOnlyList<EngineeringCalculationJobEventDto>> _eventsByJobId;

        public StubJobService(
            IReadOnlyDictionary<string, EngineeringCalculationJobResultDto?>? jobs = null,
            IReadOnlyDictionary<string, IReadOnlyList<EngineeringCalculationJobEventDto>>? eventsByJobId = null)
        {
            _jobs = jobs ?? new Dictionary<string, EngineeringCalculationJobResultDto?>();
            _eventsByJobId = eventsByJobId ?? new Dictionary<string, IReadOnlyList<EngineeringCalculationJobEventDto>>();
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
            return Task.FromResult<IReadOnlyList<EngineeringCalculationJobResultDto>>(Array.Empty<EngineeringCalculationJobResultDto>());
        }

        public Task<IReadOnlyList<EngineeringCalculationJobEventDto>> ListJobEventsAsync(string jobId, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            _eventsByJobId.TryGetValue(jobId, out var events);
            return Task.FromResult<IReadOnlyList<EngineeringCalculationJobEventDto>>(events ?? Array.Empty<EngineeringCalculationJobEventDto>());
        }

        public Task<EngineeringCalculationJobResultDto> CreateOrRunJobAsync(EngineeringCalculationJobRequestDto request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<EngineeringCalculationJobResultDto?> ExecuteQueuedJobAsync(string jobId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<EngineeringCalculationJobResultDto?> ExecuteClaimedJobAsync(string jobId, string workerId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<EngineeringCalculationJobResultDto?> CancelJobAsync(string jobId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
