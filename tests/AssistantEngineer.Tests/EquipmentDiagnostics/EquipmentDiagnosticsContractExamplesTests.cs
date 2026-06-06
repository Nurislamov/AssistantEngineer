using System.Text.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Contracts;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Guidance;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Staging;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Services;
using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public class EquipmentDiagnosticsContractExamplesTests
{
    [Fact]
    public void DocumentationExamplesAreValidJson()
    {
        foreach (var file in GetExampleJsonFiles())
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file));

            Assert.NotEqual(JsonValueKind.Undefined, document.RootElement.ValueKind);
        }
    }

    [Fact]
    public void DiagnosticCaseExampleDocumentsOperatorFacingContractFields()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(DiagnosticCaseExamplePath));
        var root = document.RootElement;

        foreach (var propertyName in OperatorFacingCasePropertyNames)
        {
            Assert.True(root.TryGetProperty(propertyName, out _), $"Missing '{propertyName}' in diagnostic case example.");
        }

        var source = root.GetProperty("source");
        Assert.Equal("SeededEngineeringKnowledge", source.GetProperty("sourceType").GetString());
        Assert.Equal("UnverifiedSeed", source.GetProperty("evidenceLevel").GetString());
        Assert.Equal(JsonValueKind.Null, source.GetProperty("manualTitle").ValueKind);
        Assert.Equal(JsonValueKind.Null, source.GetProperty("page").ValueKind);
        Assert.Equal(JsonValueKind.Null, source.GetProperty("quote").ValueKind);
        Assert.NotEmpty(source.GetProperty("limitations").EnumerateArray());
        Assert.NotEmpty(source.GetProperty("applicableSeries").EnumerateArray());
        Assert.Empty(source.GetProperty("applicableModels").EnumerateArray());
        Assert.False(root.GetProperty("isManualVerified").GetBoolean());
        Assert.True(root.GetProperty("isSeedKnowledge").GetBoolean());
        Assert.True(root.GetProperty("verificationRequired").GetBoolean());
    }

    [Fact]
    public async Task OperatorGuidanceExampleMatchesDeterministicFormatterContract()
    {
        var diagnosticCase = await GetDiagnosticCaseAsync("GMV", "H5");
        var message = EquipmentDiagnosticOperatorGuidanceFormatter.Format(diagnosticCase);
        using var document = JsonDocument.Parse(File.ReadAllText(OperatorGuidanceExamplePath));
        var root = document.RootElement;

        Assert.Equal(message.Title, root.GetProperty("title").GetString());
        Assert.Equal(message.Summary, root.GetProperty("summary").GetString());
        Assert.Equal(message.VerificationBanner, root.GetProperty("verificationBanner").GetString());
        Assert.Equal(message.SourceLine, root.GetProperty("sourceLine").GetString());
        Assert.Equal(message.RecommendedChecks, ReadStringArray(root.GetProperty("recommendedChecks")));
        Assert.Equal(message.SafetyLine, root.GetProperty("safetyLine").GetString());
        Assert.Equal(message.OperatorNotes, ReadStringArray(root.GetProperty("operatorNotes")));
        Assert.Equal(message.Footer, root.GetProperty("footer").GetString());
    }

    [Fact]
    public void StagingValidationReportExampleDocumentsDeterministicReportShape()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(StagingValidationReportExamplePath));
        var root = document.RootElement;

        Assert.Equal(1, root.GetProperty("totalCandidates").GetInt32());
        Assert.Equal(0, root.GetProperty("errorCount").GetInt32());
        Assert.Equal(0, root.GetProperty("warningCount").GetInt32());
        Assert.Equal(0, root.GetProperty("infoCount").GetInt32());
        Assert.Contains("GREE/GMV/VrfOutdoorUnit//SAMPLERFR", ReadStringArray(root.GetProperty("candidateKeys")));
        Assert.Empty(root.GetProperty("issuesByCandidateKey").EnumerateArray());
        Assert.False(root.GetProperty("promotionReady").GetBoolean());
        Assert.False(root.GetProperty("hasBlockingIssues").GetBoolean());
    }

    [Fact]
    public void DocumentationExamplesDoNotClaimManualVerifiedOrInventManualEvidence()
    {
        var violations = GetExampleJsonFiles()
            .SelectMany(file => FindForbiddenExampleClaims(file))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"EquipmentDiagnostics examples contain forbidden manual verification claims: {string.Join(", ", violations)}.");
    }

    [Fact]
    public void DocumentationExamplesDoNotContainUnsafeDiagnosticWording()
    {
        var violations = GetExampleFiles()
            .SelectMany(file =>
            {
                var text = File.ReadAllText(file);
                return UnsafeFragments
                    .Where(fragment => text.Contains(fragment, StringComparison.OrdinalIgnoreCase))
                    .Select(fragment => $"{Path.GetFileName(file)}:{fragment}");
            })
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"EquipmentDiagnostics examples contain unsafe diagnostic wording fragments: {string.Join(", ", violations)}.");
    }

    [Fact]
    public async Task ReleaseReadinessRuntimeCatalogAndCaseResponsesRemainBotConsumable()
    {
        using var serviceProvider = CreateServiceProvider();
        var service = serviceProvider.GetRequiredService<IEquipmentDiagnosticsService>();
        var index = await service.GetCatalogIndexAsync(CancellationToken.None);
        var runtimeEntries = new EquipmentDiagnosticsJsonKnowledgeSource().GetEntries();

        Assert.True(index.TotalEntries >= 23);
        Assert.All(runtimeEntries, entry => Assert.NotNull(entry.Source));

        foreach (var entry in runtimeEntries.Where(entry =>
                     entry.Source.SourceType == "SeededEngineeringKnowledge" &&
                     entry.Confidence == DiagnosticConfidence.Low))
        {
            var diagnosticCase = await service.GetDiagnosticCaseAsync(
                entry.Manufacturer,
                entry.Code,
                entry.SeriesName,
                entry.ModelCode,
                CancellationToken.None);

            Assert.NotNull(diagnosticCase);
            Assert.True(diagnosticCase.VerificationRequired);
            Assert.False(diagnosticCase.IsManualVerified);
            Assert.True(diagnosticCase.IsSeedKnowledge);
            Assert.NotEmpty(diagnosticCase.ShortSummary);
            Assert.NotEmpty(diagnosticCase.RecommendedNextChecks);
            Assert.NotEmpty(diagnosticCase.ConfidenceExplanation);
            Assert.NotEmpty(diagnosticCase.SourceSummary);
            Assert.NotEmpty(diagnosticCase.ApplicabilitySummary);
            Assert.NotEmpty(diagnosticCase.SafetyBoundary);
            Assert.NotEmpty(diagnosticCase.OperatorNotes);
        }
    }

    [Fact]
    public async Task ReleaseReadinessFormatterOutputHasRequiredOperatorSections()
    {
        foreach (var (series, code) in new[] { ("GMV", "H5"), ("Chiller", "E6"), ("Indoor", "H6") })
        {
            var diagnosticCase = await GetDiagnosticCaseAsync(series, code);
            var message = EquipmentDiagnosticOperatorGuidanceFormatter.Format(diagnosticCase);

            Assert.False(string.IsNullOrWhiteSpace(message.Title));
            Assert.False(string.IsNullOrWhiteSpace(message.Summary));
            Assert.False(string.IsNullOrWhiteSpace(message.VerificationBanner));
            Assert.False(string.IsNullOrWhiteSpace(message.SourceLine));
            Assert.NotEmpty(message.RecommendedChecks);
            Assert.False(string.IsNullOrWhiteSpace(message.SafetyLine));
            Assert.NotEmpty(message.OperatorNotes);
            Assert.False(string.IsNullOrWhiteSpace(message.Footer));
        }
    }

    [Fact]
    public async Task DocumentationExamplesDoNotPolluteRuntimeCatalogOrEmbeddedKnowledge()
    {
        var resources = EquipmentDiagnosticsJsonKnowledgeSource.GetEmbeddedKnowledgeResourceNames();
        var loader = new EquipmentDiagnosticsKnowledgeJsonLoader();
        var productionFileEntries = GetProductionKnowledgeJsonFiles()
            .SelectMany(file => loader.LoadFromJson(
                File.ReadAllText(file),
                Path.GetRelativePath(TestPaths.RepoRoot, file)))
            .Count();
        using var serviceProvider = CreateServiceProvider();
        var service = serviceProvider.GetRequiredService<IEquipmentDiagnosticsService>();

        var index = await service.GetCatalogIndexAsync(CancellationToken.None);

        Assert.Equal(productionFileEntries, index.TotalEntries);
        Assert.DoesNotContain(resources, resource =>
            resource.Contains(".docs.", StringComparison.OrdinalIgnoreCase) ||
            resource.Contains(".examples.", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(resources, resource =>
            resource.EndsWith("diagnostic-case-response.example.json", StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> FindForbiddenExampleClaims(string file)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(file));

        foreach (var claim in FindForbiddenExampleClaims(file, document.RootElement))
        {
            yield return claim;
        }
    }

    private static IEnumerable<string> FindForbiddenExampleClaims(string file, JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.NameEquals("manualTitle") ||
                    property.NameEquals("manualDocumentCode") ||
                    property.NameEquals("page") ||
                    property.NameEquals("section") ||
                    property.NameEquals("quote"))
                {
                    if (property.Value.ValueKind != JsonValueKind.Null)
                    {
                        yield return $"{Path.GetFileName(file)}:{property.Name}";
                    }
                }

                foreach (var claim in FindForbiddenExampleClaims(file, property.Value))
                {
                    yield return claim;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var claim in FindForbiddenExampleClaims(file, item))
                {
                    yield return claim;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.String)
        {
            var value = element.GetString();
            if (string.Equals(value, "ManualVerified", StringComparison.Ordinal) ||
                string.Equals(value, "ManualPageVerified", StringComparison.Ordinal) ||
                string.Equals(value, "CrossChecked", StringComparison.Ordinal))
            {
                yield return $"{Path.GetFileName(file)}:{value}";
            }
        }
    }

    private static async Task<EquipmentDiagnosticCaseDto> GetDiagnosticCaseAsync(string series, string code)
    {
        using var serviceProvider = CreateServiceProvider();
        var service = serviceProvider.GetRequiredService<IEquipmentDiagnosticsService>();

        var diagnosticCase = await service.GetDiagnosticCaseAsync(
            "Gree",
            code,
            series,
            modelCode: null,
            CancellationToken.None);

        return Assert.IsType<EquipmentDiagnosticCaseDto>(diagnosticCase);
    }

    private static IReadOnlyList<string> ReadStringArray(JsonElement array) =>
        array.EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddEquipmentDiagnosticsModule();
        return services.BuildServiceProvider();
    }

    private static IReadOnlyList<string> GetExampleJsonFiles() =>
        Directory.GetFiles(ExamplesRoot, "*.json", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

    private static IReadOnlyList<string> GetExampleFiles() =>
        Directory.GetFiles(ExamplesRoot, "*", SearchOption.TopDirectoryOnly)
            .Where(path => path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                           path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

    private static IReadOnlyList<string> GetProductionKnowledgeJsonFiles()
    {
        var knowledgeRoot = Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.EquipmentDiagnostics",
            "Knowledge");

        return Directory.GetFiles(knowledgeRoot, "*.json", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith(".schema.json", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Contains("staging", StringComparer.OrdinalIgnoreCase))
            .Where(path => !path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                .Contains("manual-codebook", StringComparer.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private static readonly string[] OperatorFacingCasePropertyNames =
    [
        "shortSummary",
        "recommendedNextChecks",
        "confidenceExplanation",
        "sourceSummary",
        "applicabilitySummary",
        "safetyBoundary",
        "operatorNotes",
        "isManualVerified",
        "isSeedKnowledge",
        "verificationRequired"
    ];

    private static readonly string[] UnsafeFragments =
    [
        "bypass",
        "disable protection",
        "disable protections",
        "force run",
        "short protection",
        "ignore protection"
    ];

    private static string ExamplesRoot =>
        Path.Combine(TestPaths.RepoRoot, "docs", "equipment-diagnostics", "examples");

    private static string DiagnosticCaseExamplePath =>
        Path.Combine(ExamplesRoot, "diagnostic-case-response.example.json");

    private static string OperatorGuidanceExamplePath =>
        Path.Combine(ExamplesRoot, "operator-guidance-message.example.json");

    private static string StagingValidationReportExamplePath =>
        Path.Combine(ExamplesRoot, "staging-validation-report.example.json");
}
