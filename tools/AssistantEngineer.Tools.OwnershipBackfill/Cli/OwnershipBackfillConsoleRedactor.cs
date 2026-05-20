using System.Text.RegularExpressions;

namespace AssistantEngineer.Tools.OwnershipBackfill.Cli;

public static class OwnershipBackfillConsoleRedactor
{
    private static readonly string[] SecretOptionNames =
    [
        "--connection-string",
        "--password",
        "--token",
        "--api-key",
        "--apikey",
        "--secret"
    ];

    private static readonly Regex ConnectionStringLikePattern = new(
        "(Data Source|Host|Server|Username|User Id|UserID|Password)\\s*=\\s*[^;\\s]+",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public static string RedactText(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var redacted = input;
        redacted = ConnectionStringLikePattern.Replace(redacted, "$1=<redacted>");

        foreach (var optionName in SecretOptionNames)
        {
            redacted = RedactOptionValue(redacted, optionName);
        }

        return redacted;
    }

    private static string RedactOptionValue(string input, string optionName)
    {
        var pattern = $"{Regex.Escape(optionName)}\\s+[^\\s]+";
        return Regex.Replace(
            input,
            pattern,
            $"{optionName} <redacted>",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}

