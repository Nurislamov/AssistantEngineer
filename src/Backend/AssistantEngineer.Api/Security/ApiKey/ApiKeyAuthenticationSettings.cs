namespace AssistantEngineer.Api.Security.ApiKey;

public sealed class ApiKeyAuthenticationSettings
{
    public const string SectionName = "Authentication:ApiKey";
    public const string DefaultHeaderName = "X-AssistantEngineer-Api-Key";

    public bool Enabled { get; set; } = true;

    public string HeaderName { get; set; } = DefaultHeaderName;

    public string? Key { get; set; }
}