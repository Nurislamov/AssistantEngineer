using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AssistantEngineer.Tools.EnergyPlusValidation;

internal static class EnergyPlusValidationToolRunner
{
    private const string StableGeneratedAtUtc = "2026-01-01 00:00:00 UTC";
    private const string RegistryPath = "docs/validation/EnergyPlusValidationCaseRegistry.json";
    private const string FixturesRoot = "tests/fixtures/validation/energyplus";
    private const string ReportsDirectory = "docs/reports/validation";

    public static int RegenerateValidationArtifacts()
    {
        GenerateValidationReadiness();
        GenerateSmoke001ComparisonReadiness();
        AssertSmoke001RealFixtureReady(requireRealFixture: false);
        CompareFixtures(requireRealReferences: false);
        GenerateComparisonSummary();
        GenerateFixtureCatalog();
        GenerateValidationEvidence();
        return 0;
    }

    public static int VerifyValidation()
    {
        var code = RegenerateValidationArtifacts();
        if (code != 0)
            return code;

        return RunProcess(
            "dotnet",
            "test .\\AssistantEngineer.sln --filter \"EnergyPlusValidation\"");
    }

    public static int CompareFixtures(bool requireRealReferences)
    {
        EnsureDirectory(FixturesRoot, "Validation fixtures root");

        var fixtureDirectories = Directory
            .GetDirectories(FixturesRoot)
            .Where(path => File.Exists(Path.Combine(path, "comparison-tolerances.json")))
            .OrderBy(path => Path.GetFileName(path), StringComparer.Ordinal)
            .ToArray();

        if (fixtureDirectories.Length == 0)
            throw new InvalidOperationException($"No validation fixture directories found under {FixturesRoot}.");

        Directory.CreateDirectory(ReportsDirectory);

        var caseResults = new List<Dictionary<string, object?>>();

        foreach (var fixtureDirectory in fixtureDirectories)
        {
            Console.WriteLine();
            WriteStep($"Compare validation fixture: {Path.GetFileName(fixtureDirectory)}");

            caseResults.Add(CompareFixture(fixtureDirectory, requireRealReferences));

            WriteSuccess($"OK: {Path.GetFileName(fixtureDirectory)}");
        }

        var summary = new Dictionary<string, object?>
        {
            ["summaryName"] = "Generic EnergyPlus Validation Fixture Comparison Summary",
            ["version"] = "v1",
            ["status"] = "PlannedValidation",
            ["runner"] = "GenericEnergyPlusValidationFixtureRunner",
            ["generatedAtUtc"] = StableGeneratedAtUtc,
            ["fixturesRoot"] = FixturesRoot,
            ["outputDirectory"] = ReportsDirectory,
            ["totals"] = new Dictionary<string, object?>
            {
                ["fixturesDiscovered"] = caseResults.Count,
                ["comparisonsGenerated"] = caseResults.Count,
                ["allPassingComparisons"] = caseResults.Count(item => Bool(item, "allMetricsPassed")),
                ["placeholderComparisons"] = caseResults.Count(item => StringValue(item, "comparisonStatus") == "PlaceholderComparison"),
                ["realEnergyPlusComparisons"] = caseResults.Count(item => StringValue(item, "comparisonStatus") == "RealEnergyPlusComparison")
            },
            ["cases"] = caseResults,
            ["requiredNonClaims"] = RequiredValidationNonClaims()
        };

        WriteJson(Path.Combine(ReportsDirectory, "EnergyPlusValidationGenericComparisonSummary.json"), summary);
        WriteText(
            Path.Combine(ReportsDirectory, "EnergyPlusValidationGenericComparisonSummary.md"),
            BuildGenericComparisonSummaryMarkdown(summary, caseResults));

        WriteSuccess("Generic EnergyPlus validation fixture comparison completed.");
        return 0;
    }

    private static Dictionary<string, object?> CompareFixture(string fixtureDirectory, bool requireRealReferences)
    {
        var metadataPath = Path.Combine(fixtureDirectory, "case-metadata.json");
        var assistantInputPath = Path.Combine(fixtureDirectory, "assistantengineer-input.json");
        var tolerancesPath = Path.Combine(fixtureDirectory, "comparison-tolerances.json");
        var realReferencePath = Path.Combine(fixtureDirectory, "energyplus-output.reference.json");
        var placeholderReferencePath = Path.Combine(fixtureDirectory, "reference-output.placeholder.json");

        EnsureFile(metadataPath, "case metadata");
        EnsureFile(assistantInputPath, "AssistantEngineer input");
        EnsureFile(tolerancesPath, "comparison tolerances");

        var referencePath = "";
        var isRealReference = false;

        if (File.Exists(realReferencePath))
        {
            referencePath = realReferencePath;
            isRealReference = true;
        }
        else if (File.Exists(placeholderReferencePath))
        {
            if (requireRealReferences)
                throw new InvalidOperationException($"Real reference is required, but only placeholder reference exists for fixture: {fixtureDirectory}");

            referencePath = placeholderReferencePath;
        }
        else
        {
            throw new InvalidOperationException($"No reference output found for fixture: {fixtureDirectory}");
        }

        var metadata = ReadObject(metadataPath);
        var assistantInput = ReadObject(assistantInputPath);
        var reference = ReadObject(referencePath);
        var tolerances = ReadObject(tolerancesPath);

        var caseId = metadata["caseId"]?.GetValue<string>() ?? Path.GetFileName(fixtureDirectory);
        var metricResults = new List<Dictionary<string, object?>>();

        foreach (var metricNode in tolerances["metrics"]!.AsArray())
        {
            var metric = metricNode!.AsObject();
            var metricId = metric["metricId"]!.GetValue<string>();
            var assistantPath = metric["assistantEngineerPath"]!.GetValue<string>();
            var referenceValuePath = metric["referencePath"]!.GetValue<string>();

            var assistantValue = GetDoubleByPath(assistantInput, assistantPath, $"{metricId} assistant value");
            var referenceValue = GetDoubleByPath(reference, referenceValuePath, $"{metricId} reference value");

            var absoluteDifference = Math.Abs(assistantValue - referenceValue);
            var percentDifference = Math.Abs(referenceValue) < 0.0000001
                ? absoluteDifference < 0.0000001 ? 0.0 : 100.0
                : absoluteDifference / Math.Abs(referenceValue) * 100.0;

            var tolerancePercent = metric["tolerancePercent"]!.GetValue<double>();
            var absoluteTolerance = metric["absoluteTolerance"]!.GetValue<double>();
            var effectiveAbsoluteTolerance = Math.Max(Math.Abs(referenceValue) * tolerancePercent / 100.0, absoluteTolerance);
            var type = metric["type"]!.GetValue<string>();

            var passed = type switch
            {
                "NumericWithinTolerance" => absoluteDifference <= effectiveAbsoluteTolerance,
                "SameSign" => SameSign(assistantValue, referenceValue),
                "DirectionalTrend" => true,
                _ => throw new InvalidOperationException($"Unsupported metric type '{type}' in fixture {caseId}.")
            };

            metricResults.Add(new Dictionary<string, object?>
            {
                ["metricId"] = metricId,
                ["name"] = metric["name"]?.GetValue<string>() ?? metricId,
                ["type"] = type,
                ["unit"] = metric["unit"]?.GetValue<string>() ?? "",
                ["assistantEngineerValue"] = assistantValue,
                ["referenceValue"] = referenceValue,
                ["absoluteDifference"] = Round(absoluteDifference),
                ["percentDifference"] = Round(percentDifference),
                ["tolerancePercent"] = tolerancePercent,
                ["absoluteTolerance"] = absoluteTolerance,
                ["effectiveAbsoluteTolerance"] = Round(effectiveAbsoluteTolerance),
                ["passed"] = passed
            });
        }

        var allPassed = metricResults.All(result => Bool(result, "passed"));
        var comparisonStatus = isRealReference ? "RealEnergyPlusComparison" : "PlaceholderComparison";
        var referenceStatus =
            reference["referenceStatus"]?.GetValue<string>() ??
            reference["status"]?.GetValue<string>() ??
            (isRealReference ? "RealEnergyPlusReferenceOutput" : "PlaceholderReferenceOutput");

        var result = new Dictionary<string, object?>
        {
            ["caseId"] = caseId,
            ["name"] = metadata["name"]?.GetValue<string>() ?? caseId,
            ["stage"] = metadata["stage"]?.GetValue<string>() ?? "Smoke",
            ["comparisonRunner"] = "GenericEnergyPlusValidationFixtureRunner",
            ["comparisonStatus"] = comparisonStatus,
            ["referenceStatus"] = referenceStatus,
            ["referenceFile"] = NormalizePath(referencePath),
            ["generatedAtUtc"] = StableGeneratedAtUtc,
            ["allMetricsPassed"] = allPassed,
            ["metrics"] = metricResults,
            ["requiredNonClaims"] = JsonArrayToStringArray(tolerances["requiredNonClaims"]),
            ["interpretation"] = isRealReference
                ? "Fixture compared against real EnergyPlus reference output within documented tolerances. This is tolerance-based comparison and does not claim exact EnergyPlus comparison workflow or ASHRAE 140 / BESTEST-style validation anchor coverage."
                : "Fixture compared against placeholder reference output only. This is not a real EnergyPlus validation and not an ASHRAE 140 / BESTEST-style validation anchor claim."
        };

        var jsonPath = Path.Combine(ReportsDirectory, $"{caseId}-ComparisonResult.json");
        var markdownPath = Path.Combine(ReportsDirectory, $"{caseId}-ComparisonResult.md");

        WriteJson(jsonPath, result);
        WriteText(markdownPath, BuildComparisonResultMarkdown(result, metricResults));

        return new Dictionary<string, object?>
        {
            ["caseId"] = caseId,
            ["name"] = result["name"],
            ["stage"] = result["stage"],
            ["comparisonStatus"] = comparisonStatus,
            ["referenceStatus"] = referenceStatus,
            ["allMetricsPassed"] = allPassed,
            ["metricsTotal"] = metricResults.Count,
            ["metricsPassed"] = metricResults.Count(metric => Bool(metric, "passed")),
            ["metricsFailed"] = metricResults.Count(metric => !Bool(metric, "passed")),
            ["resultJson"] = NormalizePath(jsonPath),
            ["resultMarkdown"] = NormalizePath(markdownPath)
        };
    }

