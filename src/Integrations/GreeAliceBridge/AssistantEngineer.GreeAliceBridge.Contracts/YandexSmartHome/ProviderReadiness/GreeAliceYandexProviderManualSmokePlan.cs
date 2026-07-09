namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.ProviderReadiness;

public sealed record GreeAliceYandexProviderManualSmokePlan(
    IReadOnlyList<GreeAliceYandexProviderReadinessRequirement> Steps,
    bool LiveCallsAllowed,
    string Status);
