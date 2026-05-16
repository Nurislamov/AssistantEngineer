using System.Net;
using AssistantEngineer.Api;
using AssistantEngineer.Api.Security.ApiKey;
using AssistantEngineer.Api.Security.Authentication;
using AssistantEngineer.Infrastructure.Seeding;
using AssistantEngineer.Modules.Identity.Domain.Enums;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AssistantEngineer.Tests.Api;

public sealed class ProtectedEndpointPilotTests
{
    private const string Endpoint = "/api/v1/development/demo-data/seed";
    private const string HeaderName = "X-AssistantEngineer-Api-Key";
    private const string ValidApiKey = "pilot-endpoint-test-api-key";

    [Fact]
    public async Task PilotDisabled_PreservesExistingDevelopmentBehavior()
    {
        await using var factory = new ProtectedEndpointPilotFactory(
            environmentName: "Development",
            apiAuthenticationEnabled: false,
            apiAuthenticationAllowAnonymousInDevelopment: true,
            apiAuthorizationEnabled: false,
            enableEndpointProtectionPilot: false,
            apiAuthorizationAllowAnonymousInDevelopment: true,
            principalPermissions: new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        var client = factory.CreateClient();
        var response = await client.PostAsync(Endpoint, content: null);

        Assert.True(
            response.IsSuccessStatusCode,
            $"Pilot disabled should preserve endpoint behavior. Actual status: {(int)response.StatusCode}.");
    }

    [Fact]
    public async Task PilotEnabled_MissingCredentials_ReturnsUnauthorized()
    {
        await using var factory = new ProtectedEndpointPilotFactory(
            environmentName: "Development",
            apiAuthenticationEnabled: true,
            apiAuthenticationAllowAnonymousInDevelopment: false,
            apiAuthorizationEnabled: true,
            enableEndpointProtectionPilot: true,
            apiAuthorizationAllowAnonymousInDevelopment: false,
            principalPermissions: new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        var client = factory.CreateClient();
        var response = await client.PostAsync(Endpoint, content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PilotEnabled_AuthenticatedWithoutPermission_ReturnsForbidden()
    {
        await using var factory = new ProtectedEndpointPilotFactory(
            environmentName: "Development",
            apiAuthenticationEnabled: true,
            apiAuthenticationAllowAnonymousInDevelopment: false,
            apiAuthorizationEnabled: true,
            enableEndpointProtectionPilot: true,
            apiAuthorizationAllowAnonymousInDevelopment: false,
            principalPermissions: new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);

        var response = await client.PostAsync(Endpoint, content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task PilotEnabled_AuthenticatedWithAdministrationPermission_Succeeds()
    {
        await using var factory = new ProtectedEndpointPilotFactory(
            environmentName: "Development",
            apiAuthenticationEnabled: true,
            apiAuthenticationAllowAnonymousInDevelopment: false,
            apiAuthorizationEnabled: true,
            enableEndpointProtectionPilot: true,
            apiAuthorizationAllowAnonymousInDevelopment: false,
            principalPermissions: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Permission.AdministrationManage.ToString()
            });

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);

        var response = await client.PostAsync(Endpoint, content: null);

        Assert.True(
            response.IsSuccessStatusCode,
            $"Pilot enabled with AdministrationManage should succeed. Actual status: {(int)response.StatusCode}.");
    }

    [Fact]
    public async Task ProductionEnvironment_KeepsDevelopmentEndpointNotFound_EvenWhenAuthorized()
    {
        await using var factory = new ProtectedEndpointPilotFactory(
            environmentName: "Production",
            apiAuthenticationEnabled: true,
            apiAuthenticationAllowAnonymousInDevelopment: false,
            apiAuthorizationEnabled: true,
            enableEndpointProtectionPilot: true,
            apiAuthorizationAllowAnonymousInDevelopment: false,
            principalPermissions: new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Permission.AdministrationManage.ToString()
            });

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);

        var response = await client.PostAsync(Endpoint, content: null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task PilotAuthorizationFailures_DoNotDiscloseSecretsOrAuthInternals(HttpStatusCode expectedStatus)
    {
        await using var factory = new ProtectedEndpointPilotFactory(
            environmentName: "Development",
            apiAuthenticationEnabled: true,
            apiAuthenticationAllowAnonymousInDevelopment: false,
            apiAuthorizationEnabled: true,
            enableEndpointProtectionPilot: true,
            apiAuthorizationAllowAnonymousInDevelopment: false,
            principalPermissions: new HashSet<string>(StringComparer.OrdinalIgnoreCase));

        var client = factory.CreateClient();
        if (expectedStatus == HttpStatusCode.Forbidden)
        {
            client.DefaultRequestHeaders.Add(HeaderName, ValidApiKey);
        }

        var response = await client.PostAsync(Endpoint, content: null);
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.DoesNotContain("apiKey", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("authorization", body, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ProtectedEndpointPilotFactory : WebApplicationFactory<Program>
    {
        private readonly string _environmentName;
        private readonly bool _apiAuthenticationEnabled;
        private readonly bool _apiAuthenticationAllowAnonymousInDevelopment;
        private readonly bool _apiAuthorizationEnabled;
        private readonly bool _enableEndpointProtectionPilot;
        private readonly bool _apiAuthorizationAllowAnonymousInDevelopment;
        private readonly IReadOnlySet<string> _principalPermissions;

        public ProtectedEndpointPilotFactory(
            string environmentName,
            bool apiAuthenticationEnabled,
            bool apiAuthenticationAllowAnonymousInDevelopment,
            bool apiAuthorizationEnabled,
            bool enableEndpointProtectionPilot,
            bool apiAuthorizationAllowAnonymousInDevelopment,
            IReadOnlySet<string> principalPermissions)
        {
            _environmentName = environmentName;
            _apiAuthenticationEnabled = apiAuthenticationEnabled;
            _apiAuthenticationAllowAnonymousInDevelopment = apiAuthenticationAllowAnonymousInDevelopment;
            _apiAuthorizationEnabled = apiAuthorizationEnabled;
            _enableEndpointProtectionPilot = enableEndpointProtectionPilot;
            _apiAuthorizationAllowAnonymousInDevelopment = apiAuthorizationAllowAnonymousInDevelopment;
            _principalPermissions = principalPermissions;
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(_environmentName);
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
                    ["ApiAuthentication:AllowAnonymousInDevelopment"] = _apiAuthenticationAllowAnonymousInDevelopment ? "true" : "false",
                    ["ApiAuthentication:ApiKeyHeaderName"] = HeaderName,
                    ["ApiAuthentication:EnableApiKeyAuthentication"] = "true",
                    ["ApiAuthentication:EnableJwtBearerAuthentication"] = "false",
                    ["ApiAuthorization:Enabled"] = _apiAuthorizationEnabled ? "true" : "false",
                    ["ApiAuthorization:EnableEndpointProtectionPilot"] = _enableEndpointProtectionPilot ? "true" : "false",
                    ["ApiAuthorization:AllowAnonymousInDevelopment"] = _apiAuthorizationAllowAnonymousInDevelopment ? "true" : "false"
                });
            });

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IApiKeyValidator>();
                services.AddSingleton<IApiKeyValidator>(new StubApiKeyValidator(
                    expectedApiKey: ValidApiKey,
                    principalPermissions: _principalPermissions));

                services.RemoveAll<IDevelopmentDemoDataSeeder>();
                services.AddScoped<IDevelopmentDemoDataSeeder, StubDevelopmentDemoDataSeeder>();
            });
        }
    }

    private sealed class StubApiKeyValidator : IApiKeyValidator
    {
        private readonly string _expectedApiKey;
        private readonly IReadOnlySet<string> _principalPermissions;

        public StubApiKeyValidator(
            string expectedApiKey,
            IReadOnlySet<string> principalPermissions)
        {
            _expectedApiKey = expectedApiKey;
            _principalPermissions = principalPermissions;
        }

        public Task<ApiKeyValidationResult> ValidateAsync(string apiKey, CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;

            if (!string.Equals(apiKey, _expectedApiKey, StringComparison.Ordinal))
            {
                return Task.FromResult(ApiKeyValidationResult.Failure("InvalidApiKey"));
            }

            var principal = new AuthenticatedPrincipal(
                UserId: 1001,
                OrganizationId: 2001,
                ExternalSubjectId: "pilot-test-subject",
                AuthenticationScheme: ApiKeyAuthenticationHandler.SchemeName,
                Roles: new HashSet<string>(StringComparer.OrdinalIgnoreCase),
                Permissions: _principalPermissions,
                IsAuthenticated: true);

            return Task.FromResult(ApiKeyValidationResult.Success(principal));
        }
    }

    private sealed class StubDevelopmentDemoDataSeeder : IDevelopmentDemoDataSeeder
    {
        public Task<AssistantEngineer.SharedKernel.Primitives.Result<DevelopmentDemoSeedResult>> SeedAsync(
            CancellationToken cancellationToken = default)
        {
            _ = cancellationToken;

            return Task.FromResult(
                AssistantEngineer.SharedKernel.Primitives.Result<DevelopmentDemoSeedResult>.Success(
                    new DevelopmentDemoSeedResult
                    {
                        ClimateZoneId = 1,
                        ProjectId = 1,
                        BuildingId = 1,
                        FloorId = 1,
                        RoomId = 1,
                        WallId = 1,
                        WindowId = 1,
                        VentilationParametersId = 1,
                        WeatherYear = 2020,
                        EquipmentCatalogItemIds = new List<int> { 1, 2 }
                    }));
        }
    }
}
