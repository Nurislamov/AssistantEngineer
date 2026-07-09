namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.ProviderReadiness;

public sealed record GreeAliceYandexProviderReadinessRequirement(
    string RequirementId,
    string Title,
    bool IsSatisfied,
    string Status,
    string ReviewArea);
