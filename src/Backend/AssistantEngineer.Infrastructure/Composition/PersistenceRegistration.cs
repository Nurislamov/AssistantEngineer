using AssistantEngineer.Infrastructure.Persistence;
using AssistantEngineer.SharedKernel.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Infrastructure.Composition;

internal static class PersistenceRegistration
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        string environmentName)
    {
        services.AddAppDbContextPersistence(configuration, environmentName);

        services.AddScoped<IUnitOfWork>(sp =>
            sp.GetRequiredService<AppDbContext>());

        return services;
    }
}
