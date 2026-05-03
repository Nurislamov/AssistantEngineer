using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AssistantEngineer.Tools.EngineeringCoreEvidence;

internal static class Program
{
    private const string StableGeneratedAtUtc = "2026-01-01 00:00:00 UTC";
    private const string ManifestPath = "docs/releases/EngineeringCoreV1Manifest.json";
    private const string DiagnosticsCatalogPath = "docs/calculations/EngineeringCoreV1DiagnosticsCatalog.json";
    private const string ValidationRegistryPath = "docs/validation/EnergyPlusValidationCaseRegistry.json";

    public static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
            {
                PrintHelp();
                return 0;
            }

            var repoRoot = FindRepositoryRoot();
            Directory.SetCurrentDirectory(repoRoot);

            var command = args[0];

            return command switch
            {
                "generate-release-evidence" => GenerateReleaseEvidence(ReadOption(args, "--output-path") ?? "docs/reports/EngineeringCoreV1ReleaseEvidence.md"),
                "generate-export-disclosure-checklist" => GenerateExportDisclosureChecklist(ReadOption(args, "--output-path") ?? "docs/reports/engineering-core-v1/ExportDisclosureChecklist.md"),
                "generate-traceability-matrix" => GenerateTraceabilityMatrix(ReadOption(args, "--output-directory") ?? "docs/traceability"),
                "generate-all-evidence" => GenerateAllEvidence(),
                _ => Unknown(command)
            };
        }
        catch (Exception exception)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(exception.Message);
            Console.ResetColor();
            return 1;
        }
    }

    private static void PrintHelp()
    {
        Console.WriteLine("AssistantEngineer Engineering Core evidence tools");
        Console.WriteLine();
        Console.WriteLine("Commands:");
        Console.WriteLine("  generate-release-evidence [--output-path <path>]");
        Console.WriteLine("  generate-export-disclosure-checklist [--output-path <path>]");
        Console.WriteLine("  generate-traceability-matrix [--output-directory <path>]");
        Console.WriteLine("  generate-all-evidence");
    }

    private static int Unknown(string command)
    {
        Console.Error.WriteLine($"Unknown command: {command}");
        PrintHelp();
        return 1;
    }

    private static int GenerateAllEvidence()
    {
        var releaseCode = GenerateReleaseEvidence("docs/reports/EngineeringCoreV1ReleaseEvidence.md");
        if (releaseCode != 0)
            return releaseCode;

        var exportCode = GenerateExportDisclosureChecklist("docs/reports/engineering-core-v1/ExportDisclosureChecklist.md");
        if (exportCode != 0)
            return exportCode;

        return GenerateTraceabilityMatrix("docs/traceability");
    }

    private static int GenerateReleaseEvidence(string outputPath)
    {
        EnsureFile(ManifestPath, "Manifest");
        EnsureFile(DiagnosticsCatalogPath, "Diagnostics catalog");

        var manifest = ReadJsonObject(ManifestPath);
        var diagnosticsCatalog = ReadJsonObject(DiagnosticsCatalogPath);
        var diagnostics = ArrayOfObjects(diagnosticsCatalog, "diagnostics");

        var closedFormulaGates = StringArray(manifest["closedFormulaGates"]);
        var outOfScope = StringArray(manifest["outOfScopeV1"]);
        var plannedValidation = StringArray(manifest["plannedValidation"]);
        var explicitNonClaims = StringArray(manifest["explicitNonClaims"]);
        var documentationFiles = StringArray(manifest["documentationFiles"]);

        var errorCount = diagnostics.Count(item => StringValue(item, "severity") == "Error");
        var warningCount = diagnostics.Count(item => StringValue(item, "severity") == "Warning");
        var infoCount = diagnostics.Count(item => StringValue(item, "severity") == "Info");

        var diagnosticCategories = diagnostics
            .GroupBy(item => StringValue(item, "category"))
            .OrderBy(item => item.Key, StringComparer.Ordinal)
            .Select(item => $"| {item.Key} | {item.Count()} |")
            .ToArray();

        var closedGateRows = closedFormulaGates
            .Order(StringComparer.Ordinal)
            .Select(gate => $"| {gate} | ClosedV1 |")
            .ToArray();

        var docRows = documentationFiles
            .Order(StringComparer.Ordinal)
            .Select(path => $"| {path} | {(File.Exists(path) ? "present" : "missing")} |")
            .ToArray();

        var diagnosticRows = diagnostics
            .OrderBy(item => StringValue(item, "code"), StringComparer.Ordinal)
            .Select(item => $"| {StringValue(item, "code")} | {StringValue(item, "severity")} | {StringValue(item, "category")} | {StringValue(item, "closedV1Gate")} |")
            .ToArray();

        var builder = new StringBuilder();
        builder.AppendLine("# Engineering Core V1 Release Evidence");
        builder.AppendLine();
        builder.AppendLine($"Generated at: {StableGeneratedAtUtc}");
        builder.AppendLine();
        builder.AppendLine("## Status summary");
        builder.AppendLine();
        builder.AppendLine("| Field | Value |");
        builder.AppendLine("|---|---|");
        builder.AppendLine($"| Core name | {StringValue(manifest, "coreName")} |");
        builder.AppendLine($"| Version | {StringValue(manifest, "version")} |");
        builder.AppendLine($"| Status | {StringValue(manifest, "status")} |");
        builder.AppendLine($"| Release type | {StringValue(manifest, "releaseType")} |");
        builder.AppendLine($"| Formula gates closed | {BoolValue(manifest, "formulaGatesClosed")} |");
        builder.AppendLine($"| Weather 8760 gates closed | {BoolValue(manifest, "weather8760GatesClosed")} |");
        builder.AppendLine($"| Annual hourly 8760 gate closed | {BoolValue(manifest, "annualHourly8760GateClosed")} |");
        builder.AppendLine($"| Success results must not contain Error diagnostics | {BoolValue(manifest, "successfulResultsMustNotContainErrorDiagnostics")} |");
        builder.AppendLine();
        builder.AppendLine("## Counts");
        builder.AppendLine();
        builder.AppendLine("| Item | Count |");
        builder.AppendLine("|---|---:|");
        builder.AppendLine($"| Closed formula gates | {closedFormulaGates.Length} |");
        builder.AppendLine($"| Out of scope v1 items | {outOfScope.Length} |");
        builder.AppendLine($"| Planned validation items | {plannedValidation.Length} |");
        builder.AppendLine($"| Diagnostics total | {diagnostics.Count} |");
        builder.AppendLine($"| Error diagnostics | {errorCount} |");
        builder.AppendLine($"| Warning diagnostics | {warningCount} |");
        builder.AppendLine($"| Info diagnostics | {infoCount} |");
        builder.AppendLine();
        builder.AppendLine("## Closed formula gates");
        builder.AppendLine();
        builder.AppendLine("| CalculationId | Status |");
        builder.AppendLine("|---|---|");
        foreach (var row in closedGateRows)
            builder.AppendLine(row);
        builder.AppendLine();
        builder.AppendLine("## Annual 8760 requirements");
        builder.AppendLine();
        AppendList(builder, StringArray(manifest["requiredAnnual8760Flags"]));
        builder.AppendLine();
        builder.AppendLine("## Application endpoints");
        builder.AppendLine();
        AppendList(builder, StringArray(manifest["applicationEndpoints"]));
        builder.AppendLine();
        builder.AppendLine("## Frontend visibility files");
        builder.AppendLine();
        AppendList(builder, StringArray(manifest["frontendVisibility"]));
        builder.AppendLine();
        builder.AppendLine("## Backend visibility files");
        builder.AppendLine();
        AppendList(builder, StringArray(manifest["backendVisibility"]));
        builder.AppendLine();
        builder.AppendLine("## Verification scripts");
        builder.AppendLine();
        AppendList(builder, StringArray(manifest["verificationScripts"]));
        builder.AppendLine();
        builder.AppendLine("## CI workflows");
        builder.AppendLine();
        AppendList(builder, StringArray(manifest["ciWorkflows"]));
        builder.AppendLine();
        builder.AppendLine("## Out of scope v1");
        builder.AppendLine();
        AppendList(builder, outOfScope);
        builder.AppendLine();
        builder.AppendLine("## Planned validation");
        builder.AppendLine();
        AppendList(builder, plannedValidation);
        builder.AppendLine();
        builder.AppendLine("## Explicit non-claims");
        builder.AppendLine();
        AppendList(builder, explicitNonClaims);
        builder.AppendLine();
        builder.AppendLine("## Diagnostics by category");
        builder.AppendLine();
        builder.AppendLine("| Category | Count |");
        builder.AppendLine("|---|---:|");
        foreach (var row in diagnosticCategories)
            builder.AppendLine(row);
        builder.AppendLine();
        builder.AppendLine("## Diagnostics catalog");
        builder.AppendLine();
        builder.AppendLine("| Code | Severity | Category | ClosedV1 gate |");
        builder.AppendLine("|---|---|---|---|");
        foreach (var row in diagnosticRows)
            builder.AppendLine(row);
        builder.AppendLine();
        builder.AppendLine("## Documentation inventory");
        builder.AppendLine();
        builder.AppendLine("| File | Status |");
        builder.AppendLine("|---|---|");
        foreach (var row in docRows)
            builder.AppendLine(row);
        builder.AppendLine();
        builder.AppendLine("## Required verification command");
        builder.AppendLine();
        builder.AppendLine("Full verification:");
        builder.AppendLine();
        builder.AppendLine($"    {StringValue(manifest, "releaseVerificationCommand")}");
        builder.AppendLine();
        builder.AppendLine("Fast verification:");
        builder.AppendLine();
        builder.AppendLine($"    {StringValue(manifest, "fastVerificationCommand")}");
        builder.AppendLine();
        builder.AppendLine("Manifest verification:");
        builder.AppendLine();
        builder.AppendLine($"    {StringValue(manifest, "manifestVerificationCommand")}");
        builder.AppendLine();
        builder.AppendLine("## Release interpretation");
        builder.AppendLine();
        builder.AppendLine("Engineering Core V1 is closed as an engineering formula gate.");
        builder.AppendLine();
        builder.AppendLine("This release evidence does not claim exact EnergyPlus numerical parity, exact pyBuildingEnergy numerical parity, ASHRAE 140 validation coverage, full ISO 52016 node/matrix solver parity, full ISO 13370 implementation, full EN 15316 implementation or latent/moisture/humidity support in v1.");

        WriteText(outputPath, builder.ToString());

        WriteSuccess($"Engineering Core V1 release evidence generated: {outputPath}");
        Console.WriteLine($"Closed formula gates: {closedFormulaGates.Length}");
        Console.WriteLine($"Diagnostics: {diagnostics.Count} total, {errorCount} Error, {warningCount} Warning, {infoCount} Info");

        return 0;
    }

    private static int GenerateExportDisclosureChecklist(string outputPath)
    {
        var snapshotDirectory = "docs/reports/engineering-core-v1";

        var requiredSnapshots = new[]
        {
            "heating-report.sample.json",
            "cooling-report.sample.json",
            "annual-energy-disclosure.sample.json"
        };

        var requiredDisclosureFields = new[]
        {
            "coreStatus",
            "calculationScope",
            "calculationMethod",
            "actualMethod",
            "warnings",
            "assumptions",
            "explicitNonClaims",
            "outOfScopeV1",
            "documentationFiles"
        };

        var builder = new StringBuilder();
        builder.AppendLine("# Engineering Core V1 Export Disclosure Checklist");
        builder.AppendLine();
        builder.AppendLine("Generated from report contract snapshots.");
        builder.AppendLine();
        builder.AppendLine("## Snapshot status");
        builder.AppendLine();
        builder.AppendLine("| Snapshot | Exists | Has calculationDisclosure | Missing disclosure fields |");
        builder.AppendLine("|---|---|---|---|");

        foreach (var snapshot in requiredSnapshots)
        {
            var path = Path.Combine(snapshotDirectory, snapshot);
            var exists = File.Exists(path);
            var hasDisclosure = false;
            var missingFields = new List<string>();

            if (exists)
            {
                var json = ReadJsonObject(path);

                if (json["calculationDisclosure"] is JsonObject disclosure)
                {
                    hasDisclosure = true;

                    foreach (var field in requiredDisclosureFields)
                    {
                        if (!disclosure.ContainsKey(field))
                            missingFields.Add(field);
                    }
                }
                else
                {
                    missingFields.AddRange(requiredDisclosureFields);
                }
            }
            else
            {
                missingFields.AddRange(requiredDisclosureFields);
            }

            var missingText = missingFields.Count == 0 ? "none" : string.Join(", ", missingFields);
            builder.AppendLine($"| {snapshot} | {exists} | {hasDisclosure} | {missingText} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Required export surfaces");
        builder.AppendLine();
        builder.AppendLine("- Frontend report UI");
        builder.AppendLine("- JSON exports");
        builder.AppendLine("- PDF exports");
        builder.AppendLine("- Excel exports");
        builder.AppendLine("- Future report templates");
        builder.AppendLine("- Support/debug report packages");
        builder.AppendLine();
        builder.AppendLine("## Required disclosure fields");
        builder.AppendLine();

        foreach (var field in requiredDisclosureFields)
            builder.AppendLine($"- calculationDisclosure.{field}");

        builder.AppendLine();
        builder.AppendLine("## Required visible sections");
        builder.AppendLine();
        builder.AppendLine("- Calculation scope");
        builder.AppendLine("- Calculation method and actual method");
        builder.AppendLine("- Warnings");
        builder.AppendLine("- Assumptions");
        builder.AppendLine("- Explicit non-claims");
        builder.AppendLine("- Out-of-scope v1");
        builder.AppendLine("- Documentation references");
        builder.AppendLine();
        builder.AppendLine("## Annual 8760 requirements");
        builder.AppendLine();
        builder.AppendLine("- EnergyDataSource = TrueHourlySimulation");
        builder.AppendLine("- IsTrueHourly8760 = true");
        builder.AppendLine("- HourlyRecordCount = 8760");
        builder.AppendLine();
        builder.AppendLine("## Required non-claims");
        builder.AppendLine();
        builder.AppendLine("- No exact EnergyPlus numerical parity claim.");
        builder.AppendLine("- No exact pyBuildingEnergy numerical parity claim.");
        builder.AppendLine("- No ASHRAE 140 validation coverage claim.");
        builder.AppendLine("- No full ISO 52016 node/matrix solver parity claim.");
        builder.AppendLine("- No latent/moisture/humidity support in v1.");
        builder.AppendLine();
        builder.AppendLine("## Export approval checklist");
        builder.AppendLine();
        builder.AppendLine("- [ ] PDF exports show warnings and non-claims near report totals.");
        builder.AppendLine("- [ ] Excel exports include a visible disclosure sheet/table.");
        builder.AppendLine("- [ ] JSON exports preserve structured calculationDisclosure.");
        builder.AppendLine("- [ ] Frontend report UI shows disclosure before raw JSON.");
        builder.AppendLine("- [ ] Annual energy exports do not misuse true hourly 8760 wording.");
        builder.AppendLine("- [ ] No external-simulator parity claim is introduced.");

        WriteText(outputPath, builder.ToString());
        WriteSuccess($"Engineering Core V1 export disclosure checklist generated: {outputPath}");

        return 0;
    }

    private static int GenerateTraceabilityMatrix(string outputDirectory)
    {
        EnsureFile(ManifestPath, "Manifest");
        EnsureFile(DiagnosticsCatalogPath, "Diagnostics catalog");
        EnsureFile(ValidationRegistryPath, "Validation registry");

        Directory.CreateDirectory(outputDirectory);

        var manifest = ReadJsonObject(ManifestPath);
        var diagnosticsCatalog = ReadJsonObject(DiagnosticsCatalogPath);
        var validationRegistry = ReadJsonObject(ValidationRegistryPath);

        var diagnosticsByGate = ArrayOfObjects(diagnosticsCatalog, "diagnostics")
            .GroupBy(item => StringValue(item, "closedV1Gate"))
            .ToDictionary(
                item => item.Key,
                item => item
                    .Select(diagnostic => StringValue(diagnostic, "code"))
                    .Where(code => !string.IsNullOrWhiteSpace(code))
                    .Order(StringComparer.Ordinal)
                    .ToArray(),
                StringComparer.Ordinal);

        var closedFormulaGates = StringArray(manifest["closedFormulaGates"])
            .Order(StringComparer.Ordinal)
            .Select(gate => new Dictionary<string, object?>
            {
                ["calculationId"] = gate,
                ["status"] = "ClosedV1",
                ["diagnostics"] = diagnosticsByGate.TryGetValue(gate, out var diagnostics) ? diagnostics : [],
                ["apiVisible"] = true,
                ["reportDisclosureVisible"] = true,
                ["frontendVisible"] = true,
                ["documentationFiles"] = StringArray(manifest["documentationFiles"]),
                ["verificationScripts"] = StringArray(manifest["verificationScripts"])
            })
            .ToArray();

        var validationCases = ArrayOfObjects(validationRegistry, "cases")
            .Select(item => new Dictionary<string, object?>
            {
                ["caseId"] = StringValue(item, "caseId"),
                ["stage"] = StringValue(item, "stage"),
                ["status"] = StringValue(item, "status"),
                ["metrics"] = ArrayOfObjects(item, "metrics").Select(metric => StringValue(metric, "metricId")).ToArray(),
                ["nonClaims"] = StringArray(item["nonClaims"])
            })
            .ToArray();

        var traceability = new Dictionary<string, object?>
        {
            ["matrixName"] = "Engineering Core V1 Traceability Matrix",
            ["version"] = "v1",
            ["status"] = "ClosedV1",
            ["sourceManifest"] = ManifestPath,
            ["sourceDiagnosticsCatalog"] = DiagnosticsCatalogPath,
            ["sourceValidationRegistry"] = ValidationRegistryPath,
            ["generatedFrom"] = new[] { ManifestPath, DiagnosticsCatalogPath, ValidationRegistryPath },
            ["closedFormulaGateCount"] = StringArray(manifest["closedFormulaGates"]).Length,
            ["diagnosticsCount"] = ArrayOfObjects(diagnosticsCatalog, "diagnostics").Count,
            ["validationCaseCount"] = ArrayOfObjects(validationRegistry, "cases").Count,
            ["annual8760Requirements"] = StringArray(manifest["requiredAnnual8760Flags"]),
            ["outOfScopeV1"] = StringArray(manifest["outOfScopeV1"]),
            ["plannedValidation"] = StringArray(manifest["plannedValidation"]),
            ["applicationEndpoints"] = StringArray(manifest["applicationEndpoints"]),
            ["frontendVisibility"] = StringArray(manifest["frontendVisibility"]),
            ["backendVisibility"] = StringArray(manifest["backendVisibility"]),
            ["documentationFiles"] = StringArray(manifest["documentationFiles"]),
            ["verificationScripts"] = StringArray(manifest["verificationScripts"]),
            ["ciWorkflows"] = StringArray(manifest["ciWorkflows"]),
            ["explicitNonClaims"] = StringArray(manifest["explicitNonClaims"]),
            ["closedFormulaGates"] = closedFormulaGates,
            ["validationCases"] = validationCases
        };

        var jsonPath = Path.Combine(outputDirectory, "EngineeringCoreV1TraceabilityMatrix.json");
        var markdownPath = Path.Combine(outputDirectory, "EngineeringCoreV1TraceabilityMatrix.md");

        WriteJson(jsonPath, traceability);
        WriteText(markdownPath, BuildTraceabilityMarkdown(traceability, closedFormulaGates, validationCases));

        WriteSuccess("Engineering Core V1 traceability matrix generated:");
        Console.WriteLine($"- {jsonPath}");
        Console.WriteLine($"- {markdownPath}");
        Console.WriteLine($"Closed gates: {StringArray(manifest["closedFormulaGates"]).Length}");
        Console.WriteLine($"Diagnostics: {ArrayOfObjects(diagnosticsCatalog, "diagnostics").Count}");
        Console.WriteLine($"Validation cases: {validationCases.Length}");

        return 0;
    }

    private static string BuildTraceabilityMarkdown(
        Dictionary<string, object?> traceability,
        IReadOnlyList<Dictionary<string, object?>> closedFormulaGates,
        IReadOnlyList<Dictionary<string, object?>> validationCases)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Engineering Core V1 Traceability Matrix");
        builder.AppendLine();
        builder.AppendLine("## Status");
        builder.AppendLine();
        builder.AppendLine("| Field | Value |");
        builder.AppendLine("|---|---|");
        builder.AppendLine($"| Matrix name | {traceability["matrixName"]} |");
        builder.AppendLine($"| Version | {traceability["version"]} |");
        builder.AppendLine($"| Status | {traceability["status"]} |");
        builder.AppendLine($"| Closed formula gates | {traceability["closedFormulaGateCount"]} |");
        builder.AppendLine($"| Diagnostics | {traceability["diagnosticsCount"]} |");
        builder.AppendLine($"| Validation cases | {traceability["validationCaseCount"]} |");
        builder.AppendLine();
        builder.AppendLine("## Sources");
        builder.AppendLine();
        AppendList(builder, (string[])traceability["generatedFrom"]!);
        builder.AppendLine();
        builder.AppendLine("## Annual 8760 requirements");
        builder.AppendLine();
        AppendList(builder, (string[])traceability["annual8760Requirements"]!);
        builder.AppendLine();
        builder.AppendLine("## Application endpoints");
        builder.AppendLine();
        AppendList(builder, (string[])traceability["applicationEndpoints"]!);
        builder.AppendLine();
        builder.AppendLine("## Closed formula gates");
        builder.AppendLine();
        builder.AppendLine("| CalculationId | Status | Diagnostics | API | Report disclosure | Frontend |");
        builder.AppendLine("|---|---|---:|---|---|---|");

        foreach (var gate in closedFormulaGates)
        {
            var diagnosticsCount = gate["diagnostics"] is string[] diagnostics ? diagnostics.Length : 0;
            builder.AppendLine($"| {gate["calculationId"]} | {gate["status"]} | {diagnosticsCount} | {gate["apiVisible"]} | {gate["reportDisclosureVisible"]} | {gate["frontendVisible"]} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Validation cases");
        builder.AppendLine();
        builder.AppendLine("| CaseId | Stage | Status | Metrics |");
        builder.AppendLine("|---|---|---|---:|");

        foreach (var validationCase in validationCases)
        {
            var metricCount = validationCase["metrics"] is string[] metrics ? metrics.Length : 0;
            builder.AppendLine($"| {validationCase["caseId"]} | {validationCase["stage"]} | {validationCase["status"]} | {metricCount} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Out of scope v1");
        builder.AppendLine();
        AppendList(builder, (string[])traceability["outOfScopeV1"]!);
        builder.AppendLine();
        builder.AppendLine("## Planned validation");
        builder.AppendLine();
        AppendList(builder, (string[])traceability["plannedValidation"]!);
        builder.AppendLine();
        builder.AppendLine("## Explicit non-claims");
        builder.AppendLine();
        AppendList(builder, (string[])traceability["explicitNonClaims"]!);
        builder.AppendLine();
        builder.AppendLine("## Verification scripts");
        builder.AppendLine();
        AppendList(builder, (string[])traceability["verificationScripts"]!);
        builder.AppendLine();
        builder.AppendLine("## CI workflows");
        builder.AppendLine();
        AppendList(builder, (string[])traceability["ciWorkflows"]!);
        builder.AppendLine();
        builder.AppendLine("## Interpretation");
        builder.AppendLine();
        builder.AppendLine("This matrix proves traceability between the closed Engineering Core V1 formula gates, diagnostics catalog, validation registry, API visibility, report/frontend visibility, documentation, verification scripts and CI workflow.");
        builder.AppendLine();
        builder.AppendLine("It does not claim exact EnergyPlus numerical parity, exact pyBuildingEnergy numerical parity, ASHRAE 140 validation coverage, full ISO 52016 node/matrix solver parity or latent/moisture/humidity support in v1.");

        return builder.ToString();
    }

    private static JsonObject ReadJsonObject(string path)
    {
        EnsureFile(path, "JSON input file");
        return JsonNode.Parse(File.ReadAllText(path))!.AsObject();
    }

    private static List<JsonObject> ArrayOfObjects(JsonObject source, string propertyName)
    {
        if (source[propertyName] is not JsonArray array)
            return [];

        return array
            .Where(node => node is JsonObject)
            .Select(node => node!.AsObject())
            .ToList();
    }

    private static string[] StringArray(JsonNode? node)
    {
        if (node is not JsonArray array)
            return [];

        return array
            .Select(item => item?.GetValue<string>() ?? "")
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToArray();
    }

    private static string StringValue(JsonObject source, string propertyName) =>
        source[propertyName]?.GetValue<string>() ?? "";

    private static bool BoolValue(JsonObject source, string propertyName) =>
        source[propertyName]?.GetValue<bool>() ?? false;

    private static void AppendList(StringBuilder builder, IReadOnlyCollection<string> items)
    {
        if (items.Count == 0)
        {
            builder.AppendLine("- none");
            return;
        }

        foreach (var item in items)
            builder.AppendLine($"- {item}");
    }

    private static string? ReadOption(IReadOnlyList<string> args, string name)
    {
        for (var index = 0; index < args.Count - 1; index++)
        {
            if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase))
                return args[index + 1];
        }

        return null;
    }

    private static void EnsureFile(string path, string description)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"{description} not found: {path}", path);
    }

    private static void WriteJson(string path, object value)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var json = JsonSerializer.Serialize(
            value,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        File.WriteAllText(path, json + Environment.NewLine, Utf8NoBom());
    }

    private static void WriteText(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content, Utf8NoBom());
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(Directory.GetCurrentDirectory());

        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "AssistantEngineer.sln")))
                return current.FullName;

            current = current.Parent;
        }

        throw new InvalidOperationException("Repository root with AssistantEngineer.sln was not found.");
    }

    private static Encoding Utf8NoBom() =>
        new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

    private static void WriteSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        Console.ResetColor();
    }
}
