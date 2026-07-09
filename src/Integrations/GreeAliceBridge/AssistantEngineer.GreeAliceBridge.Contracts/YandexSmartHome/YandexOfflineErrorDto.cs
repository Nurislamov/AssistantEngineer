namespace AssistantEngineer.GreeAliceBridge.Contracts.YandexSmartHome;

public sealed record YandexOfflineErrorDto(
    string ErrorCode,
    string Message,
    string RuntimeMode);
