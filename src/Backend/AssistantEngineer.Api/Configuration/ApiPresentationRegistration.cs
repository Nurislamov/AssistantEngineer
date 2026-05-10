using AssistantEngineer.Api.Filters;
using AssistantEngineer.Api.Filters.Exceptions;
using AssistantEngineer.Api.Services.Calculations;
using AssistantEngineer.Api.Services.Calculations.Persistence;

namespace AssistantEngineer.Api.Configuration;

internal static class ApiPresentationRegistration
{
    public static IServiceCollection AddApiPresentation(
        this IServiceCollection services)
    {
        services.AddScoped<ValidationFilter>();
        services.AddScoped<GlobalExceptionFilter>();

        services.AddSingleton<IExceptionProblemDetailsMapper, ExceptionProblemDetailsMapper>();
        services.AddSingleton<EngineeringWorkflowMemoryStore>();
        services.AddScoped<IEngineeringProjectRepository, InMemoryEngineeringProjectRepository>();
        services.AddScoped<IEngineeringWorkflowStateRepository, InMemoryEngineeringWorkflowStateRepository>();
        services.AddScoped<IEngineeringCalculationScenarioRepository, InMemoryEngineeringCalculationScenarioRepository>();
        services.AddScoped<IEngineeringCalculationArtifactRepository, InMemoryEngineeringCalculationArtifactRepository>();
        services.AddScoped<IEngineeringScenarioHistoryRepository, InMemoryEngineeringScenarioHistoryRepository>();
        services.AddScoped<IEngineeringWorkflowPersistenceService, EngineeringWorkflowPersistenceService>();
        services.AddScoped<IEngineeringCalculationScenarioRunner, EngineeringCalculationScenarioRunner>();

        services.AddControllers();

        services.ConfigureOptions<ApiMvcOptionsSetup>();
        services.ConfigureOptions<ApiBehaviorOptionsSetup>();

        return services;
    }
}
