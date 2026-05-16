namespace AssistantEngineer.Api.Options.Security;

public sealed class ApiAuthenticationOptions
{
    public const string SectionName = "ApiAuthentication";
    public const string DefaultApiKeyHeaderName = "X-AssistantEngineer-Api-Key";

    public bool Enabled { get; set; } = false;
    public bool AllowAnonymousInDevelopment { get; set; } = true;
    public string ApiKeyHeaderName { get; set; } = DefaultApiKeyHeaderName;
    public bool RequireHttps { get; set; }
    public bool EnableApiKeyAuthentication { get; set; } = true;
    public bool EnableJwtBearerAuthentication { get; set; }
    public string? JwtAuthority { get; set; }
    public string? JwtAudience { get; set; }
}