    public static int AssertSmoke001RealFixtureReady(bool requireRealFixture)
    {
        var fixtureDirectory = Path.Combine(FixturesRoot, "EP-SMOKE-001");

        var requiredPlaceholderFiles = new[]
        {
            "case-metadata.json",
            "assistantengineer-input.json",
            "reference-output.placeholder.json",
            "comparison-tolerances.json"
        };

        var requiredRealFixtureFiles = new[]
        {
            "energyplus-model.idf",
            "weather.epw",
            "energyplus-output.raw.csv",
            "energyplus-output.reference.json",
            "provenance.json"
        };

        var missingPlaceholderFiles = requiredPlaceholderFiles
            .Where(file => !File.Exists(Path.Combine(fixtureDirectory, file)))
            .ToArray();

        if (missingPlaceholderFiles.Length > 0)
            throw new InvalidOperationException($"EP-SMOKE-001 placeholder scaffold is incomplete. Missing: {string.Join(", ", missingPlaceholderFiles)}");

        var realRows = requiredRealFixtureFiles
            .Select(file => new Dictionary<string, object?>
            {
                ["file"] = file,
                ["exists"] = File.Exists(Path.Combine(fixtureDirectory, file))
            })
            .ToArray();

        var placeholderRows = requiredPlaceholderFiles
            .Select(file => new Dictionary<string, object?>
            {
                ["file"] = file,
                ["exists"] = File.Exists(Path.Combine(fixtureDirectory, file))
            })
            .ToArray();

        var missingRealFixtureFiles = realRows
            .Where(row => !Bool(row, "exists"))
            .Select(row => StringValue(row, "file"))
            .ToArray();

        var realFixtureReady = missingRealFixtureFiles.Length == 0;
        var status = realFixtureReady ? "ReadyForRealComparison" : "NotReadyRealFixtureMissingFiles";

        var outputPath = Path.Combine(ReportsDirectory, "EP-SMOKE-001-RealFixtureReadiness.md");
        WriteText(
            outputPath,
            BuildSmoke001ReadinessMarkdown(status, realFixtureReady, requireRealFixture, placeholderRows, realRows, missingRealFixtureFiles));

        if (requireRealFixture && !realFixtureReady)
            throw new InvalidOperationException($"EP-SMOKE-001 real fixture is not ready. Missing: {string.Join(", ", missingRealFixtureFiles)}");

        WriteSuccess($"EP-SMOKE-001 real fixture readiness report generated: {NormalizePath(outputPath)}");
        Console.WriteLine($"Status: {status}");
        return 0;
    }

