namespace AssistantEngineer.Api.Security.RateLimiting;

public static class RateLimitPartitionTypes
{
    public const string OrganizationId = "OrganizationId";
    public const string UserId = "UserId";
    public const string ApiKeyFingerprint = "ApiKeyFingerprint";
    public const string IpAddress = "IpAddress";
    public const string AnonymousUnknown = "AnonymousUnknown";
}
