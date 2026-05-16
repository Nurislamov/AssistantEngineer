namespace AssistantEngineer.Api.Security.RateLimiting;

public interface IRateLimitPartitionKeyProvider
{
    RateLimitPartitionKey GetPartitionKey(HttpContext httpContext);
}