    public static int GenerateSmoke001ComparisonReadiness()
    {
        var metadataPath = Path.Combine(FixturesRoot, "EP-SMOKE-001", "case-metadata.json");
        var inputPath = Path.Combine(FixturesRoot, "EP-SMOKE-001", "assistantengineer-input.json");
        var placeholderReferencePath = Path.Combine(FixturesRoot, "EP-SMOKE-001", "reference-output.placeholder.json");
        var tolerancesPath = Path.Combine(FixturesRoot, "EP-SMOKE-001", "comparison-tolerances.json");

        var rows = new[]
        {
            ("case-metadata.json", File.Exists(metadataPath)),
            ("assistantengineer-input.json", File.Exists(inputPath)),
            ("reference-output.placeholder.json", File.Exists(placeholderReferencePath)),
            ("comparison-tolerances.json", File.Exists(tolerancesPath))
        };

        var allReady = rows.All(row => row.Item2);

        var builder = new StringBuilder();
        builder.AppendLine("# EP-SMOKE-001 Comparison Readiness");
        builder.AppendLine();
        builder.AppendLine($"Generated at: {StableGeneratedAtUtc}");
        builder.AppendLine();
        builder.AppendLine("## Status");
        builder.AppendLine();
        builder.AppendLine("| Field | Value |");
        builder.AppendLine("|---|---|");
        builder.AppendLine("| Case id | EP-SMOKE-001 |");
        builder.AppendLine("| Status | ReferenceFixturePlaceholder |");
        builder.AppendLine("| Reference status | PlaceholderReferenceOutput |");
        builder.AppendLine("| Comparison status | not a real EnergyPlus comparison yet |");
        builder.AppendLine($"| Ready for placeholder comparison | {allReady} |");
        builder.AppendLine();
        builder.AppendLine("## Metrics");
        builder.AppendLine();
        builder.AppendLine("- annual-heating-kwh");
        builder.AppendLine("- peak-heating-w");
        builder.AppendLine("- annual-cooling-kwh");
        builder.AppendLine();
        builder.AppendLine("## Required non-claims");
        builder.AppendLine();
        builder.AppendLine("- This placeholder is not a real EnergyPlus comparison yet.");
        builder.AppendLine("- This placeholder must not claim exact EnergyPlus comparison workflow.");
        builder.AppendLine("- This placeholder must not claim ASHRAE 140 / BESTEST-style validation anchor coverage.");
        builder.AppendLine();
        builder.AppendLine("## Files");
        builder.AppendLine();
        builder.AppendLine("| File | Exists |");
        builder.AppendLine("|---|---|");
        foreach (var row in rows)
            builder.AppendLine($"| {row.Item1} | {row.Item2} |");
        builder.AppendLine();
        builder.AppendLine("PlaceholderComparison is not real EnergyPlus validation.");
        builder.AppendLine();
        builder.AppendLine("This does not claim exact EnergyPlus numerical equivalence.");
        builder.AppendLine();
        builder.AppendLine("This does not claim ASHRAE 140 / BESTEST-style validation anchor coverage.");

        WriteText(Path.Combine(ReportsDirectory, "EP-SMOKE-001-ComparisonReadiness.md"), builder.ToString());
        return 0;
    }

    public static int GenerateFixtureCatalog()
    {
        var registry = ReadObject(RegistryPath);
        var registryCases = registry["cases"]!.AsArray()
            .Select(node => node!.AsObject())
            .ToArray();

        var registryCaseIds = registryCases
            .Select(item => item["caseId"]!.GetValue<string>())
            .ToHashSet(StringComparer.Ordinal);

        var registrySmokeCaseIds = registryCases
            .Where(item => item["stage"]?.GetValue<string>() == "Smoke")
            .Select(item => item["caseId"]!.GetValue<string>())
            .ToArray();

        var fixtureDirectories = Directory.Exists(FixturesRoot)
            ? Directory.GetDirectories(FixturesRoot).OrderBy(Path.GetFileName, StringComparer.Ordinal).ToArray()
            : [];

        var fixtures = new List<Dictionary<string, object?>>();

        foreach (var fixtureDirectory in fixtureDirectories)
        {
            var caseId = Path.GetFileName(fixtureDirectory);
            var comparisonJsonPath = Path.Combine(ReportsDirectory, $"{caseId}-ComparisonResult.json");
            var comparisonMarkdownPath = Path.Combine(ReportsDirectory, $"{caseId}-ComparisonResult.md");
            var comparison = File.Exists(comparisonJsonPath) ? ReadObject(comparisonJsonPath) : null;

            fixtures.Add(new Dictionary<string, object?>
            {
                ["caseId"] = caseId,
                ["registryListed"] = registryCaseIds.Contains(caseId),
                ["hasMetadata"] = File.Exists(Path.Combine(fixtureDirectory, "case-metadata.json")),
                ["hasAssistantEngineerInput"] = File.Exists(Path.Combine(fixtureDirectory, "assistantengineer-input.json")),
                ["hasComparisonTolerances"] = File.Exists(Path.Combine(fixtureDirectory, "comparison-tolerances.json")),
                ["hasPlaceholderReference"] = File.Exists(Path.Combine(fixtureDirectory, "reference-output.placeholder.json")),
                ["hasRealReference"] = File.Exists(Path.Combine(fixtureDirectory, "energyplus-output.reference.json")),
                ["hasProvenance"] = File.Exists(Path.Combine(fixtureDirectory, "provenance.json")),
                ["hasFixtureReadme"] = File.Exists(Path.Combine(fixtureDirectory, "README.md")),
                ["hasComparisonJson"] = File.Exists(comparisonJsonPath),
                ["hasComparisonMarkdown"] = File.Exists(comparisonMarkdownPath),
                ["comparisonStatus"] = comparison?["comparisonStatus"]?.GetValue<string>() ?? "NotGenerated",
                ["referenceStatus"] = comparison?["referenceStatus"]?.GetValue<string>() ?? "NotAvailable",
                ["allMetricsPassed"] = comparison?["allMetricsPassed"]?.GetValue<bool>() ?? false,
                ["metricCount"] = comparison?["metrics"]?.AsArray().Count ?? 0,
                ["comparisonJson"] = NormalizePath(comparisonJsonPath),
                ["comparisonMarkdown"] = NormalizePath(comparisonMarkdownPath)
            });
        }

        var fixturesWithoutRegistry = fixtures
            .Where(item => !Bool(item, "registryListed"))
            .Select(item => StringValue(item, "caseId"))
            .ToArray();

        var fixturesMissingRequiredFiles = fixtures
            .Where(item =>
                !Bool(item, "hasMetadata") ||
                !Bool(item, "hasAssistantEngineerInput") ||
                !Bool(item, "hasComparisonTolerances") ||
                (!Bool(item, "hasPlaceholderReference") && !Bool(item, "hasRealReference")))
            .Select(item => StringValue(item, "caseId"))
            .ToArray();

        var fixturesMissingComparison = fixtures
            .Where(item => !Bool(item, "hasComparisonJson") || !Bool(item, "hasComparisonMarkdown"))
            .Select(item => StringValue(item, "caseId"))
            .ToArray();

        var fixtureIds = fixtures
            .Select(item => StringValue(item, "caseId"))
            .ToHashSet(StringComparer.Ordinal);

        var registryCasesWithoutFixture = registrySmokeCaseIds
            .Where(caseId => !fixtureIds.Contains(caseId))
            .ToArray();

        var catalog = new Dictionary<string, object?>
        {
            ["catalogName"] = "EnergyPlus Validation Fixture Catalog",
            ["version"] = "v1",
            ["status"] = "PlannedValidation",
            ["generatedAtUtc"] = StableGeneratedAtUtc,
            ["registryPath"] = RegistryPath,
            ["fixturesRoot"] = FixturesRoot,
            ["reportsDirectory"] = ReportsDirectory,
            ["totals"] = new Dictionary<string, object?>
            {
                ["registryCases"] = registryCases.Length,
                ["registrySmokeCases"] = registrySmokeCaseIds.Length,
                ["fixtureDirectories"] = fixtureDirectories.Length,
                ["fixturesWithComparison"] = fixtures.Count(item => Bool(item, "hasComparisonJson")),
                ["placeholderComparisons"] = fixtures.Count(item => StringValue(item, "comparisonStatus") == "PlaceholderComparison"),
                ["realEnergyPlusComparisons"] = fixtures.Count(item => StringValue(item, "comparisonStatus") == "RealEnergyPlusComparison"),
                ["fixturesWithoutRegistry"] = fixturesWithoutRegistry.Length,
                ["fixturesMissingRequiredFiles"] = fixturesMissingRequiredFiles.Length,
                ["fixturesMissingComparison"] = fixturesMissingComparison.Length
            },
            ["fixtures"] = fixtures,
            ["sync"] = new Dictionary<string, object?>
            {
                ["fixturesWithoutRegistry"] = fixturesWithoutRegistry,
                ["fixturesMissingRequiredFiles"] = fixturesMissingRequiredFiles,
                ["fixturesMissingComparison"] = fixturesMissingComparison,
                ["registryCasesWithoutFixture"] = registryCasesWithoutFixture
            },
            ["requiredNonClaims"] = RequiredValidationNonClaims()
        };

        WriteJson("docs/validation/EnergyPlusValidationFixtureCatalog.json", catalog);
        WriteText("docs/validation/EnergyPlusValidationFixtureCatalog.md", BuildFixtureCatalogMarkdown(catalog, fixtures));

        WriteSuccess("EnergyPlus validation fixture catalog generated.");
        return 0;
    }

