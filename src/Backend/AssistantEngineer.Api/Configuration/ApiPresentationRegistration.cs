using AssistantEngineer.Api.Filters;
using AssistantEngineer.Api.Filters.Exceptions;
using AssistantEngineer.Api.Services.Calculations;

namespace AssistantEngineer.Api.Configuration;

internal static class ApiPresentationRegistration
{
    public static IServiceCollection AddApiPresentation(
        this IServiceCollection services)
    {
        services.AddScoped<ValidationFilter>();
        services.AddScoped<GlobalExceptionFilter>();

        services.AddSingleton<IExceptionProblemDetailsMapper, ExceptionProblemDetailsMapper>();
        services.AddScoped<IEngineeringCalculationScenarioRunner, EngineeringCalculationScenarioRunner>();

        services.AddControllers();

        services.ConfigureOptions<ApiMvcOptionsSetup>();
        services.ConfigureOptions<ApiBehaviorOptionsSetup>();

        return services;
    }
}
