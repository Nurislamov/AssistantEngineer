using AssistantEngineer.Api.Filters;
using AssistantEngineer.Api.Filters.Exceptions;

namespace AssistantEngineer.Api.Configuration;

internal static class ApiPresentationRegistration
{
    public static IServiceCollection AddApiPresentation(
        this IServiceCollection services)
    {
        services.AddScoped<ValidationFilter>();
        services.AddScoped<GlobalExceptionFilter>();

        services.AddSingleton<IExceptionProblemDetailsMapper, ExceptionProblemDetailsMapper>();

        services.AddControllers();

        services.ConfigureOptions<ApiMvcOptionsSetup>();
        services.ConfigureOptions<ApiBehaviorOptionsSetup>();

        return services;
    }
}