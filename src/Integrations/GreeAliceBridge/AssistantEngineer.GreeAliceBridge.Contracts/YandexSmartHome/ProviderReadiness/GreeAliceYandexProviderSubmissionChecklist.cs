namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.ProviderReadiness;

public sealed record GreeAliceYandexProviderSubmissionChecklist(
    string SubmissionStatus,
    IReadOnlyList<GreeAliceYandexProviderReadinessRequirement> Items);
