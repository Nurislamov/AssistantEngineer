namespace AssistantEngineer.Api.Configuration;

internal sealed class ApiHardeningOptions
{
    public const string SectionName = "ApiHardening";

    public ApiCorsOptions Cors { get; set; } = new();

    public ApiRateLimitingOptions RateLimiting { get; set; } = new();
}

internal sealed class ApiCorsOptions
{
    public bool Enabled { get; set; } = true;

    public string PolicyName { get; set; } = ApiHardeningRegistration.DefaultCorsPolicyName;

    public string[] AllowedOrigins { get; set; } = [];

    public string[] AllowedMethods { get; set; } =
    [
        "GET",
        "POST",
        "PUT",
        "PATCH",
        "DELETE",
        "OPTIONS"
    ];

    public string[] AllowedHeaders { get; set; } =
    [
        "Content-Type",
        "Authorization",
        "X-AssistantEngineer-Api-Key"
    ];
}

internal sealed class ApiRateLimitingOptions
{
    public bool Enabled { get; set; } = true;

    public int PermitLimit { get; set; } = 30;

    public int WindowSeconds { get; set; } = 60;

    public int QueueLimit { get; set; } = 0;

    public bool AutoReplenishment { get; set; } = true;

    public string DefaultPolicyName { get; set; } = ApiHardeningRegistration.DefaultRateLimitingPolicyName;

    public string HeavyPolicyName { get; set; } = ApiHardeningRegistration.EngineeringHeavyPolicyName;

    public int HeavyPermitLimit { get; set; } = 10;

    public int HeavyWindowSeconds { get; set; } = 60;
}
