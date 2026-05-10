using AssistantEngineer.Api.Filters;
using AssistantEngineer.Api.Filters.Exceptions;
using AssistantEngineer.Api.Services.Calculations;
using AssistantEngineer.Api.Services.Calculations.Persistence;
using AssistantEngineer.Api.Services.Calculations.Workflow;

namespace AssistantEngineer.Api.Configuration;

internal static class ApiPresentationRegistration
{
    public static IServiceCollection AddApiPresentation(
        this IServiceCollection services)
    {
        services.AddScoped<ValidationFilter>();
        services.AddScoped<GlobalExceptionFilter>();

        services.AddSingleton<IExceptionProblemDetailsMapper, ExceptionProblemDetailsMapper>();
        services.AddEngineeringWorkflowPersistence();
        services.AddEngineeringWorkflowServices();
        services.AddScoped<IEngineeringCalculationScenarioRunner, EngineeringCalculationScenarioRunner>();
        services.AddScoped<IEngineeringCalculationJobService, EngineeringCalculationJobService>();

        services.AddControllers();

        services.ConfigureOptions<ApiMvcOptionsSetup>();
        services.ConfigureOptions<ApiBehaviorOptionsSetup>();

        return services;
    }
}
