namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.ProviderReadiness;

public sealed record GreeAliceYandexProviderSecurityChecklist(
    IReadOnlyList<GreeAliceYandexProviderReadinessRequirement> Items,
    string ApprovalStatus);