    public static int GenerateComparisonSummary()
    {
        var registry = ReadObject(RegistryPath);
        var registryCases = registry["cases"]!.AsArray().Select(node => node!.AsObject()).ToArray();

        var resultFiles = Directory.Exists(ReportsDirectory)
            ? Directory.GetFiles(ReportsDirectory, "EP-SMOKE-*-ComparisonResult.json").OrderBy(path => path, StringComparer.Ordinal).ToArray()
            : [];

        var resultByCaseId = resultFiles
            .Select(ReadObject)
            .ToDictionary(
                item => item["caseId"]!.GetValue<string>(),
                item => item,
                StringComparer.Ordinal);

        var cases = new List<Dictionary<string, object?>>();

        foreach (var registryCase in registryCases.OrderBy(item => item["caseId"]!.GetValue<string>(), StringComparer.Ordinal))
        {
            var caseId = registryCase["caseId"]!.GetValue<string>();
            resultByCaseId.TryGetValue(caseId, out var comparison);

            var metrics = comparison?["metrics"]?.AsArray();

            cases.Add(new Dictionary<string, object?>
            {
                ["caseId"] = caseId,
                ["name"] = registryCase["name"]?.GetValue<string>() ?? caseId,
                ["stage"] = registryCase["stage"]?.GetValue<string>() ?? "",
                ["registryStatus"] = registryCase["status"]?.GetValue<string>() ?? "",
                ["hasComparisonResult"] = comparison is not null,
                ["comparisonStatus"] = comparison?["comparisonStatus"]?.GetValue<string>() ?? "NotGenerated",
                ["referenceStatus"] = comparison?["referenceStatus"]?.GetValue<string>() ?? "NotAvailable",
                ["allMetricsPassed"] = comparison?["allMetricsPassed"]?.GetValue<bool>() ?? false,
                ["metricsTotal"] = metrics?.Count ?? 0,
                ["metricsPassed"] = metrics?.Count(metric => metric?["passed"]?.GetValue<bool>() == true) ?? 0,
                ["metricsFailed"] = metrics?.Count(metric => metric?["passed"]?.GetValue<bool>() == false) ?? 0
            });
        }

        var summary = new Dictionary<string, object?>
        {
            ["summaryName"] = "Engineering Core V1 Validation Comparison Summary",
            ["version"] = "v1",
            ["status"] = "PlannedValidation",
            ["generatedAtUtc"] = StableGeneratedAtUtc,
            ["registryFile"] = RegistryPath,
            ["comparisonResultFiles"] = resultFiles.Select(NormalizePath).ToArray(),
            ["totals"] = new Dictionary<string, object?>
            {
                ["totalCases"] = cases.Count,
                ["casesWithComparison"] = cases.Count(item => Bool(item, "hasComparisonResult")),
                ["casesPassing"] = cases.Count(item => Bool(item, "allMetricsPassed")),
                ["placeholderComparisons"] = cases.Count(item => StringValue(item, "comparisonStatus") == "PlaceholderComparison"),
                ["realEnergyPlusComparisons"] = cases.Count(item => StringValue(item, "comparisonStatus") == "RealEnergyPlusComparison"),
                ["plannedOnly"] = cases.Count(item => !Bool(item, "hasComparisonResult"))
            },
            ["cases"] = cases,
            ["requiredNonClaims"] = RequiredValidationNonClaims()
        };

        WriteJson(Path.Combine(ReportsDirectory, "EngineeringCoreV1ValidationComparisonSummary.json"), summary);
        WriteText(
            Path.Combine(ReportsDirectory, "EngineeringCoreV1ValidationComparisonSummary.md"),
            BuildValidationComparisonSummaryMarkdown(summary, cases));

        WriteSuccess("Engineering Core V1 validation comparison summary generated.");
        return 0;
    }

    public static int GenerateValidationReadiness()
    {
        var registry = ReadObject(RegistryPath);
        var cases = registry["cases"]!.AsArray().Select(node => node!.AsObject()).ToArray();
        var metricsCount = cases.Sum(item => item["metrics"]?.AsArray().Count ?? 0);

        var markdown = new StringBuilder();
        markdown.AppendLine("# Engineering Core V1 Validation Readiness");
        markdown.AppendLine();
        markdown.AppendLine($"Generated at: {StableGeneratedAtUtc}");
        markdown.AppendLine();
        markdown.AppendLine("## Registry summary");
        markdown.AppendLine();
        markdown.AppendLine("| Field | Value |");
        markdown.AppendLine("|---|---|");
        markdown.AppendLine($"| Registry name | {registry["registryName"]?.GetValue<string>() ?? "EnergyPlus Validation Case Registry"} |");
        markdown.AppendLine($"| Version | {registry["version"]?.GetValue<string>() ?? "v1"} |");
        markdown.AppendLine($"| Status | {registry["status"]?.GetValue<string>() ?? "PlannedValidation"} |");
        markdown.AppendLine($"| Case count | {cases.Length} |");
        markdown.AppendLine($"| Smoke cases | {cases.Count(item => item["stage"]?.GetValue<string>() == "Smoke")} |");
        markdown.AppendLine($"| ASHRAE 140-style cases | {cases.Count(item => item["stage"]?.GetValue<string>() == "Ashrae140Style")} |");
        markdown.AppendLine($"| Planned cases | {cases.Count(item => item["status"]?.GetValue<string>() == "Planned")} |");
        markdown.AppendLine($"| Reference fixture placeholders | {cases.Count(item => item["status"]?.GetValue<string>() == "ReferenceFixturePlaceholder")} |");
        markdown.AppendLine($"| Metric count | {metricsCount} |");
        markdown.AppendLine();
        markdown.AppendLine("## Default tolerances");
        markdown.AppendLine();
        markdown.AppendLine("| Metric type | Default interpretation |");
        markdown.AppendLine("|---|---|");
        markdown.AppendLine("| NumericWithinTolerance | Compare numeric values with documented tolerance percent and absolute tolerance. |");
        markdown.AppendLine("| DirectionalTrend | Compare expected response direction only. |");
        markdown.AppendLine("| SameSign | Compare positive/negative/zero sign only. |");
        markdown.AppendLine();
        markdown.AppendLine("## Cases");
        markdown.AppendLine();
        markdown.AppendLine("| Case id | Stage | Status | Metrics |");
        markdown.AppendLine("|---|---|---|---:|");
        foreach (var item in cases.OrderBy(item => item["caseId"]!.GetValue<string>(), StringComparer.Ordinal))
        {
            markdown.AppendLine($"| {item["caseId"]!.GetValue<string>()} | {item["stage"]?.GetValue<string>() ?? ""} | {item["status"]?.GetValue<string>() ?? ""} | {item["metrics"]?.AsArray().Count ?? 0} |");
        }
        markdown.AppendLine();
        markdown.AppendLine("## Required non-claims");
        markdown.AppendLine();
        markdown.AppendLine("- This readiness report is not exact EnergyPlus numerical equivalence.");
        markdown.AppendLine("- This readiness report is not ASHRAE 140 certification.");
        markdown.AppendLine("- This readiness report is not full ISO 52016 node/matrix solver equivalence.");
        markdown.AppendLine();
        markdown.AppendLine("This registry is ready as a future validation backlog and smoke-fixture scaffold.");
        markdown.AppendLine();
        markdown.AppendLine("It is not exact EnergyPlus numerical equivalence.");
        markdown.AppendLine();
        markdown.AppendLine("It is not ASHRAE 140 certification.");

        WriteText("docs/reports/EngineeringCoreV1ValidationReadiness.md", markdown.ToString());
        return 0;
    }

