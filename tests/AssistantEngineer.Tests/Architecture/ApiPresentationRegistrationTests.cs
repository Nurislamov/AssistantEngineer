using Microsoft.Extensions.Hosting;
using AssistantEngineer.Api.Configuration;
using AssistantEngineer.Api.Filters;
using AssistantEngineer.Api.Filters.Exceptions;
using AssistantEngineer.Api.Services.Calculations;
using AssistantEngineer.Api.Services.Calculations.Persistence;
using AssistantEngineer.Api.Services.Calculations.Persistence.Durable;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.Architecture;

public class ApiPresentationRegistrationTests
{
    [Fact]
    public void AddApiPresentationRegistersFiltersAndExceptionMapper()
    {
        var services = new ServiceCollection();

        services.AddLogging();
        services.AddApiPresentation();

        AssertServiceLifetime<ValidationFilter>(
            services,
            ServiceLifetime.Scoped);

        AssertServiceLifetime<GlobalExceptionFilter>(
            services,
            ServiceLifetime.Scoped);

        AssertServiceLifetime<IExceptionProblemDetailsMapper>(
            services,
            ServiceLifetime.Singleton);

        AssertServiceLifetime<EngineeringWorkflowMemoryStore>(
            services,
            ServiceLifetime.Singleton);

        AssertServiceLifetime<IEngineeringProjectRepository>(
            services,
            ServiceLifetime.Scoped);

        AssertServiceLifetime<IEngineeringWorkflowStateRepository>(
            services,
            ServiceLifetime.Scoped);

        AssertServiceLifetime<IEngineeringCalculationScenarioRepository>(
            services,
            ServiceLifetime.Scoped);

        AssertServiceLifetime<IEngineeringCalculationArtifactRepository>(
            services,
            ServiceLifetime.Scoped);

        AssertServiceLifetime<IEngineeringScenarioHistoryRepository>(
            services,
            ServiceLifetime.Scoped);

        AssertServiceLifetime<IEngineeringCalculationJobRepository>(
            services,
            ServiceLifetime.Scoped);

        AssertServiceLifetime<IEngineeringCalculationJobEventRepository>(
            services,
            ServiceLifetime.Scoped);

        AssertServiceLifetime<IEngineeringWorkflowPersistenceService>(
            services,
            ServiceLifetime.Scoped);

        AssertServiceLifetime<EngineeringWorkflowPersistenceDbContext>(
            services,
            ServiceLifetime.Scoped);

        AssertServiceLifetime<EngineeringWorkflowPersistenceDatabaseInitializer>(
            services,
            ServiceLifetime.Singleton);

        AssertServiceLifetime<IEngineeringCalculationScenarioModuleExecutor>(
            services,
            ServiceLifetime.Scoped);

        
                AssertServiceLifetime<IEngineeringCalculationWeatherSolarScenarioStep>(
            services,
            ServiceLifetime.Scoped);
        AssertServiceLifetime<IEngineeringCalculationVentilationScenarioStep>(
            services,
            ServiceLifetime.Scoped);        AssertServiceLifetime<IEngineeringCalculationGroundScenarioStep>(
            services,
            ServiceLifetime.Scoped);
        AssertServiceLifetime<IEngineeringCalculationDomesticHotWaterScenarioStep>(
            services,
            ServiceLifetime.Scoped);        AssertServiceLifetime<IEngineeringCalculationSystemEnergyScenarioStep>(
            services,
            ServiceLifetime.Scoped);AssertServiceLifetime<IEngineeringCalculationScenarioResultBuilder>(
            services,
            ServiceLifetime.Scoped);

        AssertServiceLifetime<IEngineeringCalculationScenarioRequestValidator>(
            services,
            ServiceLifetime.Scoped);
AssertServiceLifetime<IEngineeringCalculationScenarioRunner>(
            services,
            ServiceLifetime.Scoped);

        AssertServiceLifetime<IEngineeringCalculationJobService>(
            services,
            ServiceLifetime.Scoped);
        Assert.Contains(services, service =>
            service.ServiceType == typeof(IHostedService) &&
            service.ImplementationType == typeof(EngineeringCalculationJobWorker) &&
            service.Lifetime == ServiceLifetime.Singleton);
    }

    private static void AssertServiceLifetime<TService>(
        IServiceCollection services,
        ServiceLifetime expectedLifetime)
    {
        var descriptor = services.LastOrDefault(service =>
            service.ServiceType == typeof(TService));

        Assert.NotNull(descriptor);
        Assert.Equal(expectedLifetime, descriptor.Lifetime);
    }
}
