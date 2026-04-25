using Microsoft.Extensions.Configuration;

namespace AssistantEngineer.Infrastructure.Configuration;

internal static class ConfigurationSecurityValidator
{
    private static readonly string[] SensitiveKeys =
    [
        "ConnectionStrings:DefaultConnection",
        "EnergyPlus:DockerUri"
    ];

    public static void Validate(IConfiguration configuration, string environmentName)
    {
        if (string.Equals(environmentName, "Development", StringComparison.OrdinalIgnoreCase))
            return;

        if (configuration is not IConfigurationRoot root)
            return;

        var failures = new List<string>();

        foreach (var key in SensitiveKeys)
        {
            if (!TryGetEffectiveValue(root, key, out var provider, out var value) || string.IsNullOrWhiteSpace(value))
                continue;

            if (!IsJsonProvider(provider))
                continue;

            if (key.Equals("ConnectionStrings:DefaultConnection", StringComparison.OrdinalIgnoreCase) &&
                ContainsEmbeddedSecretInConnectionString(value))
            {
                failures.Add(
                    $"Sensitive setting '{key}' must not be loaded from appsettings JSON in {environmentName}. Use environment overrides or a secret provider.");
            }

            if (key.Equals("EnergyPlus:DockerUri", StringComparison.OrdinalIgnoreCase) &&
                Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
                !string.IsNullOrWhiteSpace(uri.UserInfo))
            {
                failures.Add(
                    $"Sensitive setting '{key}' must not embed credentials in appsettings JSON in {environmentName}. Use environment overrides or a secret provider.");
            }
        }

        if (failures.Count > 0)
            throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
    }

    private static bool TryGetEffectiveValue(
        IConfigurationRoot root,
        string key,
        out IConfigurationProvider provider,
        out string? value)
    {
        foreach (var candidate in root.Providers.Reverse())
        {
            if (candidate.TryGet(key, out value))
            {
                provider = candidate;
                return true;
            }
        }

        provider = null!;
        value = null;
        return false;
    }

    private static bool IsJsonProvider(IConfigurationProvider provider) =>
        provider.GetType().Name.Contains("Json", StringComparison.OrdinalIgnoreCase);

    private static bool ContainsEmbeddedSecretInConnectionString(string connectionString)
    {
        var segments = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var segment in segments)
        {
            var parts = segment.Split('=', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
                continue;

            if (parts[0].Equals("Password", StringComparison.OrdinalIgnoreCase) ||
                parts[0].Equals("Pwd", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
