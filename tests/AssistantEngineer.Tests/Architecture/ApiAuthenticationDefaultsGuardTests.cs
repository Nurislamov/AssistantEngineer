using System.Text.Json;
using AssistantEngineer.Api.Options.Security;

namespace AssistantEngineer.Tests.Architecture;

public sealed class ApiAuthenticationDefaultsGuardTests
{
    [Fact]
    public void ApiAuthenticationOptionsDefaultsPreserveCompatibility()
    {
        var options = new ApiAuthenticationOptions();

        Assert.True(
            !options.Enabled || options.AllowAnonymousInDevelopment,
            "Authentication defaults must preserve compatibility when rollout is incomplete.");
        Assert.Equal("X-AssistantEngineer-Api-Key", options.ApiKeyHeaderName);
    }

    [Fact]
    public void AppsettingsContainApiAuthenticationSectionWithCompatibilitySafeValues()
    {
        using var production = JsonDocument.Parse(File.ReadAllText(AppSettingsPath));
        using var development = JsonDocument.Parse(File.ReadAllText(AppSettingsDevelopmentPath));

        Assert.True(production.RootElement.TryGetProperty("ApiAuthentication", out var productionAuth));
        Assert.True(development.RootElement.TryGetProperty("ApiAuthentication", out var developmentAuth));

        var productionEnabled = productionAuth.GetProperty("Enabled").GetBoolean();
        var productionAllowAnonymous = productionAuth.GetProperty("AllowAnonymousInDevelopment").GetBoolean();
        Assert.True(!productionEnabled || productionAllowAnonymous);

        var developmentEnabled = developmentAuth.GetProperty("Enabled").GetBoolean();
        var developmentAllowAnonymous = developmentAuth.GetProperty("AllowAnonymousInDevelopment").GetBoolean();
        Assert.True(!developmentEnabled || developmentAllowAnonymous);
    }

    [Fact]
    public void AppsettingsDoNotContainPlaintextApiKeys()
    {
        var files = new[]
        {
            AppSettingsPath,
            AppSettingsDevelopmentPath
        };

        var violations = new List<string>();

        foreach (var file in files)
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file));
            if (!document.RootElement.TryGetProperty("Authentication", out var authentication))
            {
                continue;
            }

            if (!authentication.TryGetProperty("ApiKey", out var apiKeySection))
            {
                continue;
            }

            if (!apiKeySection.TryGetProperty("Key", out var keyElement))
            {
                continue;
            }

            if (keyElement.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(keyElement.GetString()))
            {
                violations.Add(Path.GetRelativePath(TestPaths.RepoRoot, file));
            }
        }

        Assert.True(violations.Count == 0, "Plaintext API keys must not be stored in appsettings files:\n" + string.Join('\n', violations));
    }

    [Fact]
    public void AuthenticationBoundaryDocumentStatesFutureProductionEnablement()
    {
        var content = File.ReadAllText(ApiAuthenticationBoundaryPath);

        Assert.Contains("Production target should require authenticated principal", content, StringComparison.OrdinalIgnoreCase);
    }

    private static string AppSettingsPath =>
        Path.Combine(TestPaths.ApiProjectPath, "appsettings.json");

    private static string AppSettingsDevelopmentPath =>
        Path.Combine(TestPaths.ApiProjectPath, "appsettings.Development.json");

    private static string ApiAuthenticationBoundaryPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "security", "api-authentication-boundary.md");
}
