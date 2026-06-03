using System.Reflection;
using System.Text.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge;
using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;

namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Json;

public sealed class EquipmentDiagnosticsKnowledgeJsonLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false
    };

    private static readonly string[] UnsafeTextFragments =
    [
        "bypass",
        "disable protection",
        "force run",
        "short protection",
        "ignore protection"
    ];

    public IReadOnlyCollection<EquipmentDiagnosticsKnowledgeEntry> LoadFromAssembly(Assembly assembly)
    {
        var resourceNames = assembly
            .GetManifestResourceNames()
            .Where(EquipmentDiagnosticsKnowledgeCatalog.IsKnowledgeJsonResource)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        if (resourceNames.Length == 0)
        {
            throw new InvalidOperationException(
                $"No embedded equipment diagnostics JSON knowledge resources were found in {assembly.GetName().Name}.");
        }

        return resourceNames
            .SelectMany(resourceName =>
            {
                using var stream = assembly.GetManifestResourceStream(resourceName)
                    ?? throw new InvalidOperationException($"Embedded resource '{resourceName}' was not found.");
                using var reader = new StreamReader(stream);
                return LoadFromJson(reader.ReadToEnd(), resourceName);
            })
            .ToArray();
    }

    public IReadOnlyCollection<EquipmentDiagnosticsKnowledgeEntry> LoadFromJson(
        string json,
        string sourceName)
    {
        var file = JsonSerializer.Deserialize<EquipmentDiagnosticsKnowledgeJsonFile>(json, JsonOptions)
            ?? throw new InvalidOperationException($"Knowledge file '{sourceName}' is empty or invalid.");

        if (file.Entries is null)
        {
            throw new InvalidOperationException($"Knowledge file '{sourceName}' must contain an entries array.");
        }

        return file.Entries
            .Select((entry, index) => MapEntry(entry, sourceName, index))
            .ToArray();
    }

    private static EquipmentDiagnosticsKnowledgeEntry MapEntry(
        EquipmentDiagnosticsKnowledgeJsonEntry entry,
        string sourceName,
        int index)
    {
        var path = $"{sourceName}:entries[{index}]";

        var category = ParseEnum<EquipmentCategory>(
            RequireText(entry.Category, path, "category"),
            path,
            "category");
        var confidence = ParseEnum<DiagnosticConfidence>(
            RequireText(entry.Confidence, path, "confidence"),
            path,
            "confidence");

        var mappedEntry = new EquipmentDiagnosticsKnowledgeEntry(
            Manufacturer: RequireText(entry.Manufacturer, path, "manufacturer"),
            SeriesName: NormalizeNullable(entry.SeriesName),
            ModelCode: NormalizeNullable(entry.ModelCode),
            Category: category,
            Code: RequireText(entry.Code, path, "code"),
            Title: RequireText(entry.Title, path, "title"),
            Meaning: RequireText(entry.Meaning, path, "meaning"),
            Severity: RequireText(entry.Severity, path, "severity"),
            Confidence: confidence,
            LikelyCauses: RequireTextArray(entry.LikelyCauses, path, "likelyCauses"),
            DiagnosticSteps: RequireSteps(entry.DiagnosticSteps, path),
            RequiredMeasurements: RequireMeasurements(entry.RequiredMeasurements, path),
            SafetyNotes: RequireTextArray(entry.SafetyNotes, path, "safetyNotes"),
            ManualReferences: RequireManualReferences(entry.ManualReferences, path),
            Tags: RequireTextArray(entry.Tags, path, "tags"));

        ValidateKnowledgeRules(mappedEntry, path);

        return mappedEntry;
    }

    private static IReadOnlyList<DiagnosticStep> RequireSteps(
        IReadOnlyList<EquipmentDiagnosticsKnowledgeJsonStep>? steps,
        string path)
    {
        if (steps is null)
        {
            throw new InvalidOperationException($"{path}.diagnosticSteps must be present.");
        }

        return steps
            .Select((step, index) =>
            {
                var stepPath = $"{path}.diagnosticSteps[{index}]";
                if (step.Order < 1)
                {
                    throw new InvalidOperationException($"{stepPath}.order must be greater than or equal to 1.");
                }

                return new DiagnosticStep(
                    Order: step.Order,
                    Title: RequireText(step.Title, stepPath, "title"),
                    Instruction: RequireText(step.Instruction, stepPath, "instruction"),
                    ExpectedResult: RequireText(step.ExpectedResult, stepPath, "expectedResult"),
                    IfFailedAction: RequireText(step.IfFailedAction, stepPath, "ifFailedAction"));
            })
            .ToArray();
    }

    private static IReadOnlyList<RequiredMeasurement> RequireMeasurements(
        IReadOnlyList<EquipmentDiagnosticsKnowledgeJsonMeasurement>? measurements,
        string path)
    {
        if (measurements is null)
        {
            throw new InvalidOperationException($"{path}.requiredMeasurements must be present.");
        }

        return measurements
            .Select((measurement, index) =>
            {
                var measurementPath = $"{path}.requiredMeasurements[{index}]";
                return new RequiredMeasurement(
                    Name: RequireText(measurement.Name, measurementPath, "name"),
                    Unit: RequireText(measurement.Unit, measurementPath, "unit"),
                    Description: RequireText(measurement.Description, measurementPath, "description"),
                    RequiredBeforeConclusion: measurement.RequiredBeforeConclusion);
            })
            .ToArray();
    }

    private static IReadOnlyList<ManualReference> RequireManualReferences(
        IReadOnlyList<EquipmentDiagnosticsKnowledgeJsonManualReference>? references,
        string path)
    {
        if (references is null)
        {
            throw new InvalidOperationException($"{path}.manualReferences must be present.");
        }

        return references
            .Select((reference, index) =>
            {
                var referencePath = $"{path}.manualReferences[{index}]";
                return new ManualReference(
                    Manufacturer: RequireText(reference.Manufacturer, referencePath, "manufacturer"),
                    ManualTitle: RequireText(reference.ManualTitle, referencePath, "manualTitle"),
                    ManualVersion: NormalizeNullable(reference.ManualVersion),
                    Page: NormalizeNullable(reference.Page),
                    Notes: NormalizeNullable(reference.Notes));
            })
            .ToArray();
    }

    private static IReadOnlyList<string> RequireTextArray(
        IReadOnlyList<string>? values,
        string path,
        string propertyName)
    {
        if (values is null)
        {
            throw new InvalidOperationException($"{path}.{propertyName} must be present.");
        }

        return values
            .Select((value, index) => RequireText(value, path, $"{propertyName}[{index}]"))
            .ToArray();
    }

    private static void ValidateKnowledgeRules(
        EquipmentDiagnosticsKnowledgeEntry entry,
        string path)
    {
        if (entry.Confidence == DiagnosticConfidence.ManualVerified)
        {
            throw new InvalidOperationException($"{path}.confidence must not be ManualVerified without explicit verified source evidence.");
        }

        if (entry.SafetyNotes.Count == 0)
        {
            throw new InvalidOperationException($"{path}.safetyNotes must contain at least one note.");
        }

        if (entry.DiagnosticSteps.Count == 0)
        {
            throw new InvalidOperationException($"{path}.diagnosticSteps must contain at least one step.");
        }

        if (entry.RequiredMeasurements.Count == 0)
        {
            throw new InvalidOperationException($"{path}.requiredMeasurements must contain at least one measurement.");
        }

        var combinedText = entry.SafetyNotes
            .Concat(entry.LikelyCauses)
            .Concat(entry.DiagnosticSteps.SelectMany(step => new[]
            {
                step.Title,
                step.Instruction,
                step.ExpectedResult,
                step.IfFailedAction
            }))
            .Concat(entry.RequiredMeasurements.SelectMany(measurement => new[]
            {
                measurement.Name,
                measurement.Unit,
                measurement.Description
            }))
            .Concat(entry.ManualReferences.SelectMany(reference => new[]
            {
                reference.Manufacturer,
                reference.ManualTitle,
                reference.ManualVersion ?? string.Empty,
                reference.Page ?? string.Empty,
                reference.Notes ?? string.Empty
            }))
            .Concat(entry.Tags ?? [])
            .ToArray();

        var unsafeFragments = UnsafeTextFragments
            .Where(fragment => combinedText.Any(text =>
                text.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        if (unsafeFragments.Length > 0)
        {
            throw new InvalidOperationException(
                $"{path} contains unsafe wording fragments: {string.Join(", ", unsafeFragments)}.");
        }
    }

    private static TEnum ParseEnum<TEnum>(
        string value,
        string path,
        string propertyName)
        where TEnum : struct, Enum
    {
        if (!Enum.TryParse<TEnum>(value, ignoreCase: false, out var parsed))
        {
            throw new InvalidOperationException(
                $"{path}.{propertyName} has unsupported value '{value}'. Allowed values: {string.Join(", ", Enum.GetNames<TEnum>())}.");
        }

        return parsed;
    }

    private static string RequireText(
        string? value,
        string path,
        string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{path}.{propertyName} must be present and non-empty.");
        }

        return value;
    }

    private static string? NormalizeNullable(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;
}
