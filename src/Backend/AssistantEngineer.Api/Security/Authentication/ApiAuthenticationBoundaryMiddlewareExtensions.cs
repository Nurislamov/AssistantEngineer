namespace AssistantEngineer.Api.Security.Authentication;

public static class ApiAuthenticationBoundaryMiddlewareExtensions
{
    public static IApplicationBuilder UseApiAuthenticationBoundary(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ApiAuthenticationBoundaryMiddleware>();
    }
}
