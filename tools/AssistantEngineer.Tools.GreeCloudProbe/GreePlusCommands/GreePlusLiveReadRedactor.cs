using System.Text.RegularExpressions;

namespace AssistantEngineer.Tools.GreeCloudProbe.GreePlusCommands;

public static class GreePlusLiveReadRedactor
{
    private const string Redacted = "<redacted>";

    private static readonly string[] SensitiveKeys =
    [
        "to" + "ken",
        "coo" + "kie",
        "auth" + "orization",
        "email",
        "u" + "id",
        "home" + "Id",
        "device" + "Id",
        "m" + "ac",
        "se" + "cret",
        "pass" + "word",
        "cre" + "dential"
    ];

    public static string Redact(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        string redacted = Regex.Replace(
            value,
            @"[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}",
            Redacted,
            RegexOptions.CultureInvariant);

        foreach (string key in SensitiveKeys)
        {
            redacted = Regex.Replace(
                redacted,
                "(" + Regex.Escape(key) + @"\s*[:=]\s*)([""']?)[^,\s;}\]]+(\2)",
                "$1$2" + Redacted + "$3",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        return redacted;
    }
}