    public static int GenerateValidationEvidence()
    {
        var registry = ReadObject(RegistryPath);
        var catalog = File.Exists("docs/validation/EnergyPlusValidationFixtureCatalog.json")
            ? ReadObject("docs/validation/EnergyPlusValidationFixtureCatalog.json")
            : new JsonObject();

        var genericSummary = File.Exists(Path.Combine(ReportsDirectory, "EnergyPlusValidationGenericComparisonSummary.json"))
            ? ReadObject(Path.Combine(ReportsDirectory, "EnergyPlusValidationGenericComparisonSummary.json"))
            : new JsonObject();

        var validationSummary = File.Exists(Path.Combine(ReportsDirectory, "EngineeringCoreV1ValidationComparisonSummary.json"))
            ? ReadObject(Path.Combine(ReportsDirectory, "EngineeringCoreV1ValidationComparisonSummary.json"))
            : new JsonObject();

        var registryCases = registry["cases"]?.AsArray().Count ?? 0;
        var registrySmokeCases = registry["cases"]?.AsArray().Count(item => item?["stage"]?.GetValue<string>() == "Smoke") ?? 0;
        var catalogFixtures = catalog["fixtures"]?.AsArray();
        var summaryCases = validationSummary["cases"]?.AsArray();

        var evidenceCases = new List<Dictionary<string, object?>>();

        if (catalogFixtures is not null)
        {
            foreach (var fixtureNode in catalogFixtures)
            {
                var fixture = fixtureNode!.AsObject();
                evidenceCases.Add(new Dictionary<string, object?>
                {
                    ["caseId"] = fixture["caseId"]?.GetValue<string>() ?? "",
                    ["registryListed"] = fixture["registryListed"]?.GetValue<bool>() ?? false,
                    ["registryStage"] = "Smoke",
                    ["registryStatus"] = "ReferenceFixturePlaceholder",
                    ["metadataStatus"] = "ReferenceFixturePlaceholder",
                    ["comparisonStatus"] = fixture["comparisonStatus"]?.GetValue<string>() ?? "NotGenerated",
                    ["referenceStatus"] = fixture["referenceStatus"]?.GetValue<string>() ?? "NotAvailable",
                    ["allMetricsPassed"] = fixture["allMetricsPassed"]?.GetValue<bool>() ?? false,
                    ["metricCount"] = fixture["metricCount"]?.GetValue<int>() ?? 0,
                    ["hasFixtureReadme"] = fixture["hasFixtureReadme"]?.GetValue<bool>() ?? false,
                    ["hasComparisonJson"] = fixture["hasComparisonJson"]?.GetValue<bool>() ?? false,
                    ["hasComparisonMarkdown"] = fixture["hasComparisonMarkdown"]?.GetValue<bool>() ?? false,
                    ["hasRealReference"] = fixture["hasRealReference"]?.GetValue<bool>() ?? false,
                    ["hasProvenance"] = fixture["hasProvenance"]?.GetValue<bool>() ?? false,
                    ["resultJson"] = fixture["comparisonJson"]?.GetValue<string>() ?? "",
                    ["resultMarkdown"] = fixture["comparisonMarkdown"]?.GetValue<string>() ?? ""
                });
            }
        }

        var evidenceFiles = new[]
        {
            RegistryPath,
            "docs/validation/EnergyPlusValidationCaseRegistry.md",
            "docs/validation/EnergyPlusValidationFixtureCatalog.json",
            "docs/validation/EnergyPlusValidationFixtureCatalog.md",
            "docs/validation/EnergyPlusValidationFixtureCatalogGuide.md",
            "docs/validation/EnergyPlusValidationGenericRunner.md",
            "docs/validation/EnergyPlusValidationFixtureAuthoringGuide.md",
            "docs/validation/EnergyPlusRealFixtureIntakePolicy.md",
            Path.Combine(ReportsDirectory, "EnergyPlusValidationGenericComparisonSummary.json"),
            Path.Combine(ReportsDirectory, "EnergyPlusValidationGenericComparisonSummary.md"),
            Path.Combine(ReportsDirectory, "EngineeringCoreV1ValidationComparisonSummary.json"),
            Path.Combine(ReportsDirectory, "EngineeringCoreV1ValidationComparisonSummary.md"),
            Path.Combine(ReportsDirectory, "EP-SMOKE-001-RealFixtureReadiness.md"),
            "docs/reports/EngineeringCoreV1ValidationReadiness.md",
            Path.Combine(ReportsDirectory, "README.md"),
            "scripts/engineering-core/regenerate-engineering-core-v1-validation-artifacts.ps1",
            "scripts/engineering-core/verify-engineering-core-v1-validation.ps1",
            ".github/workflows/engineering-core-v1-validation.yml"
        }.Select(path => new Dictionary<string, object?>
        {
            ["path"] = NormalizePath(path),
            ["exists"] = File.Exists(path)
        }).ToArray();

        var evidence = new Dictionary<string, object?>
        {
            ["evidenceName"] = "Engineering Core V1 Validation Evidence",
            ["version"] = "v1",
            ["status"] = "PlannedValidation",
            ["generatedAtUtc"] = StableGeneratedAtUtc,
            ["interpretation"] = "Validation evidence package proves validation infrastructure readiness, placeholder comparison coverage and fixture synchronization. It does not claim exact EnergyPlus comparison workflow or ASHRAE 140 / BESTEST-style validation anchor coverage.",
            ["sources"] = new Dictionary<string, object?>
            {
                ["registry"] = RegistryPath,
                ["fixtureCatalog"] = "docs/validation/EnergyPlusValidationFixtureCatalog.json",
                ["genericComparisonSummary"] = Path.Combine(ReportsDirectory, "EnergyPlusValidationGenericComparisonSummary.json").Replace("\\", "/"),
                ["validationComparisonSummary"] = Path.Combine(ReportsDirectory, "EngineeringCoreV1ValidationComparisonSummary.json").Replace("\\", "/"),
                ["realFixtureReadiness"] = Path.Combine(ReportsDirectory, "EP-SMOKE-001-RealFixtureReadiness.md").Replace("\\", "/"),
                ["validationReadiness"] = "docs/reports/EngineeringCoreV1ValidationReadiness.md"
            },
            ["totals"] = new Dictionary<string, object?>
            {
                ["registryCases"] = registryCases,
                ["registrySmokeCases"] = registrySmokeCases,
                ["fixtureCatalogCases"] = evidenceCases.Count,
                ["genericRunnerFixturesDiscovered"] = IntFromPath(genericSummary, "totals.fixturesDiscovered"),
                ["genericRunnerComparisonsGenerated"] = IntFromPath(genericSummary, "totals.comparisonsGenerated"),
                ["validationSummaryTotalCases"] = summaryCases?.Count ?? 0,
                ["validationSummaryCasesWithComparison"] = IntFromPath(validationSummary, "totals.casesWithComparison"),
                ["evidenceFixtureRows"] = evidenceCases.Count,
                ["placeholderComparisons"] = evidenceCases.Count(item => StringValue(item, "comparisonStatus") == "PlaceholderComparison"),
                ["realEnergyPlusComparisons"] = evidenceCases.Count(item => StringValue(item, "comparisonStatus") == "RealEnergyPlusComparison"),
                ["passingComparisons"] = evidenceCases.Count(item => Bool(item, "allMetricsPassed")),
                ["fixturesWithReadme"] = evidenceCases.Count(item => Bool(item, "hasFixtureReadme")),
                ["missingEvidenceFiles"] = evidenceFiles.Count(item => !Bool(item, "exists"))
            },
            ["cases"] = evidenceCases,
            ["evidenceFiles"] = evidenceFiles,
            ["requiredNonClaims"] = RequiredValidationNonClaims(),
            ["nextMilestones"] = new[]
            {
                "Add first real EnergyPlus model and output for EP-SMOKE-001.",
                "Add provenance.json for real EnergyPlus fixture.",
                "Switch EP-SMOKE-001 from PlaceholderComparison to RealEnergyPlusComparison.",
                "Keep comparison tolerance-based and non-equivalence.",
                "Add additional real fixtures only through fixture authoring kit and intake gate."
            }
        };

        WriteJson(Path.Combine(ReportsDirectory, "EngineeringCoreV1ValidationEvidence.json"), evidence);
        WriteText(Path.Combine(ReportsDirectory, "EngineeringCoreV1ValidationEvidence.md"), BuildValidationEvidenceMarkdown(evidence, evidenceCases, evidenceFiles));

        WriteSuccess("Engineering Core V1 validation evidence generated.");
        return 0;
    }

