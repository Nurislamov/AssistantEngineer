using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AssistantEngineer.Api;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.EngineeringWorkflow;
using AssistantEngineer.Api.Contracts.Common;
using AssistantEngineer.Api.Security.ApiKey;
using AssistantEngineer.Api.Security.Authentication;
using AssistantEngineer.Api.Security.Authorization;
using AssistantEngineer.Api.Security.TenantIsolation;
using AssistantEngineer.Api.Services.Calculations;
using AssistantEngineer.Api.Services.Calculations.Persistence;
using AssistantEngineer.Modules.Identity.Application.Contracts.Access;
using AssistantEngineer.Modules.Identity.Application.Services.Access;
using AssistantEngineer.Modules.Identity.Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AssistantEngineer.Tests.Api.Security.TenantIsolation;

public sealed class ProtectedWorkflowReadControllersTenantScopedQueryIntegrationTests
{
    private const string HeaderName = "X-AssistantEngineer-Api-Key";
    private const string ValidApiKey = "p5-16d-workflow-read-tenant-test-key";
    private const int TenantAOrganizationId = 1001;
    private const int TenantBOrganizationId = 1002;
    private const int TenantAProjectId = 10;
    private const int TenantBProjectId = 11;
    private const string TenantAScenarioId = "scenario-tenant-a";
    private const string TenantBScenarioId = "scenario-tenant-b";
    private const string TenantAJobId = "job-tenant-a";
    private const string TenantBJobId = "job-tenant-b";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task ProtectionDisabled_PreservesExistingWorkflowReadBehavior()
    {
        await using var factory = CreateFactory(
            apiAuthenticationEnabled: false,
            apiAuthorizationEnabled: false,
            returnNotFoundForTenantMismatch: false,
            principalPermissions: PermissionSet(Permission.WorkflowsRead));

        var client = factory.CreateClient();

        var state = await client.GetAsync($"/api/v1/engineering-workflow/{TenantAProjectId}/state");
        var scenario = await client.GetAsync($"/api/v1/engineering-workflow/scenarios/{TenantAScenarioId}");

        Assert.Equal(HttpStatusCode.OK, state.StatusCode);
        Assert.Equal(HttpStatusCode.OK, scenario.StatusCode);
    }

    [Fact]
    public async Task WorkflowState_SameTenant_Succeeds()
    {
        await using var factory = CreateProtectedFactory(
            permissions: PermissionSet(Permission.WorkflowsRead),
            returnNotFoundForTenantMismatch: false);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);

