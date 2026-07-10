namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.HttpSmoke;

public sealed record GreeAliceLocalHttpSmokeRequest(
    string EndpointId,
    string Method,
    string Path,
    string? BodyJson,
    IReadOnlyDictionary<string, string> Headers,
    bool IsLocalOnly,
    bool UsesDummyOrTemplateData);
