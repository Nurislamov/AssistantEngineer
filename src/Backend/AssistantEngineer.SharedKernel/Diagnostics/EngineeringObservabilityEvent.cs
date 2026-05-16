namespace AssistantEngineer.SharedKernel.Diagnostics;

public sealed record EngineeringObservabilityEvent(
    string EventCode,
    string Category,
    string Severity,
    IReadOnlyDictionary<string, string> Properties,
    DateTimeOffset TimestampUtc);
