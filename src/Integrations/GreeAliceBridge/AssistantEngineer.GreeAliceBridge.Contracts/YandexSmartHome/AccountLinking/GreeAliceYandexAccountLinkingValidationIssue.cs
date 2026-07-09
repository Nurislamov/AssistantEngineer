namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome.AccountLinking;

public sealed record GreeAliceYandexAccountLinkingValidationIssue(
    string Code,
    string Message,
    string Severity,
    string Path);
