namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.AccountLinking;

public sealed record GreeAliceYandexAccountLinkingValidationResult(
    bool IsAccepted,
    bool IsFailClosed,
    IReadOnlyList<GreeAliceYandexAccountLinkingValidationIssue> Issues)
{
    public static GreeAliceYandexAccountLinkingValidationResult Accepted { get; } = new(true, false, []);
}
