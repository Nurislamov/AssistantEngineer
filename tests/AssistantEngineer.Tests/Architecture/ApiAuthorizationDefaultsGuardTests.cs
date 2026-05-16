using System.Text.Json;
using AssistantEngineer.Api.Options.Security;

namespace AssistantEngineer.Tests.Architecture;

public sealed class ApiAuthorizationDefaultsGuardTests
{
    [Fact]
    public void ApiAuthorizationOptionsDefaultsPreserveCompatibility()
    {
        var options = new ApiAuthorizationOptions();

        Assert.False(options.Enabled);
        Assert.False(options.EnableEndpointProtectionPilot);
        Assert.False(options.EnableReadEndpointProtectionPilot);
        Assert.False(options.RequireProjectReadAuthorization);
        Assert.False(options.RequireBuildingReadAuthorization);
        Assert.False(options.EnableWriteEndpointProtectionPilot);
        Assert.False(options.RequireProjectWriteAuthorization);
        Assert.False(options.RequireBuildingWriteAuthorization);
        Assert.False(options.ReturnNotFoundForTenantMismatch);
        Assert.True(options.AllowAnonymousInDevelopment);
    }

    [Fact]
    public void AppsettingsContainCompatibilitySafeApiAuthorizationDefaults()
    {
        using var production = JsonDocument.Parse(File.ReadAllText(AppSettingsPath));
        using var development = JsonDocument.Parse(File.ReadAllText(AppSettingsDevelopmentPath));

        Assert.True(production.RootElement.TryGetProperty("ApiAuthorization", out var productionSection));
        Assert.True(development.RootElement.TryGetProperty("ApiAuthorization", out var developmentSection));

        Assert.False(productionSection.GetProperty("Enabled").GetBoolean());
        Assert.False(productionSection.GetProperty("EnableEndpointProtectionPilot").GetBoolean());
        Assert.False(productionSection.GetProperty("EnableReadEndpointProtectionPilot").GetBoolean());
        Assert.False(productionSection.GetProperty("RequireProjectReadAuthorization").GetBoolean());
        Assert.False(productionSection.GetProperty("RequireBuildingReadAuthorization").GetBoolean());
        Assert.False(productionSection.GetProperty("EnableWriteEndpointProtectionPilot").GetBoolean());
        Assert.False(productionSection.GetProperty("RequireProjectWriteAuthorization").GetBoolean());
        Assert.False(productionSection.GetProperty("RequireBuildingWriteAuthorization").GetBoolean());
        Assert.False(productionSection.GetProperty("ReturnNotFoundForTenantMismatch").GetBoolean());
        Assert.True(productionSection.GetProperty("AllowAnonymousInDevelopment").GetBoolean());

        Assert.False(developmentSection.GetProperty("Enabled").GetBoolean());
        Assert.False(developmentSection.GetProperty("EnableEndpointProtectionPilot").GetBoolean());
        Assert.False(developmentSection.GetProperty("EnableReadEndpointProtectionPilot").GetBoolean());
        Assert.False(developmentSection.GetProperty("RequireProjectReadAuthorization").GetBoolean());
        Assert.False(developmentSection.GetProperty("RequireBuildingReadAuthorization").GetBoolean());
        Assert.False(developmentSection.GetProperty("EnableWriteEndpointProtectionPilot").GetBoolean());
        Assert.False(developmentSection.GetProperty("RequireProjectWriteAuthorization").GetBoolean());
        Assert.False(developmentSection.GetProperty("RequireBuildingWriteAuthorization").GetBoolean());
        Assert.False(developmentSection.GetProperty("ReturnNotFoundForTenantMismatch").GetBoolean());
        Assert.True(developmentSection.GetProperty("AllowAnonymousInDevelopment").GetBoolean());
    }

    private static string AppSettingsPath =>
        Path.Combine(TestPaths.ApiProjectPath, "appsettings.json");

    private static string AppSettingsDevelopmentPath =>
        Path.Combine(TestPaths.ApiProjectPath, "appsettings.Development.json");
}
