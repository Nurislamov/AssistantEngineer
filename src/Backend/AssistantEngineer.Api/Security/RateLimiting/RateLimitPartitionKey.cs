namespace AssistantEngineer.Api.Security.RateLimiting;

public sealed record RateLimitPartitionKey(
    string PartitionType,
    string PartitionValue,
    string SafeDisplayValue);