        var response = await client.GetAsync($"/api/v1/engineering-workflow/{TenantAProjectId}/state");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData(false, HttpStatusCode.Forbidden)]
    [InlineData(true, HttpStatusCode.NotFound)]
    public async Task WorkflowState_CrossTenant_RespectsNotFoundOption(bool returnNotFoundForTenantMismatch, HttpStatusCode expected)
    {
        await using var factory = CreateProtectedFactory(
            permissions: PermissionSet(Permission.WorkflowsRead),
            returnNotFoundForTenantMismatch: returnNotFoundForTenantMismatch);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);

        var response = await client.GetAsync($"/api/v1/engineering-workflow/{TenantBProjectId}/state");

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task ScenarioRead_SameTenant_Succeeds()
    {
        await using var factory = CreateProtectedFactory(
            permissions: PermissionSet(Permission.WorkflowsRead),
            returnNotFoundForTenantMismatch: false);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);

        var response = await client.GetAsync($"/api/v1/engineering-workflow/scenarios/{TenantAScenarioId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData(false, HttpStatusCode.Forbidden)]
    [InlineData(true, HttpStatusCode.NotFound)]
    public async Task ScenarioRead_CrossTenant_RespectsNotFoundOption(bool returnNotFoundForTenantMismatch, HttpStatusCode expected)
    {
        await using var factory = CreateProtectedFactory(
            permissions: PermissionSet(Permission.WorkflowsRead),
            returnNotFoundForTenantMismatch: returnNotFoundForTenantMismatch);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);

        var response = await client.GetAsync($"/api/v1/engineering-workflow/scenarios/{TenantBScenarioId}");

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task JobRead_SameTenant_Succeeds()
    {
        await using var factory = CreateProtectedFactory(
            permissions: PermissionSet(Permission.WorkflowsRead),
            returnNotFoundForTenantMismatch: false);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);

        var response = await client.GetAsync($"/api/v1/engineering-workflow/jobs/{TenantAJobId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData(false, HttpStatusCode.Forbidden)]
    [InlineData(true, HttpStatusCode.NotFound)]
    public async Task JobRead_CrossTenant_RespectsNotFoundOption(bool returnNotFoundForTenantMismatch, HttpStatusCode expected)
    {
        await using var factory = CreateProtectedFactory(
            permissions: PermissionSet(Permission.WorkflowsRead),
            returnNotFoundForTenantMismatch: returnNotFoundForTenantMismatch);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);

        var response = await client.GetAsync($"/api/v1/engineering-workflow/jobs/{TenantBJobId}");

        Assert.Equal(expected, response.StatusCode);
    }

    [Theory]
    [InlineData(false, HttpStatusCode.Forbidden)]
    [InlineData(true, HttpStatusCode.NotFound)]
    public async Task JobEvents_CrossTenant_DoesNotDiscloseResource(bool returnNotFoundForTenantMismatch, HttpStatusCode expected)
    {
        await using var factory = CreateProtectedFactory(
            permissions: PermissionSet(Permission.WorkflowsRead),
            returnNotFoundForTenantMismatch: returnNotFoundForTenantMismatch);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);

        var response = await client.GetAsync($"/api/v1/engineering-workflow/jobs/{TenantBJobId}/events");

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task ListScenariosByProject_ReturnsOnlySameTenantProjectScenarios()
    {
        await using var factory = CreateProtectedFactory(
            permissions: PermissionSet(Permission.WorkflowsRead),
            returnNotFoundForTenantMismatch: false);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);

        var response = await client.GetAsync($"/api/v1/engineering-workflow/{TenantAProjectId}/scenarios");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<PagedResponse<EngineeringCalculationScenarioRecordDto>>(JsonOptions);
        Assert.NotNull(payload);
        Assert.All(payload.Items, scenario => Assert.Equal(TenantAProjectId, scenario.ProjectId));
        Assert.DoesNotContain(payload.Items, scenario => scenario.ProjectId == TenantBProjectId);
    }

    [Fact]
    public async Task ListJobsByProject_ReturnsOnlySameTenantProjectJobs()
    {
        await using var factory = CreateProtectedFactory(
            permissions: PermissionSet(Permission.WorkflowsRead),
            returnNotFoundForTenantMismatch: false);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);

        var response = await client.GetAsync($"/api/v1/engineering-workflow/{TenantAProjectId}/jobs");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<PagedResponse<EngineeringCalculationJobResultDto>>(JsonOptions);
        Assert.NotNull(payload);
        Assert.All(payload.Items, job => Assert.Equal(TenantAProjectId, job.ProjectId));
        Assert.DoesNotContain(payload.Items, job => job.ProjectId == TenantBProjectId);
    }

    private static WorkflowProtectedReadFactory CreateProtectedFactory(
        IReadOnlySet<string> permissions,
        bool returnNotFoundForTenantMismatch)
    {
        return CreateFactory(
            apiAuthenticationEnabled: true,
            apiAuthorizationEnabled: true,
            returnNotFoundForTenantMismatch: returnNotFoundForTenantMismatch,
            principalPermissions: permissions);
    }

    private static WorkflowProtectedReadFactory CreateFactory(
        bool apiAuthenticationEnabled,
        bool apiAuthorizationEnabled,
        bool returnNotFoundForTenantMismatch,
        IReadOnlySet<string> principalPermissions)
    {
        var principal = new AuthenticatedPrincipal(
            UserId: 2001,
            OrganizationId: TenantAOrganizationId,
            ExternalSubjectId: "p5-16d-workflow-principal",
            AuthenticationScheme: ApiKeyAuthenticationHandler.SchemeName,
            Roles: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
            Permissions: principalPermissions,
            IsAuthenticated: true);

        return new WorkflowProtectedReadFactory(
            principal,
            apiAuthenticationEnabled,
            apiAuthorizationEnabled,
            returnNotFoundForTenantMismatch);
    }

    private static IReadOnlySet<string> PermissionSet(params Permission[] permissions)
    {
        return permissions
            .Select(permission => permission.ToString())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private sealed class WorkflowProtectedReadFactory : WebApplicationFactory<Program>
    {
        private readonly AuthenticatedPrincipal _principal;
        private readonly bool _apiAuthenticationEnabled;
        private readonly bool _apiAuthorizationEnabled;
        private readonly bool _returnNotFoundForTenantMismatch;

        public WorkflowProtectedReadFactory(
            AuthenticatedPrincipal principal,
            bool apiAuthenticationEnabled,
            bool apiAuthorizationEnabled,
            bool returnNotFoundForTenantMismatch)
        {
            _principal = principal;
            _apiAuthenticationEnabled = apiAuthenticationEnabled;
            _apiAuthorizationEnabled = apiAuthorizationEnabled;
            _returnNotFoundForTenantMismatch = returnNotFoundForTenantMismatch;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Testing");
            builder.UseSetting("ConnectionStrings:DefaultConnection", "Host=localhost;Port=5432;Database=AssistantEngineerTests;Username=postgres");
            builder.ConfigureAppConfiguration(configuration =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Port=5432;Database=AssistantEngineerTests;Username=postgres",
                    ["EngineeringWorkflowPersistence:Provider"] = "InMemory",
                    ["EnergyPlus:UseDocker"] = "false",
                    ["EnergyPlus:ExecutablePath"] = "energyplus",
                    ["Authentication:ApiKey:Enabled"] = "true",
                    ["Authentication:ApiKey:HeaderName"] = HeaderName,
                    ["Authentication:ApiKey:Key"] = ValidApiKey,
                    ["ApiAuthentication:Enabled"] = _apiAuthenticationEnabled ? "true" : "false",
                    ["ApiAuthentication:AllowAnonymousInDevelopment"] = "false",
                    ["ApiAuthentication:ApiKeyHeaderName"] = HeaderName,
                    ["ApiAuthentication:EnableApiKeyAuthentication"] = "true",
                    ["ApiAuthentication:EnableJwtBearerAuthentication"] = "false",
                    ["ApiAuthorization:Enabled"] = _apiAuthorizationEnabled ? "true" : "false",
                    ["ApiAuthorization:EnableWorkflowReadEndpointProtectionPilot"] = _apiAuthorizationEnabled ? "true" : "false",
                    ["ApiAuthorization:RequireWorkflowReadAuthorization"] = _apiAuthorizationEnabled ? "true" : "false",
                    ["ApiAuthorization:ReturnNotFoundForTenantMismatch"] = _returnNotFoundForTenantMismatch ? "true" : "false",
                    ["ApiAuthorization:ReturnNotFoundForWorkflowTenantMismatch"] = _returnNotFoundForTenantMismatch ? "true" : "false",
                    ["ApiAuthorization:AllowAnonymousInDevelopment"] = "false",
                    ["Identity:ProjectTenantAccess:AllowUnscopedProjectsDuringTransition"] = "true",
                    ["Identity:ProjectTenantAccess:EnableStrictTenantMatch"] = "true"
                });
            });

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IApiKeyValidator>();
                services.AddSingleton<IApiKeyValidator>(new StubApiKeyValidator(ValidApiKey, _principal));

                services.RemoveAll<IEngineeringWorkflowPersistenceService>();
                services.RemoveAll<IEngineeringCalculationJobService>();
                services.AddScoped<IEngineeringWorkflowPersistenceService>(_ => new StubWorkflowPersistenceService());
                services.AddScoped<IEngineeringCalculationJobService>(_ => new StubJobService());

                services.RemoveAll<IProjectReadAccessScopeResolver>();
                services.RemoveAll<IWorkflowAccessScopeResolver>();
                services.AddScoped<IProjectReadAccessScopeResolver>(_ => new FixedProjectScopeResolver());
                services.AddScoped<IWorkflowAccessScopeResolver>(_ => new FixedWorkflowScopeResolver());
            });
        }
    }

    private sealed class StubApiKeyValidator : IApiKeyValidator
    {
        private readonly string _expectedApiKey;
        private readonly AuthenticatedPrincipal _principal;

        public StubApiKeyValidator(string expectedApiKey, AuthenticatedPrincipal principal)
        {
            _expectedApiKey = expectedApiKey;
            _principal = principal;
        }

        public Task<ApiKeyValidationResult> ValidateAsync(string apiKey, CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;
            return Task.FromResult(
                string.Equals(apiKey, _expectedApiKey, StringComparison.Ordinal)
                    ? ApiKeyValidationResult.Success(_principal)
                    : ApiKeyValidationResult.Failure("InvalidApiKey"));
        }
    }

    private sealed class FixedProjectScopeResolver : IProjectReadAccessScopeResolver
    {
        public Task<ProjectAccessScope?> ResolveProjectScopeAsync(int projectId, CancellationToken cancellationToken)
        {
            _ = cancellationToken;

            return projectId switch
            {
                TenantAProjectId => Task.FromResult<ProjectAccessScope?>(
                    ProjectTenantAccessScopeFactory.CreateProjectScope(
                        projectId,
                        TenantAOrganizationId,
                        ownerUserId: null,
                        isTenantScoped: true,
                        tenantScope: new TenantScope(TenantAOrganizationId, $"org-{TenantAOrganizationId}", IsActive: true))),
                TenantBProjectId => Task.FromResult<ProjectAccessScope?>(
                    ProjectTenantAccessScopeFactory.CreateProjectScope(
                        projectId,
                        TenantBOrganizationId,
                        ownerUserId: null,
                        isTenantScoped: true,
                        tenantScope: new TenantScope(TenantBOrganizationId, $"org-{TenantBOrganizationId}", IsActive: true))),
                _ => Task.FromResult<ProjectAccessScope?>(null)
            };
        }
    }

    private sealed class FixedWorkflowScopeResolver : IWorkflowAccessScopeResolver
    {
        public Task<WorkflowAccessScope?> ResolveWorkflowScopeAsync(string workflowId, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            return Task.FromResult<WorkflowAccessScope?>(null);
        }

        public Task<WorkflowAccessScope?> ResolveScenarioScopeAsync(string scenarioId, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            return Task.FromResult<WorkflowAccessScope?>(scenarioId switch
            {
                TenantAScenarioId => CreateScope(scenarioId, TenantAProjectId, TenantAOrganizationId),
                TenantBScenarioId => CreateScope(scenarioId, TenantBProjectId, TenantBOrganizationId),
                _ => null
            });
        }

        public Task<WorkflowAccessScope?> ResolveJobScopeAsync(string jobId, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            return Task.FromResult<WorkflowAccessScope?>(jobId switch
            {
                TenantAJobId => CreateScope(jobId, TenantAProjectId, TenantAOrganizationId),
                TenantBJobId => CreateScope(jobId, TenantBProjectId, TenantBOrganizationId),
                _ => null
            });
        }

        private static WorkflowAccessScope CreateScope(string id, int projectId, int organizationId)
        {
            return ProjectTenantAccessScopeFactory.CreateWorkflowScope(
                id,
                projectId,
                buildingId: null,
                organizationId: organizationId,
                ownerUserId: null,
                isTenantScoped: true,
                tenantScope: new TenantScope(organizationId, $"org-{organizationId}", IsActive: true));
        }
    }

    private sealed class StubWorkflowPersistenceService : IEngineeringWorkflowPersistenceService
    {
        public EngineeringWorkflowPersistenceProviderInfo GetProviderInfo() =>
            new(EngineeringWorkflowPersistenceProvider.InMemory, DurableEnabled: false, ProviderLabel: "InMemory");

        public Task<EngineeringWorkflowStateDto?> GetLatestWorkflowStateAsync(
            int projectId,
            int? buildingId,
            CancellationToken cancellationToken)
        {
            _ = buildingId;
            _ = cancellationToken;
            return Task.FromResult(projectId switch
            {
                TenantAProjectId => CreateState(TenantAProjectId),
                TenantBProjectId => CreateState(TenantBProjectId),
                _ => null
            });
        }

        public Task<EngineeringCalculationScenarioRecordDto?> GetScenarioAsync(
            string scenarioId,
            CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            return Task.FromResult(scenarioId switch
            {
                TenantAScenarioId => CreateScenario(TenantAScenarioId, TenantAProjectId),
                TenantBScenarioId => CreateScenario(TenantBScenarioId, TenantBProjectId),
                _ => null
            });
        }

        public Task<IReadOnlyList<EngineeringCalculationScenarioRecordDto>> ListProjectScenariosAsync(
            int projectId,
            CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            return Task.FromResult<IReadOnlyList<EngineeringCalculationScenarioRecordDto>>(projectId switch
            {
                TenantAProjectId => [CreateScenario(TenantAScenarioId, TenantAProjectId)],
                TenantBProjectId => [CreateScenario(TenantBScenarioId, TenantBProjectId)],
                _ => Array.Empty<EngineeringCalculationScenarioRecordDto>()
            });
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
        public Task<EngineeringCalculationJobResultDto?> GetJobAsync(string jobId, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            return Task.FromResult(jobId switch
            {
                TenantAJobId => CreateJob(TenantAJobId, TenantAProjectId, TenantAScenarioId),
                TenantBJobId => CreateJob(TenantBJobId, TenantBProjectId, TenantBScenarioId),
                _ => null
            });
        }

        public Task<IReadOnlyList<EngineeringCalculationJobResultDto>> ListProjectJobsAsync(int projectId, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            return Task.FromResult<IReadOnlyList<EngineeringCalculationJobResultDto>>(projectId switch
            {
                TenantAProjectId => [CreateJob(TenantAJobId, TenantAProjectId, TenantAScenarioId)],
                TenantBProjectId => [CreateJob(TenantBJobId, TenantBProjectId, TenantBScenarioId)],
                _ => Array.Empty<EngineeringCalculationJobResultDto>()
            });
        }

        public Task<IReadOnlyList<EngineeringCalculationJobEventDto>> ListJobEventsAsync(string jobId, CancellationToken cancellationToken)
        {
            _ = cancellationToken;
            return Task.FromResult<IReadOnlyList<EngineeringCalculationJobEventDto>>(jobId switch
            {
                TenantAJobId => [CreateEvent(TenantAJobId, TenantAScenarioId)],
                TenantBJobId => [CreateEvent(TenantBJobId, TenantBScenarioId)],
                _ => Array.Empty<EngineeringCalculationJobEventDto>()
            });
        }

        public Task<EngineeringCalculationJobResultDto> CreateOrRunJobAsync(EngineeringCalculationJobRequestDto request, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<EngineeringCalculationJobResultDto?> ExecuteQueuedJobAsync(string jobId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<EngineeringCalculationJobResultDto?> ExecuteClaimedJobAsync(string jobId, string workerId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<EngineeringCalculationJobResultDto?> CancelJobAsync(string jobId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private static EngineeringWorkflowStateDto CreateState(int projectId)
    {
        return new EngineeringWorkflowStateDto(
            ProjectId: projectId,
            ProjectName: $"Workflow project {projectId}",
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
}
