using System.Globalization;
using System.Text;
using AssistantEngineer.Modules.Reporting.Application.Abstractions;
using AssistantEngineer.Modules.Reporting.Application.Contracts.Reports.Engineering;

namespace AssistantEngineer.Modules.Reporting.Application.Services;

internal sealed class EngineeringReportMarkdownExporter : IEngineeringReportMarkdownExporter
{
    public string Export(
        EngineeringReportDocument report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var builder = new StringBuilder();
        builder.AppendLine($"# {report.Title}");
        builder.AppendLine();
        builder.AppendLine($"- Report id: `{report.ReportId}`");
        builder.AppendLine($"- Report kind: `{report.ReportKind}`");
        builder.AppendLine($"- Generated (UTC): `{report.GeneratedTimestampUtc:O}`");
        builder.AppendLine($"- Schema version: `{report.SchemaVersion}`");
        builder.AppendLine();

        foreach (var section in report.Sections.OrderBy(item => item.Order))
        {
            builder.AppendLine($"## {section.Title}");
            if (!string.IsNullOrWhiteSpace(section.SummaryText))
            {
                builder.AppendLine();
                builder.AppendLine(section.SummaryText);
            }

            if (section.KeyValues.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("| Key | Value | Unit |");
                builder.AppendLine("|---|---:|---|");
                foreach (var value in section.KeyValues.OrderBy(item => item.Key, StringComparer.Ordinal))
                {
                    var renderedValue = FormatValue(value.Value, value.DisplayPrecision);
                    builder.AppendLine($"| {value.Label} | {renderedValue} | {value.Unit?.Symbol ?? string.Empty} |");
                }
            }

            foreach (var table in section.Tables.OrderBy(item => item.TableId, StringComparer.Ordinal))
            {
                builder.AppendLine();
                builder.AppendLine($"### {table.Title}");
                builder.AppendLine();
                builder.AppendLine($"| {string.Join(" | ", table.Columns)} |");
                builder.AppendLine($"| {string.Join(" | ", table.Columns.Select(_ => "---"))} |");
                foreach (var row in table.Rows)
                    builder.AppendLine($"| {string.Join(" | ", row.Select(EscapeCell))} |");

                foreach (var note in table.Notes.Where(note => !string.IsNullOrWhiteSpace(note)))
                    builder.AppendLine($"- Note: {note}");
            }

            if (section.Assumptions.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("### Assumptions");
                foreach (var item in section.Assumptions.OrderBy(item => item, StringComparer.Ordinal))
                    builder.AppendLine($"- {item}");
            }

            if (section.Diagnostics.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("### Diagnostics");
                foreach (var item in section.Diagnostics)
                    builder.AppendLine($"- `{item.Severity}` `{item.Code}` ({item.Module}): {item.Message}");
            }

            builder.AppendLine();
        }

        if (report.Assumptions.Count > 0)
        {
            builder.AppendLine("## Report Assumptions");
            foreach (var item in report.Assumptions.OrderBy(item => item, StringComparer.Ordinal))
                builder.AppendLine($"- {item}");
            builder.AppendLine();
        }

        if (report.Warnings.Count > 0)
        {
            builder.AppendLine("## Report Warnings");
            foreach (var item in report.Warnings.OrderBy(item => item, StringComparer.Ordinal))
                builder.AppendLine($"- {item}");
            builder.AppendLine();
        }

        if (report.Diagnostics.Count > 0)
        {
            builder.AppendLine("## Report Diagnostics");
            foreach (var item in report.Diagnostics)
                builder.AppendLine($"- `{item.Severity}` `{item.Code}` ({item.Module}): {item.Message}");
            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
    }

    private static string FormatValue(
        object? value,
        int precision)
    {
        return value switch
        {
            null => "n/a",
            double numeric => numeric.ToString($"F{Math.Clamp(precision, 0, 8)}", CultureInfo.InvariantCulture),
            float numeric => numeric.ToString($"F{Math.Clamp(precision, 0, 8)}", CultureInfo.InvariantCulture),
            decimal numeric => numeric.ToString($"F{Math.Clamp(precision, 0, 8)}", CultureInfo.InvariantCulture),
            bool boolean => boolean ? "true" : "false",
            _ => value.ToString() ?? string.Empty
        };
    }

    private static string EscapeCell(
        string? cell) =>
        (cell ?? string.Empty).Replace("|", "\\|", StringComparison.Ordinal);
}