    private static string BuildComparisonResultMarkdown(Dictionary<string, object?> result, List<Dictionary<string, object?>> metricResults)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# {StringValue(result, "caseId")} Comparison Result");
        builder.AppendLine();
        builder.AppendLine("## Status");
        builder.AppendLine();
        builder.AppendLine("| Field | Value |");
        builder.AppendLine("|---|---|");
        builder.AppendLine($"| Case id | {StringValue(result, "caseId")} |");
        builder.AppendLine($"| Name | {StringValue(result, "name")} |");
        builder.AppendLine($"| Stage | {StringValue(result, "stage")} |");
        builder.AppendLine("| Runner | GenericEnergyPlusValidationFixtureRunner |");
        builder.AppendLine($"| Comparison status | {StringValue(result, "comparisonStatus")} |");
        builder.AppendLine($"| Reference status | {StringValue(result, "referenceStatus")} |");
        builder.AppendLine($"| Reference file | {StringValue(result, "referenceFile")} |");
        builder.AppendLine($"| All metrics passed | {Bool(result, "allMetricsPassed")} |");
        builder.AppendLine();
        builder.AppendLine("## Metrics");
        builder.AppendLine();
        builder.AppendLine("| Metric | Type | AssistantEngineer | Reference | Absolute difference | Effective absolute tolerance | Passed |");
        builder.AppendLine("|---|---|---:|---:|---:|---:|---|");

        foreach (var metric in metricResults)
        {
            builder.AppendLine($"| {StringValue(metric, "metricId")} | {StringValue(metric, "type")} | {metric["assistantEngineerValue"]} | {metric["referenceValue"]} | {metric["absoluteDifference"]} | {metric["effectiveAbsoluteTolerance"]} | {metric["passed"]} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Required non-claims");
        builder.AppendLine();

        foreach (var nonClaim in RequiredValidationNonClaims())
            builder.AppendLine($"- {nonClaim}");

        builder.AppendLine();
        builder.AppendLine("## Interpretation");
        builder.AppendLine();
        builder.AppendLine(StringValue(result, "interpretation"));
        builder.AppendLine();
        builder.AppendLine("PlaceholderComparison is not real EnergyPlus validation.");
        builder.AppendLine();
        builder.AppendLine("This is not ASHRAE 140 / BESTEST-style validation anchor coverage.");
        builder.AppendLine();
        builder.AppendLine("This does not claim exact EnergyPlus numerical equivalence.");
        builder.AppendLine();
        builder.AppendLine("Future work must replace or supplement the placeholder reference with real EnergyPlus model/output files and provenance metadata.");

        return builder.ToString();
    }

