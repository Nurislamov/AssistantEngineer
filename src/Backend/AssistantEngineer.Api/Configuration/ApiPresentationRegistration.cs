using AssistantEngineer.Api.Filters;
using AssistantEngineer.Api.Filters.Exceptions;
using AssistantEngineer.Api.Services.Calculations;
using AssistantEngineer.Api.Services.Calculations.Idempotency;
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
        services.AddEngineeringIdempotency();
        services.AddEngineeringWorkflowServices();
        services.AddScoped<IEngineeringCalculationScenarioModuleExecutor, EngineeringCalculationScenarioModuleExecutor>();
        services.AddScoped<IEngineeringCalculationVentilationScenarioStep, EngineeringCalculationVentilationScenarioStep>();
        services.AddScoped<IEngineeringCalculationDomesticHotWaterScenarioStep, EngineeringCalculationDomesticHotWaterScenarioStep>();
        services.AddScoped<IEngineeringCalculationSystemEnergyScenarioStep, EngineeringCalculationSystemEnergyScenarioStep>();
        services.AddScoped<IEngineeringCalculationGroundScenarioStep, EngineeringCalculationGroundScenarioStep>();
        services.AddScoped<IEngineeringCalculationWeatherSolarScenarioStep, EngineeringCalculationWeatherSolarScenarioStep>();
        services.AddScoped<IEngineeringCalculationScenarioResultBuilder, EngineeringCalculationScenarioResultBuilder>();
        services.AddScoped<IEngineeringCalculationScenarioRequestValidator, EngineeringCalculationScenarioRequestValidator>();
        services.AddScoped<IEngineeringCalculationScenarioRunner, EngineeringCalculationScenarioRunner>();
        services.AddScoped<EngineeringCalculationJobPayloadCodec>();
        services.AddScoped<EngineeringCalculationJobStatusTransitionPolicy>();
        services.AddScoped<EngineeringCalculationJobEventRecorder>();
        services.AddScoped<IEngineeringCalculationJobService, EngineeringCalculationJobService>();
        services.AddOptions<EngineeringCalculationJobWorkerOptions>()
            .Configure<IConfiguration>((options, configuration) =>
            {
                configuration.GetSection(EngineeringCalculationJobWorkerOptions.SectionName).Bind(options);
            })
            .Validate(options => options.PollIntervalSeconds > 0, "Engineering calculation job worker poll interval must be positive.")
            .Validate(options => options.BatchSize > 0, "Engineering calculation job worker batch size must be positive.")
            .Validate(options => options.LeaseDurationSeconds > 0, "Engineering calculation job worker lease duration must be positive.");
        services.AddHostedService<EngineeringCalculationJobWorker>();

        services.AddControllers();

        services.ConfigureOptions<ApiMvcOptionsSetup>();
        services.ConfigureOptions<ApiBehaviorOptionsSetup>();

        return services;
    }
}
