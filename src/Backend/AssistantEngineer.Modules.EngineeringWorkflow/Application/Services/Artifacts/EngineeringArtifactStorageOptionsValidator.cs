using AssistantEngineer.Modules.EngineeringWorkflow.Application.Contracts.Artifacts;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Modules.EngineeringWorkflow.Application.Services.Artifacts;

public sealed class EngineeringArtifactStorageOptionsValidator : IValidateOptions<EngineeringArtifactStorageOptions>
{
    public ValidateOptionsResult Validate(string? name, EngineeringArtifactStorageOptions options)
    {
        if (options.MaxArtifactBytes <= 0)
        {
            return ValidateOptionsResult.Fail($"{EngineeringArtifactStorageOptions.SectionName}:MaxArtifactBytes must be positive.");
        }

        var provider = options.Provider?.Trim();
        if (string.IsNullOrWhiteSpace(provider))
        {
            return ValidateOptionsResult.Fail($"{EngineeringArtifactStorageOptions.SectionName}:Provider is required.");
        }

        if (!provider.Equals(EngineeringArtifactStorageProviders.InMemory, StringComparison.OrdinalIgnoreCase) &&
            !provider.Equals(EngineeringArtifactStorageProviders.FileSystem, StringComparison.OrdinalIgnoreCase))
        {
            return ValidateOptionsResult.Fail(
                $"{EngineeringArtifactStorageOptions.SectionName}:Provider must be '{EngineeringArtifactStorageProviders.InMemory}' or '{EngineeringArtifactStorageProviders.FileSystem}'.");
        }

        if (provider.Equals(EngineeringArtifactStorageProviders.FileSystem, StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(options.RootPath))
        {
            return ValidateOptionsResult.Fail(
                $"{EngineeringArtifactStorageOptions.SectionName}:RootPath is required when Provider is '{EngineeringArtifactStorageProviders.FileSystem}'.");
        }

        return ValidateOptionsResult.Success;
    }
}
