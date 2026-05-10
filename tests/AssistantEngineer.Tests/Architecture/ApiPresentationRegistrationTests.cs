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

        AssertServiceLifetime<IEngineeringWorkflowPersistenceService>(
            services,
            ServiceLifetime.Scoped);

        AssertServiceLifetime<EngineeringWorkflowPersistenceDbContext>(
            services,
            ServiceLifetime.Scoped);

        AssertServiceLifetime<EngineeringWorkflowPersistenceDatabaseInitializer>(
            services,
            ServiceLifetime.Singleton);

        AssertServiceLifetime<IEngineeringCalculationScenarioRunner>(
            services,
            ServiceLifetime.Scoped);
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
