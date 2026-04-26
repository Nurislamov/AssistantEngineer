namespace AssistantEngineer.Api.Configuration;

internal sealed class RequestLimitOptions
{
    public const string SectionName = "RequestLimits";

    public long MaxRequestBodyBytes { get; init; } = 1_048_576;

    public int DefaultTimeoutSeconds { get; init; } = 30;

    public int LongRunningTimeoutSeconds { get; init; } = 600;
}