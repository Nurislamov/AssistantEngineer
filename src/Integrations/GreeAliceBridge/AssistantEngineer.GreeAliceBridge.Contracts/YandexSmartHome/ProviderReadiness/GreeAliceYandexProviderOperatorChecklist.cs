namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.ProviderReadiness;

public sealed record GreeAliceYandexProviderOperatorChecklist(
    string OperatorApprovalStatus,
    IReadOnlyList<GreeAliceYandexProviderReadinessRequirement> Items);
