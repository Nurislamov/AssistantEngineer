using System.Text;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;

internal static class EquipmentDiagnosticCodeInputNormalizer
{
    public static string Normalize(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormKC);
        var builder = new StringBuilder(normalized.Length);

        foreach (var character in normalized)
        {
            builder.Append(NormalizeVisualEquivalent(character));
        }

        return builder.ToString();
    }

    private static char NormalizeVisualEquivalent(char value) =>
        value switch
        {
            '\u0410' => 'A',
            '\u0430' => 'a',
            '\u0412' => 'B',
            '\u0432' => 'b',
            '\u0421' => 'C',
            '\u0441' => 'c',
            '\u0415' => 'E',
            '\u0435' => 'e',
            '\u041d' => 'H',
            '\u043d' => 'h',
            '\u041a' => 'K',
            '\u043a' => 'k',
            '\u041c' => 'M',
            '\u043c' => 'm',
            '\u041e' => 'O',
            '\u043e' => 'o',
            '\u0420' => 'P',
            '\u0440' => 'p',
            '\u0422' => 'T',
            '\u0442' => 't',
            '\u0425' => 'X',
            '\u0445' => 'x',
            '\u0423' => 'Y',
            '\u0443' => 'y',
            // Domain-specific diagnostic-code input rule: users often type Cyrillic El for Latin L.
            '\u041b' => 'L',
            '\u043b' => 'l',
            _ => value
        };
}
