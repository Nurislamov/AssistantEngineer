using System.Reflection;
using System.Text.Json;
using AssistantEngineer.Api.Controllers.Equipment;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Contracts;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Guidance;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Services;
using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public class EquipmentDiagnosticsFinalQaTests
{
    [Fact]
    public void RuntimeSeedEntriesUseUnverifiedLowConfidenceWithoutManualEvidence()
    {
        var entries = new EquipmentDiagnosticsJsonKnowledgeSource().GetEntries();

        var violations = entries
            .SelectMany(entry => GetRuntimeSeedProvenanceViolations(entry))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Runtime seed provenance violations: {string.Join(", ", violations)}.");
    }

    [Fact]
    public async Task RuntimeSeedCasesRequireVerificationAndFormatterShowsVerificationBanner()
    {
        using var serviceProvider = CreateServiceProvider();
        var service = serviceProvider.GetRequiredService<IEquipmentDiagnosticsService>();
        var seedEntries = new EquipmentDiagnosticsJsonKnowledgeSource()
            .GetEntries()
            .Where(entry => entry.Source.EvidenceLevel == "UnverifiedSeed")
            .ToArray();

        Assert.NotEmpty(seedEntries);
        foreach (var entry in seedEntries)
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

            var message = EquipmentDiagnosticOperatorGuidanceFormatter.Format(diagnosticCase);
            Assert.Contains("Verification required", message.VerificationBanner, StringComparison.Ordinal);
            Assert.Contains("seed knowledge", message.VerificationBanner, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(diagnosticCase.RecommendedNextChecks, message.RecommendedChecks);
            Assert.Equal(diagnosticCase.SafetyBoundary, message.SafetyLine);
        }
    }

    [Fact]
    public async Task RuntimeSearchAndIndexDoNotExposeStagingSampleCodes()
    {
        using var serviceProvider = CreateServiceProvider();
        var service = serviceProvider.GetRequiredService<IEquipmentDiagnosticsService>();
        var stagingCodes = GetStagingExampleCodes();

        var index = await service.GetCatalogIndexAsync(CancellationToken.None);

        Assert.NotEmpty(stagingCodes);
        foreach (var code in stagingCodes)
        {
            Assert.DoesNotContain(index.Codes, facet =>
                string.Equals(facet.Code, code, StringComparison.OrdinalIgnoreCase));

            var searchResults = await service.SearchErrorCodesAsync(
                new SearchEquipmentErrorCodesQuery(
                    Manufacturer: "Gree",
                    ErrorCode: code,
                    Series: "GMV"),
                CancellationToken.None);

            Assert.Empty(searchResults);
        }
    }

    [Fact]
    public void StagingAndDocumentationExamplesAreValidJsonButNotEmbeddedResources()
    {
        var resources = EquipmentDiagnosticsJsonKnowledgeSource.GetEmbeddedKnowledgeResourceNames();

        foreach (var file in GetExampleJsonFiles())
        {
            using var document = JsonDocument.Parse(File.ReadAllText(file));
            Assert.NotEqual(JsonValueKind.Undefined, document.RootElement.ValueKind);
            Assert.DoesNotContain(resources, resource =>
                resource.Contains(Path.GetFileNameWithoutExtension(file), StringComparison.OrdinalIgnoreCase));
        }

        Assert.DoesNotContain(resources, resource =>
            resource.Contains(".Knowledge.staging.", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(resources, resource =>
            resource.Contains(".docs.", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(resources, resource =>
            resource.Contains(".tests.", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ExamplesDoNotContainUnsafeWordingOrLongManualQuotes()
    {
        var violations = GetExampleTextFiles()
            .SelectMany(file => GetExampleTextViolations(file))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"EquipmentDiagnostics examples contain unsafe wording or long quote-like manual text: {string.Join(", ", violations)}.");
    }

    [Fact]
    public void EquipmentDiagnosticsControllerKeepsExpectedRoutesAndOnlyOneBotPost()
    {
        var route = typeof(EquipmentDiagnosticsController).GetCustomAttribute<RouteAttribute>();
        var getTemplates = typeof(EquipmentDiagnosticsController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .SelectMany(method => method.GetCustomAttributes<HttpGetAttribute>())
            .Select(attribute => attribute.Template ?? string.Empty)
            .OrderBy(template => template, StringComparer.Ordinal)
            .ToArray();
        var postTemplates = typeof(EquipmentDiagnosticsController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .SelectMany(method => method.GetCustomAttributes<HttpPostAttribute>())
            .Select(attribute => attribute.Template ?? string.Empty)
            .OrderBy(template => template, StringComparer.Ordinal)
            .ToArray();

        Assert.NotNull(route);
        Assert.Equal("api/v{version:apiVersion}/equipment-diagnostics", route.Template);
        Assert.Equal(["cases", "catalog", "error-codes"], getTemplates);
        Assert.Equal(["bot/diagnose"], postTemplates);
    }

    private static IEnumerable<string> GetRuntimeSeedProvenanceViolations(
        EquipmentDiagnosticsKnowledgeEntry entry)
    {
        var key = $"{entry.Manufacturer}/{entry.SeriesName}/{entry.Category}/{entry.Code}";

        if (entry.Source.SourceType == "SeededEngineeringKnowledge")
        {
            if (entry.Source.EvidenceLevel != "UnverifiedSeed")
            {
                yield return $"{key}:seed-source-with-{entry.Source.EvidenceLevel}";
            }

            if (entry.Confidence != DiagnosticConfidence.Low)
            {
                yield return $"{key}:seed-confidence-{entry.Confidence}";
            }
        }

        if (entry.Source.EvidenceLevel == "UnverifiedSeed")
        {
            if (entry.Confidence == DiagnosticConfidence.ManualVerified)
            {
                yield return $"{key}:manual-verified-unverified-seed";
            }

            if (entry.Source.ManualTitle is not null ||
                entry.Source.ManualDocumentCode is not null ||
                entry.Source.Page is not null ||
                entry.Source.Section is not null ||
                entry.Source.Quote is not null)
            {
                yield return $"{key}:invented-manual-evidence";
            }
        }

        if (entry.Source.Limitations.Count == 0)
        {
            yield return $"{key}:missing-source-limitations";
        }

        if (entry.Confidence == DiagnosticConfidence.ManualVerified &&
            entry.Source.EvidenceLevel is not ("ManualPageVerified" or "CrossChecked"))
        {
            yield return $"{key}:manual-verified-without-verified-evidence";
        }
    }

    private static IReadOnlyList<string> GetStagingExampleCodes() =>
        Directory.GetFiles(StagingExamplesRoot, "*.json", SearchOption.TopDirectoryOnly)
            .SelectMany(file =>
            {
                using var document = JsonDocument.Parse(File.ReadAllText(file));
                return document.RootElement
                    .GetProperty("candidates")
                    .EnumerateArray()
                    .Select(candidate => candidate.GetProperty("code").GetString() ?? string.Empty)
                    .Where(code => !string.IsNullOrWhiteSpace(code))
                    .ToArray();
            })
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToArray();

    private static IEnumerable<string> GetExampleTextViolations(string file)
    {
        var text = File.ReadAllText(file);
        foreach (var fragment in UnsafeFragments)
        {
            if (text.Contains(fragment, StringComparison.OrdinalIgnoreCase))
            {
                yield return $"{Path.GetFileName(file)}:{fragment}";
            }
        }

        using var document = JsonDocument.Parse(text);
        foreach (var quote in EnumerateStringProperties(document.RootElement, "quote"))
        {
            if (quote.Length > 240)
            {
                yield return $"{Path.GetFileName(file)}:long-quote";
            }
        }
    }

    private static IEnumerable<string> EnumerateStringProperties(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.NameEquals(propertyName) &&
                    property.Value.ValueKind == JsonValueKind.String)
                {
                    yield return property.Value.GetString() ?? string.Empty;
                }

                foreach (var value in EnumerateStringProperties(property.Value, propertyName))
                {
                    yield return value;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var value in EnumerateStringProperties(item, propertyName))
                {
                    yield return value;
                }
            }
        }
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddEquipmentDiagnosticsModule();
        return services.BuildServiceProvider();
    }

    private static IReadOnlyList<string> GetExampleJsonFiles() =>
        Directory.GetFiles(DocsExamplesRoot, "*.json", SearchOption.TopDirectoryOnly)
            .Concat(Directory.GetFiles(StagingExamplesRoot, "*.json", SearchOption.TopDirectoryOnly))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

    private static IReadOnlyList<string> GetExampleTextFiles() =>
        Directory.GetFiles(DocsExamplesRoot, "*.json", SearchOption.TopDirectoryOnly)
            .Concat(Directory.GetFiles(StagingExamplesRoot, "*.json", SearchOption.TopDirectoryOnly))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

    private static readonly string[] UnsafeFragments =
    [
        "bypass",
        "disable protection",
        "disable protections",
        "force run",
        "short protection",
        "ignore protection"
    ];

    private static string DocsExamplesRoot =>
        Path.Combine(TestPaths.RepoRoot, "docs", "equipment-diagnostics", "examples");

    private static string StagingExamplesRoot =>
        Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.EquipmentDiagnostics",
            "Knowledge",
            "staging",
            "examples");
}
