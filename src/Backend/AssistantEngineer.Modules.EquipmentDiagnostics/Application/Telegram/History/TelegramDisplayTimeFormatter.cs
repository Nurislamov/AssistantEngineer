using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.History;

public sealed class TelegramDisplayTimeFormatter
{
    public const string DefaultTimeZoneId = "Asia/Tashkent";

    private static readonly TimeSpan DefaultOffset = TimeSpan.FromHours(5);

    private readonly TimeProvider _timeProvider;
    private readonly ILogger<TelegramDisplayTimeFormatter> _logger;
    private readonly TimeZoneInfo _timeZone;

    public TelegramDisplayTimeFormatter(
        EquipmentDiagnosticTelegramOptions options,
        TimeProvider? timeProvider = null,
        ILogger<TelegramDisplayTimeFormatter>? logger = null)
    {
        _timeProvider = timeProvider ?? TimeProvider.System;
        _logger = logger ?? NullLogger<TelegramDisplayTimeFormatter>.Instance;
        _timeZone = ResolveTimeZone(options.DisplayTimeZone);
    }

    public string FormatRelative(DateTimeOffset value)
    {
        var local = Convert(value);
        var today = Convert(_timeProvider.GetUtcNow()).Date;
        if (local.Date == today)
        {
            return $"сегодня {local:HH:mm}";
        }

        if (local.Date == today.AddDays(-1))
        {
            return $"вчера {local:HH:mm}";
        }

        return local.ToString("dd.MM.yyyy HH:mm");
    }

    public string FormatAbsolute(DateTimeOffset value) =>
        Convert(value).ToString("dd.MM.yyyy HH:mm");

    private DateTimeOffset Convert(DateTimeOffset value) =>
        TimeZoneInfo.ConvertTime(value, _timeZone);

    private TimeZoneInfo ResolveTimeZone(string? configuredTimeZone)
    {
        if (string.IsNullOrWhiteSpace(configuredTimeZone))
        {
            _logger.LogWarning("Telegram display time zone is not configured. Falling back to default display time zone.");
            return DefaultTimeZone();
        }

        var id = configuredTimeZone.Trim();

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(id);
        }
        catch (TimeZoneNotFoundException)
        {
            _logger.LogWarning("Invalid Telegram display time zone configured. Falling back to default display time zone.");
            return DefaultTimeZone();
        }
        catch (InvalidTimeZoneException)
        {
            _logger.LogWarning("Invalid Telegram display time zone configured. Falling back to default display time zone.");
            return DefaultTimeZone();
        }
    }

    private static TimeZoneInfo DefaultTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(DefaultTimeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.CreateCustomTimeZone(DefaultTimeZoneId, DefaultOffset, DefaultTimeZoneId, DefaultTimeZoneId);
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.CreateCustomTimeZone(DefaultTimeZoneId, DefaultOffset, DefaultTimeZoneId, DefaultTimeZoneId);
        }
    }
}
