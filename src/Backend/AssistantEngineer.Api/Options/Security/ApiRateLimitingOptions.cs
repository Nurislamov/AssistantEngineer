namespace AssistantEngineer.Api.Options.Security;

public sealed class ApiRateLimitingOptions
{
    public const string SectionName = "ApiRateLimiting";

    public bool Enabled { get; set; }

    public bool AllowRelaxedLimitsInDevelopment { get; set; } = true;

    public bool UseAuthenticatedPrincipalPartitioning { get; set; } = true;

    public bool UseOrganizationPartitioning { get; set; } = true;

    public bool UseApiKeyFingerprintPartitioning { get; set; } = true;

    public string DefaultPolicyName { get; set; } = "AssistantEngineerDefault";

    public int AnonymousPublicReadLimitPerMinute { get; set; } = 120;

    public int AnonymousCalculationRunLimitPerMinute { get; set; } = 10;

    public int AuthenticatedCalculationRunLimitPerMinute { get; set; } = 60;

    public int OrganizationCalculationRunLimitPerMinute { get; set; } = 300;

    public int WorkflowExecuteLimitPerMinute { get; set; } = 30;

    public int ReportGenerateLimitPerMinute { get; set; } = 20;
}