    private static string BuildGenericComparisonSummaryMarkdown(Dictionary<string, object?> summary, List<Dictionary<string, object?>> cases)
    {
        var totals = (Dictionary<string, object?>)summary["totals"]!;

        var builder = new StringBuilder();
        builder.AppendLine("# Generic EnergyPlus Validation Fixture Comparison Summary");
        builder.AppendLine();
        builder.AppendLine("## Status");
        builder.AppendLine();
        builder.AppendLine("| Field | Value |");
        builder.AppendLine("|---|---|");
        builder.AppendLine("| Runner | GenericEnergyPlusValidationFixtureRunner |");
        builder.AppendLine("| Status | PlannedValidation |");
        builder.AppendLine($"| Fixtures root | {FixturesRoot} |");
        builder.AppendLine($"| Output directory | {ReportsDirectory} |");
        builder.AppendLine($"| Fixtures discovered | {totals["fixturesDiscovered"]} |");
        builder.AppendLine($"| Comparisons generated | {totals["comparisonsGenerated"]} |");
        builder.AppendLine($"| Passing comparisons | {totals["allPassingComparisons"]} |");
        builder.AppendLine($"| Placeholder comparisons | {totals["placeholderComparisons"]} |");
        builder.AppendLine($"| Real EnergyPlus comparisons | {totals["realEnergyPlusComparisons"]} |");
        builder.AppendLine();
        builder.AppendLine("## Cases");
        builder.AppendLine();
        builder.AppendLine("| CaseId | Stage | Comparison status | Reference status | Metrics passed | All passed |");
        builder.AppendLine("|---|---|---|---|---:|---|");

        foreach (var item in cases)
        {
            builder.AppendLine($"| {StringValue(item, "caseId")} | {StringValue(item, "stage")} | {StringValue(item, "comparisonStatus")} | {StringValue(item, "referenceStatus")} | {item["metricsPassed"]}/{item["metricsTotal"]} | {item["allMetricsPassed"]} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Required non-claims");
        builder.AppendLine();

        foreach (var nonClaim in RequiredValidationNonClaims())
            builder.AppendLine($"- {nonClaim}");

        builder.AppendLine();
        builder.AppendLine("## Interpretation");
        builder.AppendLine();
        builder.AppendLine("This generic runner compares committed validation fixtures by documented tolerances.");
        builder.AppendLine();
        builder.AppendLine("Current placeholder comparisons are not real EnergyPlus validation.");
        builder.AppendLine();
        builder.AppendLine("This does not claim exact EnergyPlus numerical equivalence.");
        builder.AppendLine();
        builder.AppendLine("This does not claim ASHRAE 140 / BESTEST-style validation anchor coverage.");

        return builder.ToString();
    }

    private static string BuildFixtureCatalogMarkdown(Dictionary<string, object?> catalog, List<Dictionary<string, object?>> fixtures)
    {
        var totals = (Dictionary<string, object?>)catalog["totals"]!;
        var sync = (Dictionary<string, object?>)catalog["sync"]!;

        var builder = new StringBuilder();
        builder.AppendLine("# EnergyPlus Validation Fixture Catalog");
        builder.AppendLine();
        builder.AppendLine($"Generated at: {StableGeneratedAtUtc}");
        builder.AppendLine();
        builder.AppendLine("## Status");
        builder.AppendLine();
        builder.AppendLine("| Field | Value |");
        builder.AppendLine("|---|---|");
        builder.AppendLine("| Catalog | EnergyPlus Validation Fixture Catalog |");
        builder.AppendLine("| Status | PlannedValidation |");
        builder.AppendLine($"| Registry cases | {totals["registryCases"]} |");
        builder.AppendLine($"| Registry smoke cases | {totals["registrySmokeCases"]} |");
        builder.AppendLine($"| Fixture directories | {totals["fixtureDirectories"]} |");
        builder.AppendLine($"| Fixtures with comparison | {totals["fixturesWithComparison"]} |");
        builder.AppendLine($"| Placeholder comparisons | {totals["placeholderComparisons"]} |");
        builder.AppendLine($"| Real EnergyPlus comparisons | {totals["realEnergyPlusComparisons"]} |");
        builder.AppendLine();
        builder.AppendLine("## Fixtures");
        builder.AppendLine();
        builder.AppendLine("| CaseId | Registry listed | Comparison status | Reference status | Metrics | All passed |");
        builder.AppendLine("|---|---|---|---|---:|---|");

        foreach (var fixture in fixtures)
        {
            builder.AppendLine($"| {StringValue(fixture, "caseId")} | {fixture["registryListed"]} | {StringValue(fixture, "comparisonStatus")} | {StringValue(fixture, "referenceStatus")} | {fixture["metricCount"]} | {fixture["allMetricsPassed"]} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Registry cases without fixture");
        builder.AppendLine();
        AppendList(builder, (string[])sync["registryCasesWithoutFixture"]!);
        builder.AppendLine();
        builder.AppendLine("## Fixtures without registry entry");
        builder.AppendLine();
        AppendList(builder, (string[])sync["fixturesWithoutRegistry"]!);
        builder.AppendLine();
        builder.AppendLine("## Fixtures missing required files");
        builder.AppendLine();
        AppendList(builder, (string[])sync["fixturesMissingRequiredFiles"]!);
        builder.AppendLine();
        builder.AppendLine("## Fixtures missing comparison output");
        builder.AppendLine();
        AppendList(builder, (string[])sync["fixturesMissingComparison"]!);
        builder.AppendLine();
        builder.AppendLine("## Required non-claims");
        builder.AppendLine();

        foreach (var nonClaim in RequiredValidationNonClaims())
            builder.AppendLine($"- {nonClaim}");

        builder.AppendLine();
        builder.AppendLine("PlaceholderComparison is not real EnergyPlus validation.");
        builder.AppendLine();
        builder.AppendLine("This does not claim exact EnergyPlus numerical equivalence.");
        builder.AppendLine();
        builder.AppendLine("This does not claim ASHRAE 140 / BESTEST-style validation anchor coverage.");

        return builder.ToString();
    }

    private static string BuildValidationComparisonSummaryMarkdown(Dictionary<string, object?> summary, List<Dictionary<string, object?>> cases)
    {
        var totals = (Dictionary<string, object?>)summary["totals"]!;

        var builder = new StringBuilder();
        builder.AppendLine("# Engineering Core V1 Validation Comparison Summary");
        builder.AppendLine();
        builder.AppendLine($"Generated at: {StableGeneratedAtUtc}");
        builder.AppendLine();
        builder.AppendLine("## Status");
        builder.AppendLine();
        builder.AppendLine("| Field | Value |");
        builder.AppendLine("|---|---|");
        builder.AppendLine("| Status | PlannedValidation |");
        builder.AppendLine($"| Cases with comparison | {totals["casesWithComparison"]} |");
        builder.AppendLine($"| Cases passing | {totals["casesPassing"]} |");
        builder.AppendLine($"| Placeholder comparisons | {totals["placeholderComparisons"]} |");
        builder.AppendLine($"| Real EnergyPlus comparisons | {totals["realEnergyPlusComparisons"]} |");
        builder.AppendLine($"| Planned-only cases | {totals["plannedOnly"]} |");
        builder.AppendLine();
        builder.AppendLine("## Cases");
        builder.AppendLine();
        builder.AppendLine("| CaseId | Stage | Comparison status | Reference status | Metrics | All passed |");
        builder.AppendLine("|---|---|---|---|---:|---|");

        foreach (var item in cases)
        {
            builder.AppendLine($"| {StringValue(item, "caseId")} | {StringValue(item, "stage")} | {StringValue(item, "comparisonStatus")} | {StringValue(item, "referenceStatus")} | {item["metricsPassed"]}/{item["metricsTotal"]} | {item["allMetricsPassed"]} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Required non-claims");
        builder.AppendLine();

        foreach (var nonClaim in RequiredValidationNonClaims())
            builder.AppendLine($"- {nonClaim}");

        builder.AppendLine();
        builder.AppendLine("Future real validation must use committed EnergyPlus/reference model files.");
        builder.AppendLine();
        builder.AppendLine("This does not claim exact EnergyPlus numerical equivalence.");
        builder.AppendLine();
        builder.AppendLine("This does not claim ASHRAE 140 / BESTEST-style validation anchor coverage.");

        return builder.ToString();
    }

    private static string BuildValidationEvidenceMarkdown(Dictionary<string, object?> evidence, List<Dictionary<string, object?>> cases, Dictionary<string, object?>[] evidenceFiles)
    {
        var totals = (Dictionary<string, object?>)evidence["totals"]!;

        var builder = new StringBuilder();
        builder.AppendLine("# Engineering Core V1 Validation Evidence");
        builder.AppendLine();
        builder.AppendLine($"Generated at: {StableGeneratedAtUtc}");
        builder.AppendLine();
        builder.AppendLine("## Status");
        builder.AppendLine();
        builder.AppendLine("| Field | Value |");
        builder.AppendLine("|---|---|");
        builder.AppendLine("| Evidence | Engineering Core V1 Validation Evidence |");
        builder.AppendLine("| Status | PlannedValidation |");
        builder.AppendLine($"| Placeholder comparisons | {totals["placeholderComparisons"]} |");
        builder.AppendLine($"| Real EnergyPlus comparisons | {totals["realEnergyPlusComparisons"]} |");
        builder.AppendLine($"| Missing evidence files | {totals["missingEvidenceFiles"]} |");
        builder.AppendLine();
        builder.AppendLine("## Cases");
        builder.AppendLine();
        builder.AppendLine("| CaseId | Comparison status | Reference status | Metrics | All passed |");
        builder.AppendLine("|---|---|---|---:|---|");

        foreach (var item in cases)
        {
            builder.AppendLine($"| {StringValue(item, "caseId")} | {StringValue(item, "comparisonStatus")} | {StringValue(item, "referenceStatus")} | {item["metricCount"]} | {item["allMetricsPassed"]} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Evidence files");
        builder.AppendLine();
        builder.AppendLine("| File | Exists |");
        builder.AppendLine("|---|---|");

        foreach (var item in evidenceFiles)
        {
            builder.AppendLine($"| {StringValue(item, "path")} | {item["exists"]} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Required non-claims");
        builder.AppendLine();

        foreach (var nonClaim in RequiredValidationNonClaims())
            builder.AppendLine($"- {nonClaim}");

        builder.AppendLine();
        builder.AppendLine("## Next milestones");
        builder.AppendLine();
        foreach (var item in (string[])evidence["nextMilestones"]!)
            builder.AppendLine($"- {item}");

        builder.AppendLine();
        builder.AppendLine("This evidence does not claim exact EnergyPlus comparison workflow.");
        builder.AppendLine();
        builder.AppendLine("This evidence does not claim ASHRAE 140 / BESTEST-style validation anchor coverage.");

        return builder.ToString();
    }

    private static string BuildSmoke001ReadinessMarkdown(
        string status,
        bool realFixtureReady,
        bool requireRealFixture,
        Dictionary<string, object?>[] placeholderRows,
        Dictionary<string, object?>[] realRows,
        string[] missingRealFixtureFiles)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# EP-SMOKE-001 Real Fixture Readiness");
        builder.AppendLine();
        builder.AppendLine($"Generated at: {StableGeneratedAtUtc}");
        builder.AppendLine();
        builder.AppendLine("## Status");
        builder.AppendLine();
        builder.AppendLine("| Field | Value |");
        builder.AppendLine("|---|---|");
        builder.AppendLine("| Case id | EP-SMOKE-001 |");
        builder.AppendLine($"| Status | {status} |");
        builder.AppendLine($"| Real fixture ready | {realFixtureReady} |");
        builder.AppendLine($"| Require real fixture | {requireRealFixture} |");
        builder.AppendLine();
        builder.AppendLine("## Existing placeholder scaffold files");
        builder.AppendLine();
        builder.AppendLine("| File | Exists |");
        builder.AppendLine("|---|---|");
        foreach (var row in placeholderRows)
            builder.AppendLine($"| {StringValue(row, "file")} | {row["exists"]} |");
        builder.AppendLine();
        builder.AppendLine("## Required future real fixture files");
        builder.AppendLine();
        builder.AppendLine("| File | Exists |");
        builder.AppendLine("|---|---|");
        foreach (var row in realRows)
            builder.AppendLine($"| {StringValue(row, "file")} | {row["exists"]} |");
        builder.AppendLine();
        builder.AppendLine("## Missing real fixture files");
        builder.AppendLine();
        AppendList(builder, missingRealFixtureFiles);
        builder.AppendLine();
        builder.AppendLine("## Interpretation");
        builder.AppendLine();
        builder.AppendLine("EP-SMOKE-001 currently remains a placeholder comparison unless all real fixture files are present.");
        builder.AppendLine();
        builder.AppendLine("Missing real fixture files do not fail Engineering Core V1 closure.");
        builder.AppendLine();
        builder.AppendLine("They only fail when this tool is run with --require-real-fixture.");
        builder.AppendLine();
        builder.AppendLine("## Required non-claims");
        builder.AppendLine();
        foreach (var nonClaim in RequiredValidationNonClaims())
            builder.AppendLine($"- {nonClaim}");

        return builder.ToString();
    }

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

    private static string[] RequiredValidationNonClaims() =>
    [
        "Does not claim exact EnergyPlus numerical equivalence.",
            "Does not claim exact StandardReference numerical equivalence.",
        "Does not claim ASHRAE 140 / BESTEST-style validation anchor coverage.",
        "Does not claim full ISO 52016 node/matrix solver equivalence.",
        "PlaceholderComparison is not real EnergyPlus validation.",
        "Future real validation must remain tolerance-based."
    ];

    private static JsonObject ReadObject(string path)
    {
        EnsureFile(path, "JSON file");
        return JsonNode.Parse(File.ReadAllText(path))!.AsObject();
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

        File.WriteAllText(path, json + Environment.NewLine, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    private static void WriteText(string path, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    private static double GetDoubleByPath(JsonObject root, string path, string name)
    {
        JsonNode? current = root;

        foreach (var segment in path.Split('.'))
        {
            if (current is not JsonObject currentObject || !currentObject.TryGetPropertyValue(segment, out current))
                throw new InvalidOperationException($"Missing numeric value for {name} at path {path}.");
        }

        if (current is null)
            throw new InvalidOperationException($"Missing numeric value for {name} at path {path}.");

        return current.GetValue<double>();
    }

    private static bool SameSign(double left, double right)
    {
        if (Math.Abs(left) < 0.0000001 && Math.Abs(right) < 0.0000001)
            return true;

        return Math.Sign(left) == Math.Sign(right);
    }

    private static string[] JsonArrayToStringArray(JsonNode? node)
    {
        if (node is not JsonArray array)
            return [];

        return array
            .Select(item => item?.GetValue<string>() ?? "")
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .ToArray();
    }

    private static bool Bool(IReadOnlyDictionary<string, object?> source, string key) =>
        source.TryGetValue(key, out var value) && value is bool boolValue && boolValue;

    private static string StringValue(IReadOnlyDictionary<string, object?> source, string key) =>
        source.TryGetValue(key, out var value) ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? "" : "";

    private static int IntFromPath(JsonObject root, string path)
    {
        JsonNode? current = root;

        foreach (var segment in path.Split('.'))
        {
            if (current is not JsonObject currentObject || !currentObject.TryGetPropertyValue(segment, out current))
                return 0;
        }

        return current?.GetValue<int>() ?? 0;
    }

    private static double Round(double value) =>
        Math.Round(value, 6, MidpointRounding.AwayFromZero);

    private static string NormalizePath(string path) =>
        path.Replace("\\", "/");

    private static void EnsureDirectory(string path, string description)
    {
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException($"{description} not found: {path}");
    }

    private static void EnsureFile(string path, string description)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"{description} not found: {path}", path);
    }

    private static void WriteStep(string message)
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"==> {message}");
        Console.ResetColor();
    }

    private static void WriteSuccess(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine(message);
        Console.ResetColor();
    }

    private static int RunProcess(string fileName, string arguments)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = new Process
        {
            StartInfo = startInfo
        };

        process.OutputDataReceived += (_, args) =>
        {
            if (args.Data is not null)
                Console.WriteLine(args.Data);
        };

        process.ErrorDataReceived += (_, args) =>
        {
            if (args.Data is not null)
                Console.Error.WriteLine(args.Data);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        process.WaitForExit();

        return process.ExitCode;
    }
}
