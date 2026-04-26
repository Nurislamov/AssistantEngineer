namespace AssistantEngineer.Api.Configuration;

internal static class ApiDocumentationRegistration
{
    public static IServiceCollection AddApiDocumentation(
        this IServiceCollection services)
    {
        services.AddOpenApi();

        return services;
    }
}