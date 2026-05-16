namespace AssistantEngineer.Api.Security.RateLimiting;

public interface IEndpointRateLimitCategoryResolver
{
    string ResolveCategory(HttpContext httpContext);
}
