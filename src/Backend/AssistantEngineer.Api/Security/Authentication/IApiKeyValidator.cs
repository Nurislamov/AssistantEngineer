namespace AssistantEngineer.Api.Security.Authentication;

public interface IApiKeyValidator
{
    Task<ApiKeyValidationResult> ValidateAsync(
        string apiKey,
        CancellationToken cancellationToken = default);
}
