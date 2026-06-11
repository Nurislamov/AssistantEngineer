using System.Text;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Bot;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram;

public sealed class EquipmentDiagnosticTelegramResponseFormatter
{
    public string Format(EquipmentDiagnosticBotResponse response, int maxLength)
    {
        var builder = new StringBuilder();
        builder.AppendLine(response.Title);
        builder.AppendLine(response.Message);
        AppendSafety(builder, response);

        switch (response.Status)
        {
            case EquipmentDiagnosticBotResponseStatus.Answer:
                AppendAnswer(builder, response);
                break;
            case EquipmentDiagnosticBotResponseStatus.ClarificationRequired:
                AppendClarification(builder, response);
                break;
            case EquipmentDiagnosticBotResponseStatus.ReferenceOnly:
                builder.AppendLine("Reference-only indication; this is not a confirmed fault diagnosis.");
                AppendNextSteps(builder, response.OperatorNextSteps);
                break;
            case EquipmentDiagnosticBotResponseStatus.NotFound:
                builder.AppendLine("Verify the manufacturer, equipment family, display context, and exact service manual.");
                AppendNextSteps(builder, response.OperatorNextSteps);
                break;
            case EquipmentDiagnosticBotResponseStatus.Unsupported:
            case EquipmentDiagnosticBotResponseStatus.UnsafeOrOutOfScope:
                builder.AppendLine("This request is outside the supported deterministic diagnostic flow.");
                AppendNextSteps(builder, response.OperatorNextSteps);
                break;
        }

        return Truncate(builder.ToString().Trim(), maxLength);
    }

    public string FormatHelp(int maxLength) =>
        Truncate(
            "Equipment diagnostics\n" +
            "Send a manufacturer and displayed code, for example: Gree H5.\n" +
            "Optional context examples: Gree C5 outdoor; Gree C5 indoor; /diagnose Gree H5.\n" +
            "Guidance is deterministic and must be verified against the exact installed equipment and service manual.",
            maxLength);

    public string FormatValidation(IReadOnlyList<string> errors, int maxLength) =>
        Truncate($"Request not accepted. {string.Join(" ", errors)} Use /help for examples.", maxLength);

    public string FormatUnsupported(int maxLength) =>
        Truncate(
            "Unsupported command or controller model name. Send /help or provide a manufacturer and displayed diagnostic code.",
            maxLength);

    private static void AppendAnswer(StringBuilder builder, EquipmentDiagnosticBotResponse response)
    {
        if (response.VerificationRequired)
        {
            builder.AppendLine("Verification required before final conclusion.");
        }

        builder.AppendLine($"Confidence: {response.Confidence}.");
        if (response.SourceCard is not null)
        {
            builder.AppendLine($"Source: {response.SourceCard.SourceType} / {response.SourceCard.EvidenceLevel}.");
        }

        AppendNextSteps(builder, response.OperatorNextSteps);
    }

    private static void AppendClarification(StringBuilder builder, EquipmentDiagnosticBotResponse response)
    {
        if (response.ClarificationQuestion is null)
        {
            return;
        }

        builder.AppendLine(response.ClarificationQuestion.Prompt);
        foreach (var option in response.ClarificationQuestion.Options)
        {
            builder.AppendLine($"- {option.Label}: reply with {option.EquipmentSide} context.");
        }
    }

    private static void AppendNextSteps(StringBuilder builder, IReadOnlyList<string> steps)
    {
        if (steps.Count == 0)
        {
            return;
        }

        builder.AppendLine("Next steps:");
        foreach (var step in steps.Take(2))
        {
            builder.AppendLine($"- {Compact(step, 140)}");
        }
    }

    private static void AppendSafety(StringBuilder builder, EquipmentDiagnosticBotResponse response)
    {
        builder.AppendLine($"Safety: {response.SafetyCard.Boundary}");
        foreach (var note in response.SafetyCard.Notes.Take(1))
        {
            builder.AppendLine($"- {Compact(note, 140)}");
        }
    }

    private static string Compact(string text, int maxLength) =>
        text.Length <= maxLength ? text : string.Concat(text.AsSpan(0, maxLength - 3).TrimEnd(), "...");

    private static string Truncate(string text, int maxLength)
    {
        var effectiveMax = Math.Max(80, maxLength);
        if (text.Length <= effectiveMax)
        {
            return text;
        }

        const string suffix = "\n[Response shortened; use the deterministic bot API for full details.]";
        return string.Concat(text.AsSpan(0, effectiveMax - suffix.Length).TrimEnd(), suffix);
    }
}
