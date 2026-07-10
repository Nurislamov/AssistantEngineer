namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.HttpSmoke;

public sealed record GreeAliceLocalHttpSmokeEndpoint(
    string EndpointId,
    string Method,
    string Path,
    string Purpose,
    bool IsLocalOnly,
    bool UsesDummyOrTemplateData,
    string ExpectedSafetyResult);
