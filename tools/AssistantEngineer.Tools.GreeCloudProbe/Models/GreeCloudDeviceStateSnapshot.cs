namespace AssistantEngineer.Tools.GreeCloudProbe.Models;

internal sealed record GreeCloudDeviceStateSnapshot(
    string? DeviceId,
    string? DeviceName,
    string Classification,
    bool? Online,
    string? Power,
    string? Mode,
    decimal? SetpointCelsius,
    decimal? IndoorTemperatureCelsius,
    string? FanSpeed,
    string? SwingVertical,
    string? SwingHorizontal,
    DateTimeOffset ObservedAtUtc,
    IReadOnlyDictionary<string, string?> SafeRawState)
{
    public bool HasAnyRuntimeState =>
        !string.IsNullOrWhiteSpace(Power) ||
        !string.IsNullOrWhiteSpace(Mode) ||
        SetpointCelsius is not null ||
        IndoorTemperatureCelsius is not null ||
        !string.IsNullOrWhiteSpace(FanSpeed) ||
        !string.IsNullOrWhiteSpace(SwingVertical) ||
        !string.IsNullOrWhiteSpace(SwingHorizontal);
}
