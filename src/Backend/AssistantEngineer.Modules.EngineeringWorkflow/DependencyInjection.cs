using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Abstractions.Artifacts;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.Artifacts;
using AssistantEngineer.Modules.EngineeringWorkflow.Application.Services.Artifacts;

namespace AssistantEngineer.Modules.EngineeringWorkflow;

public static class DependencyInjection
{
    public static IServiceCollection AddEngineeringWorkflowModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<EngineeringArtifactStorageOptions>()
            .Bind(configuration.GetSection(EngineeringArtifactStorageOptions.SectionName))
            .ValidateOnStart();
        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<EngineeringArtifactStorageOptions>, EngineeringArtifactStorageOptionsValidator>());
        services.AddScoped<IEngineeringArtifactStorage>(serviceProvider =>
        {
            var optionsAccessor = serviceProvider.GetRequiredService<IOptions<EngineeringArtifactStorageOptions>>();
            var options = optionsAccessor.Value;
            if (string.Equals(options.Provider, EngineeringArtifactStorageProviders.FileSystem, StringComparison.OrdinalIgnoreCase))
            {
                return new FileSystemEngineeringArtifactStorage(
                    optionsAccessor,
                    serviceProvider.GetService<Microsoft.Extensions.Logging.ILogger<FileSystemEngineeringArtifactStorage>>());
            }

            return new InMemoryEngineeringArtifactStorage(
                optionsAccessor,
                serviceProvider.GetService<Microsoft.Extensions.Logging.ILogger<InMemoryEngineeringArtifactStorage>>());
        });

        return services;
    }
}
