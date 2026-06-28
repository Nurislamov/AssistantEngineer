using System.Net;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;

internal static class TelegramHtml
{
    public const string ParseMode = "HTML";

    public static string Escape(string? value) =>
        WebUtility.HtmlEncode(value ?? string.Empty);

    public static string Bold(string? value) =>
        $"<b>{Escape(value)}</b>";
}
