using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;
using AssistantEngineer.Api.Security.Authorization;
using AssistantEngineer.Api.Security.TenantIsolation;
using AssistantEngineer.Api.Services.Calculations;
using AssistantEngineer.Api.Services.Calculations.Persistence;
using AssistantEngineer.Modules.Identity.Application.Contracts.Access;
using AssistantEngineer.Modules.Identity.Application.Services.Access;
using AssistantEngineer.Modules.Identity.Domain.Enums;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Tests.Api.Security.TenantIsolation;

public sealed class WorkflowTenantScopedReadServiceTests
{
    [Fact]
    public async Task SameTenantProjectWorkflowStateAllowed()
    {
        var service = CreateService(
            projectScopes: new Dictionary<int, ProjectAccessScope?>
            {
                [10] = CreateProjectScope(10, TenantIsolationScenario.TenantAOrganizationId)
            },
            workflowPersistence: new StubWorkflowPersistenceService(
                latestStateByProject: new Dictionary<int, EngineeringWorkflowStateDto?>
                {
                    [10] = CreateState(10)
                }));

        var result = await service.GetWorkflowStateForTenantAsync(10, null, TenantAWorkflowContext(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.PersistedState);
        Assert.Equal(10, result.Value.PersistedState!.ProjectId);
    }

    [Fact]
    public async Task CrossTenantProjectWorkflowStateDenied()
    {
        var service = CreateService(
            projectScopes: new Dictionary<int, ProjectAccessScope?>
            {
                [10] = CreateProjectScope(10, TenantIsolationScenario.TenantBOrganizationId)
            });

        var result = await service.GetWorkflowStateForTenantAsync(10, null, TenantAWorkflowContext(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Failure, result.ErrorType);
        Assert.Equal(TenantQueryFailureReasons.TenantMismatch, result.Error);
    }

    [Fact]
    public async Task CrossTenantWorkflowStateHonorsAntiEnumerationOption()
    {
        var service = CreateService(
            projectScopes: new Dictionary<int, ProjectAccessScope?>
            {
                [10] = CreateProjectScope(10, TenantIsolationScenario.TenantBOrganizationId)
            });

        var result = await service.GetWorkflowStateForTenantAsync(
            10,
            null,
            TenantAWorkflowContext(returnNotFoundForTenantMismatch: true),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.NotFound, result.ErrorType);
    }

    [Fact]
    public async Task MissingWorkflowsReadDenied()
    {
        var service = CreateService(
            projectScopes: new Dictionary<int, ProjectAccessScope?>
            {
                [10] = CreateProjectScope(10, TenantIsolationScenario.TenantAOrganizationId)
            });

        var result = await service.GetWorkflowStateForTenantAsync(10, null, TenantAContextWithoutWorkflowsRead(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(TenantQueryFailureReasons.MissingPermission, result.Error);
    }

    [Fact]
    public async Task LegacyUnscopedProjectFollowsCompatibilityOption()
    {
        var service = CreateService(
            projectScopes: new Dictionary<int, ProjectAccessScope?>
            {
                [10] = CreateProjectScope(10, organizationId: null, isTenantScoped: false)
            });

        var allowed = await service.GetWorkflowStateForTenantAsync(
            10,
            null,
            TenantAWorkflowContext(allowUnscopedResourcesDuringTransition: true),
            CancellationToken.None);
        var denied = await service.GetWorkflowStateForTenantAsync(
            10,
            null,
            TenantAWorkflowContext(allowUnscopedResourcesDuringTransition: false),
            CancellationToken.None);

        Assert.True(allowed.IsSuccess);
        Assert.True(denied.IsFailure);
        Assert.Equal(TenantQueryFailureReasons.UnscopedResourceDenied, denied.Error);
    }

    [Fact]
    public async Task ScenarioWithProjectMetadataSameTenantAllowed()
    {
        const string scenarioId = "scenario-tenant-a";
        var service = CreateService(
            projectScopes: new Dictionary<int, ProjectAccessScope?>
            {
                [10] = CreateProjectScope(10, TenantIsolationScenario.TenantAOrganizationId)
            },
            workflowScopes: new Dictionary<string, WorkflowAccessScope?>
            {
                [scenarioId] = CreateWorkflowScope(scenarioId, 10, TenantIsolationScenario.TenantAOrganizationId)
            },
            workflowPersistence: new StubWorkflowPersistenceService(
                scenarioById: new Dictionary<string, EngineeringCalculationScenarioRecordDto?>
                {
                    [scenarioId] = CreateScenario(scenarioId, 10)
                }));

        var result = await service.GetScenarioForTenantAsync(scenarioId, TenantAWorkflowContext(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(scenarioId, result.Value.ScenarioId);
    }

    [Fact]
    public async Task ScenarioWithProjectMetadataCrossTenantDenied()
    {
        const string scenarioId = "scenario-tenant-b";
        var service = CreateService(
            projectScopes: new Dictionary<int, ProjectAccessScope?>
            {
                [11] = CreateProjectScope(11, TenantIsolationScenario.TenantBOrganizationId)
            },
            workflowScopes: new Dictionary<string, WorkflowAccessScope?>
            {
                [scenarioId] = CreateWorkflowScope(scenarioId, 11, TenantIsolationScenario.TenantBOrganizationId)
            },
            workflowPersistence: new StubWorkflowPersistenceService(
                scenarioById: new Dictionary<string, EngineeringCalculationScenarioRecordDto?>
                {
                    [scenarioId] = CreateScenario(scenarioId, 11)
                }));

        var result = await service.GetScenarioForTenantAsync(scenarioId, TenantAWorkflowContext(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(TenantQueryFailureReasons.TenantMismatch, result.Error);
    }

    [Fact]
    public async Task JobWithProjectMetadataSameTenantAllowed()
    {
        const string jobId = "job-tenant-a";
        var service = CreateService(
            projectScopes: new Dictionary<int, ProjectAccessScope?>
            {
                [10] = CreateProjectScope(10, TenantIsolationScenario.TenantAOrganizationId)
            },
            workflowScopes: new Dictionary<string, WorkflowAccessScope?>
            {
                [jobId] = CreateWorkflowScope(jobId, 10, TenantIsolationScenario.TenantAOrganizationId)
            },
            jobService: new StubJobService(
                jobById: new Dictionary<string, EngineeringCalculationJobResultDto?>
                {
                    [jobId] = CreateJob(jobId, 10, "scenario-a")
                }));

        var result = await service.GetJobForTenantAsync(jobId, TenantAWorkflowContext(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(jobId, result.Value.JobId);
    }

    [Fact]
    public async Task JobWithProjectMetadataCrossTenantDenied()
    {
        const string jobId = "job-tenant-b";
        var service = CreateService(
            projectScopes: new Dictionary<int, ProjectAccessScope?>
            {
                [11] = CreateProjectScope(11, TenantIsolationScenario.TenantBOrganizationId)
            },
            workflowScopes: new Dictionary<string, WorkflowAccessScope?>
            {
                [jobId] = CreateWorkflowScope(jobId, 11, TenantIsolationScenario.TenantBOrganizationId)
            },
            jobService: new StubJobService(
                jobById: new Dictionary<string, EngineeringCalculationJobResultDto?>
                {
                    [jobId] = CreateJob(jobId, 11, "scenario-b")
                }));

        var result = await service.GetJobForTenantAsync(jobId, TenantAWorkflowContext(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(TenantQueryFailureReasons.TenantMismatch, result.Error);
    }

    [Fact]
    public async Task JobEventsRequireAuthorizedJob()
    {
        const string jobId = "job-events-cross-tenant";
        var service = CreateService(
            projectScopes: new Dictionary<int, ProjectAccessScope?>
            {
                [11] = CreateProjectScope(11, TenantIsolationScenario.TenantBOrganizationId)
            },
            workflowScopes: new Dictionary<string, WorkflowAccessScope?>
            {
                [jobId] = CreateWorkflowScope(jobId, 11, TenantIsolationScenario.TenantBOrganizationId)
            },
            jobService: new StubJobService(
                jobById: new Dictionary<string, EngineeringCalculationJobResultDto?>
                {
                    [jobId] = CreateJob(jobId, 11, "scenario-b")
                },
                eventsByJobId: new Dictionary<string, IReadOnlyList<EngineeringCalculationJobEventDto>>
                {
                    [jobId] = [CreateJobEvent(jobId, "scenario-b")]
                }));

        var result = await service.GetJobEventsForTenantAsync(jobId, TenantAWorkflowContext(), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(TenantQueryFailureReasons.TenantMismatch, result.Error);
    }

    [Fact]
    public async Task IncompleteMetadataFollowsStrictModeAndCompatibilityFallback()
    {
        const string scenarioId = "scenario-metadata-gap";
        var persistence = new StubWorkflowPersistenceService(
            scenarioById: new Dictionary<string, EngineeringCalculationScenarioRecordDto?>
            {
                [scenarioId] = CreateScenario(scenarioId, 10)
            });
        var service = CreateService(
            projectScopes: new Dictionary<int, ProjectAccessScope?>(),
            workflowScopes: new Dictionary<string, WorkflowAccessScope?>(),
            workflowPersistence: persistence);

        var strictDenied = await service.GetScenarioForTenantAsync(
            scenarioId,
            TenantAWorkflowContext(strictTenantMatch: true, allowUnscopedResourcesDuringTransition: true),
            CancellationToken.None);
        var compatibilityAllowed = await service.GetScenarioForTenantAsync(
            scenarioId,
            TenantAWorkflowContext(strictTenantMatch: false, allowUnscopedResourcesDuringTransition: true),
            CancellationToken.None);

        Assert.True(strictDenied.IsFailure);
        Assert.Equal(TenantQueryFailureReasons.MissingOrganization, strictDenied.Error);
        Assert.True(compatibilityAllowed.IsSuccess);
    }

    private static WorkflowTenantScopedReadService CreateService(
        IReadOnlyDictionary<int, ProjectAccessScope?>? projectScopes = null,
        IReadOnlyDictionary<string, WorkflowAccessScope?>? workflowScopes = null,
        StubWorkflowPersistenceService? workflowPersistence = null,
        StubJobService? jobService = null)
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

    private static ProjectAccessScope CreateProjectScope(
        int projectId,
        int? organizationId,
        bool isTenantScoped = true)
    {
        return ProjectTenantAccessScopeFactory.CreateProjectScope(
            projectId: projectId,
            organizationId: organizationId,
            ownerUserId: null,
            isTenantScoped: isTenantScoped,
            tenantScope: organizationId.HasValue
                ? new TenantScope(organizationId.Value, $"org-{organizationId.Value}", IsActive: true)
                : null);
    }

    private static WorkflowAccessScope CreateWorkflowScope(
        string workflowId,
        int projectId,
        int? organizationId)
    {
        return ProjectTenantAccessScopeFactory.CreateWorkflowScope(
            workflowId: workflowId,
            projectId: projectId,
            buildingId: null,
            organizationId: organizationId,
            ownerUserId: null,
            isTenantScoped: organizationId.HasValue,
            tenantScope: organizationId.HasValue
                ? new TenantScope(organizationId.Value, $"org-{organizationId.Value}", IsActive: true)
                : null);
    }

    private static TenantQueryContext TenantAWorkflowContext(
        bool allowUnscopedResourcesDuringTransition = true,
        bool strictTenantMatch = true,
        bool returnNotFoundForTenantMismatch = false)
    {
        return new TenantQueryContext(
            UserId: TenantIsolationScenario.TenantAUserId,
            OrganizationId: TenantIsolationScenario.TenantAOrganizationId,
            IsAuthenticated: true,
            Permissions: new HashSet<string>([Permission.WorkflowsRead.ToString()], StringComparer.OrdinalIgnoreCase),
            AllowUnscopedResourcesDuringTransition: allowUnscopedResourcesDuringTransition,
            StrictTenantMatch: strictTenantMatch,
            ReturnNotFoundForTenantMismatch: returnNotFoundForTenantMismatch);
    }

    private static TenantQueryContext TenantAContextWithoutWorkflowsRead()
    {
        return new TenantQueryContext(
            UserId: TenantIsolationScenario.TenantAUserId,
            OrganizationId: TenantIsolationScenario.TenantAOrganizationId,
            IsAuthenticated: true,
            Permissions: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            AllowUnscopedResourcesDuringTransition: true,
            StrictTenantMatch: true);
    }

    private static EngineeringWorkflowStateDto CreateState(int projectId)
    {
        return new EngineeringWorkflowStateDto(
            ProjectId: projectId,
            ProjectName: $"Project-{projectId}",
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

    private static EngineeringCalculationJobEventDto CreateJobEvent(string jobId, string scenarioId)
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
        private readonly IReadOnlyDictionary<int, EngineeringWorkflowStateDto?> _latestStateByProject;
        private readonly IReadOnlyDictionary<string, EngineeringCalculationScenarioRecordDto?> _scenarioById;
        private readonly IReadOnlyDictionary<int, IReadOnlyList<EngineeringCalculationScenarioRecordDto>> _scenariosByProject;

        public StubWorkflowPersistenceService(
            IReadOnlyDictionary<int, EngineeringWorkflowStateDto?>? latestStateByProject = null,
            IReadOnlyDictionary<string, EngineeringCalculationScenarioRecordDto?>? scenarioById = null,
            IReadOnlyDictionary<int, IReadOnlyList<EngineeringCalculationScenarioRecordDto>>? scenariosByProject = null)
        {
            _latestStateByProject = latestStateByProject ?? new Dictionary<int, EngineeringWorkflowStateDto?>();
            _scenarioById = scenarioById ?? new Dictionary<string, EngineeringCalculationScenarioRecordDto?>();
            _scenariosByProject = scenariosByProject ?? new Dictionary<int, IReadOnlyList<EngineeringCalculationScenarioRecordDto>>();
        }

        public EngineeringWorkflowPersistenceProviderInfo GetProviderInfo() =>
            new(EngineeringWorkflowPersistenceProvider.InMemory, DurableEnabled: false, ProviderLabel: "InMemory");

        public Task<EngineeringWorkflowStateDto?> GetLatestWorkflowStateAsync(
            int projectId,
            int? buildingId,
            CancellationToken cancellationToken)
        {
            _ = buildingId;
            _ = cancellationToken;
            _latestStateByProject.TryGetValue(projectId, out var state);
            return Task.FromResult(state);
        }

        public Task<EngineeringWorkflowStateRecordDto> SaveWorkflowStateAsync(
            EngineeringWorkflowStateDto state,
            IReadOnlyList<EngineeringWorkflowDiagnosticDto>? validationDiagnostics,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<EngineeringCalculationScenarioRecordDto> SavePreparedScenarioAsync(
            EngineeringCalculationScenarioRequestDto scenarioRequest,
            EngineeringCalculationScenarioResultDto scenarioResult,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<EngineeringCalculationScenarioRecordDto> SaveRunScenarioAsync(
            EngineeringCalculationScenarioRequestDto scenarioRequest,
            EngineeringCalculationScenarioResultDto scenarioResult,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<EngineeringCalculationScenarioRecordDto?> GetScenarioAsync(
            string scenarioId,
            CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            _scenarioById.TryGetValue(scenarioId, out var scenario);
            return Task.FromResult(scenario);
        }

        public Task<IReadOnlyList<EngineeringCalculationScenarioRecordDto>> ListProjectScenariosAsync(
            int projectId,
            CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            _scenariosByProject.TryGetValue(projectId, out var scenarios);
            return Task.FromResult<IReadOnlyList<EngineeringCalculationScenarioRecordDto>>(scenarios ?? Array.Empty<EngineeringCalculationScenarioRecordDto>());
        }

        public Task<IReadOnlyList<EngineeringCalculationArtifactRecordDto>> ListScenarioArtifactsAsync(
            string scenarioId,
            CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<EngineeringCalculationArtifactRecordDto?> GetScenarioArtifactAsync(
            string scenarioId,
            EngineeringCalculationArtifactKind artifactKind,
            CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class StubJobService : IEngineeringCalculationJobService
    {
        private readonly IReadOnlyDictionary<string, EngineeringCalculationJobResultDto?> _jobById;
        private readonly IReadOnlyDictionary<int, IReadOnlyList<EngineeringCalculationJobResultDto>> _jobsByProject;
        private readonly IReadOnlyDictionary<string, IReadOnlyList<EngineeringCalculationJobEventDto>> _eventsByJobId;

        public StubJobService(
            IReadOnlyDictionary<string, EngineeringCalculationJobResultDto?>? jobById = null,
            IReadOnlyDictionary<int, IReadOnlyList<EngineeringCalculationJobResultDto>>? jobsByProject = null,
            IReadOnlyDictionary<string, IReadOnlyList<EngineeringCalculationJobEventDto>>? eventsByJobId = null)
        {
            _jobById = jobById ?? new Dictionary<string, EngineeringCalculationJobResultDto?>();
            _jobsByProject = jobsByProject ?? new Dictionary<int, IReadOnlyList<EngineeringCalculationJobResultDto>>();
            _eventsByJobId = eventsByJobId ?? new Dictionary<string, IReadOnlyList<EngineeringCalculationJobEventDto>>();
        }

        public Task<EngineeringCalculationJobResultDto> CreateOrRunJobAsync(EngineeringCalculationJobRequestDto request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<EngineeringCalculationJobResultDto?> ExecuteQueuedJobAsync(string jobId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<EngineeringCalculationJobResultDto?> ExecuteClaimedJobAsync(string jobId, string workerId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<EngineeringCalculationJobResultDto?> CancelJobAsync(string jobId, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<EngineeringCalculationJobResultDto?> GetJobAsync(string jobId, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            _jobById.TryGetValue(jobId, out var job);
            return Task.FromResult(job);
        }

        public Task<IReadOnlyList<EngineeringCalculationJobResultDto>> ListProjectJobsAsync(int projectId, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            _jobsByProject.TryGetValue(projectId, out var jobs);
            return Task.FromResult<IReadOnlyList<EngineeringCalculationJobResultDto>>(jobs ?? Array.Empty<EngineeringCalculationJobResultDto>());
        }

        public Task<IReadOnlyList<EngineeringCalculationJobEventDto>> ListJobEventsAsync(string jobId, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            _eventsByJobId.TryGetValue(jobId, out var events);
            return Task.FromResult<IReadOnlyList<EngineeringCalculationJobEventDto>>(events ?? Array.Empty<EngineeringCalculationJobEventDto>());
        }
    }
}
