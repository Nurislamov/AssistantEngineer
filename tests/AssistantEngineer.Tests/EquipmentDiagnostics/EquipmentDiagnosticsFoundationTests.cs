using System.Reflection;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Contracts;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Services;
using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;
using AssistantEngineer.Modules.EquipmentDiagnostics.Public;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public class EquipmentDiagnosticsFoundationTests
{
    [Fact]
    public async Task ModuleServiceCanSearchByManufacturerAndErrorCode()
    {
        var service = CreateServiceProvider().GetRequiredService<IEquipmentDiagnosticsService>();

        var results = await service.SearchErrorCodesAsync(
            new SearchEquipmentErrorCodesQuery(
                Manufacturer: "Gree",
                ErrorCode: "H5"),
            CancellationToken.None);

        var result = Assert.Single(results);
        Assert.Equal("Gree", result.Manufacturer);
        Assert.Equal("GMV", result.SeriesName);
        Assert.Equal("H5", result.Code);
    }

    [Fact]
    public async Task SearchIsCaseInsensitiveAndWhitespaceInsensitive()
    {
        var facade = CreateServiceProvider().GetRequiredService<IEquipmentDiagnosticsFacade>();

        var results = await facade.SearchErrorCodesAsync(
            new SearchEquipmentErrorCodesQuery(
                Manufacturer: " g r e e ",
                ErrorCode: " h 5 ",
                Series: " g m v "),
            CancellationToken.None);

        var result = Assert.Single(results);
        Assert.Equal("H5", result.Code);
        Assert.Equal("GMV", result.SeriesName);
    }

    [Fact]
    public async Task UnknownCodeReturnsEmptyResult()
    {
        var service = CreateServiceProvider().GetRequiredService<IEquipmentDiagnosticsService>();

        var results = await service.SearchErrorCodesAsync(
            new SearchEquipmentErrorCodesQuery(
                Manufacturer: "Gree",
                ErrorCode: "Unknown"),
            CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task DiagnosticCaseIncludesDiagnosticFoundationData()
    {
        var service = CreateServiceProvider().GetRequiredService<IEquipmentDiagnosticsService>();

        var diagnosticCase = await service.GetDiagnosticCaseAsync(
            manufacturer: "Gree",
            errorCode: "H5",
            series: "GMV",
            modelCode: null,
            CancellationToken.None);

        Assert.NotNull(diagnosticCase);
        Assert.NotEmpty(diagnosticCase.LikelyCauses);
        Assert.NotEmpty(diagnosticCase.DiagnosticSteps);
        Assert.NotEmpty(diagnosticCase.RequiredMeasurements);
        Assert.NotEqual(DiagnosticConfidence.Unknown, diagnosticCase.Confidence);
    }

    [Fact]
    public async Task SeededGreeH5DoesNotClaimManualVerifiedConfidence()
    {
        var service = CreateServiceProvider().GetRequiredService<IEquipmentDiagnosticsService>();

        var diagnosticCase = await service.GetDiagnosticCaseAsync(
            manufacturer: "Gree",
            errorCode: "H5",
            series: "GMV",
            modelCode: null,
            CancellationToken.None);

        Assert.NotNull(diagnosticCase);
        Assert.NotEqual(DiagnosticConfidence.ManualVerified, diagnosticCase.Confidence);
        Assert.NotEqual(DiagnosticConfidence.ManualVerified, diagnosticCase.ErrorCode.Confidence);
    }

    [Fact]
    public void EquipmentDiagnosticsModuleDoesNotReferenceForbiddenBackendProjects()
    {
        var assembly = typeof(IEquipmentDiagnosticsFacade).Assembly;

        var referencedAssemblies = assembly
            .GetReferencedAssemblies()
            .Select(assemblyName => assemblyName.Name)
            .Where(name => name is not null)
            .ToHashSet(StringComparer.Ordinal);

        var forbiddenReferences = new[]
        {
            "AssistantEngineer.Modules.Calculations",
            "AssistantEngineer.Modules.Buildings",
            "AssistantEngineer.Infrastructure",
            "AssistantEngineer.Api"
        };

        var violations = forbiddenReferences
            .Where(referencedAssemblies.Contains)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"{assembly.GetName().Name} references forbidden projects: {string.Join(", ", violations)}.");
    }

    [Fact]
    public void KnowledgeSourceExposesSeededGreeEntries()
    {
        var source = new InMemoryEquipmentDiagnosticsKnowledgeSource();

        var entries = source.GetEntries();

        Assert.Contains(entries, entry =>
            entry.Manufacturer == "Gree" &&
            entry.SeriesName == "GMV" &&
            entry.Code == "H5" &&
            entry.Category == EquipmentCategory.VrfOutdoorUnit);
        Assert.Contains(entries, entry =>
            entry.Manufacturer == "Gree" &&
            entry.SeriesName == "GMV" &&
            entry.Code == "C7" &&
            entry.Category == EquipmentCategory.VrfOutdoorUnit);
        Assert.Contains(entries, entry =>
            entry.Manufacturer == "Gree" &&
            entry.SeriesName == "Chiller" &&
            entry.Code == "E6" &&
            entry.Category == EquipmentCategory.Chiller);
    }

    [Fact]
    public void JsonKnowledgeSourceLoadsSeededGreeEntries()
    {
        var source = new EquipmentDiagnosticsJsonKnowledgeSource();

        var entries = source.GetEntries();

        Assert.Contains(entries, entry =>
            entry.Manufacturer == "Gree" &&
            entry.SeriesName == "GMV" &&
            entry.Code == "H5" &&
            entry.Category == EquipmentCategory.VrfOutdoorUnit);
        Assert.Contains(entries, entry =>
            entry.Manufacturer == "Gree" &&
            entry.SeriesName == "GMV" &&
            entry.Code == "C7" &&
            entry.Category == EquipmentCategory.VrfOutdoorUnit);
        Assert.Contains(entries, entry =>
            entry.Manufacturer == "Gree" &&
            entry.SeriesName == "Chiller" &&
            entry.Code == "E6" &&
            entry.Category == EquipmentCategory.Chiller);
    }

    [Fact]
    public void JsonKnowledgeFilesAreValidAccordingToModuleRules()
    {
        var loader = new EquipmentDiagnosticsKnowledgeJsonLoader();

        foreach (var file in GetKnowledgeJsonFiles())
        {
            var entries = loader.LoadFromJson(
                File.ReadAllText(file),
                Path.GetRelativePath(global::AssistantEngineer.Tests.TestPaths.RepoRoot, file));

            Assert.NotEmpty(entries);
        }
    }

    [Fact]
    public void JsonKnowledgeEntriesContainRequiredDiagnosticData()
    {
        var source = new EquipmentDiagnosticsJsonKnowledgeSource();

        foreach (var entry in source.GetEntries())
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.Manufacturer));
            Assert.False(string.IsNullOrWhiteSpace(entry.Code));
            Assert.False(string.IsNullOrWhiteSpace(entry.Title));
            Assert.False(string.IsNullOrWhiteSpace(entry.Meaning));
            Assert.True(Enum.IsDefined(entry.Confidence));
            Assert.True(Enum.IsDefined(entry.Category));
            Assert.NotEmpty(entry.SafetyNotes);
            Assert.NotEmpty(entry.DiagnosticSteps);
            Assert.NotEmpty(entry.RequiredMeasurements);
            Assert.NotNull(entry.Source);
            Assert.False(string.IsNullOrWhiteSpace(entry.Source.SourceType));
            Assert.False(string.IsNullOrWhiteSpace(entry.Source.EvidenceLevel));
            Assert.NotEmpty(entry.Source.Limitations);
            Assert.NotNull(entry.Source.ApplicableModels);
            Assert.NotNull(entry.Source.ApplicableSeries);
        }
    }

    [Fact]
    public void SeededGreeEntriesUseUnverifiedSeedProvenance()
    {
        var entries = new EquipmentDiagnosticsJsonKnowledgeSource().GetEntries();

        foreach (var entry in GetSeededGreeEntries(entries))
        {
            Assert.Equal("SeededEngineeringKnowledge", entry.Source.SourceType);
            Assert.Equal("UnverifiedSeed", entry.Source.EvidenceLevel);
            Assert.Equal(DiagnosticConfidence.Low, entry.Confidence);
            Assert.NotEqual(DiagnosticConfidence.ManualVerified, entry.Confidence);
            Assert.Null(entry.Source.ManualTitle);
            Assert.Null(entry.Source.Page);
            Assert.Null(entry.Source.Quote);
            Assert.NotEmpty(entry.Source.Limitations);
        }
    }

    [Fact]
    public void SeededEntriesDoNotInventManualEvidence()
    {
        var seededEntries = GetSeededGreeEntries(new EquipmentDiagnosticsJsonKnowledgeSource().GetEntries());

        foreach (var entry in seededEntries)
        {
            Assert.Empty(entry.ManualReferences);
            Assert.Null(entry.Source.ManualTitle);
            Assert.Null(entry.Source.ManualVersion);
            Assert.Null(entry.Source.ManualDocumentCode);
            Assert.Null(entry.Source.Page);
            Assert.Null(entry.Source.Section);
            Assert.Null(entry.Source.Quote);
        }
    }

    [Fact]
    public async Task ServiceUsesKnowledgeSourceForSearchAndDiagnosticCases()
    {
        var service = new InMemoryEquipmentDiagnosticsService(
            new StubKnowledgeSource(
            [
                CreateKnowledgeEntry(
                    manufacturer: "Test Manufacturer",
                    seriesName: "Alpha",
                    category: EquipmentCategory.SplitSystem,
                    code: "T1")
            ]));

        var searchResults = await service.SearchErrorCodesAsync(
            new SearchEquipmentErrorCodesQuery(
                Manufacturer: "test manufacturer",
                ErrorCode: "t 1",
                Series: "alpha"),
            CancellationToken.None);
        var diagnosticCase = await service.GetDiagnosticCaseAsync(
            manufacturer: "Test Manufacturer",
            errorCode: "T1",
            series: "Alpha",
            modelCode: null,
            CancellationToken.None);

        var result = Assert.Single(searchResults);
        Assert.Equal("Test Manufacturer", result.Manufacturer);
        Assert.Equal("T1", result.Code);
        Assert.Equal(EquipmentCategory.SplitSystem, result.Category);
        Assert.NotNull(diagnosticCase);
        Assert.Equal("T1", diagnosticCase.ErrorCode.Code);
        Assert.NotEmpty(diagnosticCase.DiagnosticSteps);
    }

    [Fact]
    public void KnowledgeSourceEntriesDoNotClaimManualVerifiedConfidence()
    {
        var source = new InMemoryEquipmentDiagnosticsKnowledgeSource();

        var violations = source.GetEntries()
            .Where(entry => entry.Confidence == DiagnosticConfidence.ManualVerified)
            .Select(entry => $"{entry.Manufacturer}/{entry.SeriesName}/{entry.Code}")
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Seeded diagnostics must not claim ManualVerified confidence: {string.Join(", ", violations)}.");
    }

    [Fact]
    public void JsonKnowledgeEntriesDoNotClaimManualVerifiedConfidence()
    {
        var source = new EquipmentDiagnosticsJsonKnowledgeSource();

        var violations = source.GetEntries()
            .Where(entry => entry.Confidence == DiagnosticConfidence.ManualVerified)
            .Select(entry => $"{entry.Manufacturer}/{entry.SeriesName}/{entry.Code}")
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"JSON diagnostics must not claim ManualVerified confidence: {string.Join(", ", violations)}.");
    }

    [Fact]
    public void SeededKnowledgeDoesNotContainBypassOrDisableProtectionWording()
    {
        var source = new InMemoryEquipmentDiagnosticsKnowledgeSource();
        var forbiddenFragments = new[]
        {
            "bypass",
            "disable protection",
            "disable protections",
            "disable-protection",
            "disabling protection",
            "disabling protections",
            "force run",
            "short protection",
            "ignore protection"
        };

        var searchableTexts = source.GetEntries()
            .SelectMany(entry => entry.SafetyNotes
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
                .Concat(new[]
                {
                    entry.Source.SourceType,
                    entry.Source.EvidenceLevel,
                    entry.Source.ManualTitle ?? string.Empty,
                    entry.Source.ManualVersion ?? string.Empty,
                    entry.Source.ManualDocumentCode ?? string.Empty,
                    entry.Source.Page ?? string.Empty,
                    entry.Source.Section ?? string.Empty,
                    entry.Source.Quote ?? string.Empty,
                    entry.Source.Notes ?? string.Empty
                })
                .Concat(entry.Source.Limitations)
                .Concat(entry.Source.ApplicableModels)
                .Concat(entry.Source.ApplicableSeries))
            .ToArray();

        var violations = forbiddenFragments
            .Where(fragment => searchableTexts.Any(text =>
                text.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Seeded diagnostic text contains unsafe wording fragments: {string.Join(", ", violations)}.");
    }

    [Fact]
    public void JsonLoaderRejectsEntriesWithoutSource()
    {
        var json = CreateSingleEntryJson(sourceJson: null);
        var loader = new EquipmentDiagnosticsKnowledgeJsonLoader();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            loader.LoadFromJson(json, "missing-source.json"));

        Assert.Contains(".source must be present", exception.Message);
    }

    [Fact]
    public void JsonLoaderRejectsManualVerifiedWithoutVerifiedEvidence()
    {
        var json = CreateSingleEntryJson(
            confidence: "ManualVerified",
            sourceJson: CreateSourceJson(evidenceLevel: "UnverifiedSeed"));
        var loader = new EquipmentDiagnosticsKnowledgeJsonLoader();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            loader.LoadFromJson(json, "manual-verified-without-evidence.json"));

        Assert.Contains("ManualVerified", exception.Message);
        Assert.Contains("ManualPageVerified or CrossChecked", exception.Message);
    }

    [Fact]
    public void JsonLoaderRejectsManualPageVerifiedWithoutManualTitleAndPage()
    {
        var json = CreateSingleEntryJson(sourceJson: CreateSourceJson(evidenceLevel: "ManualPageVerified"));
        var loader = new EquipmentDiagnosticsKnowledgeJsonLoader();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            loader.LoadFromJson(json, "manual-page-verified-without-page.json"));

        Assert.Contains("manualTitle and page", exception.Message);
    }

    [Fact]
    public void InMemoryServiceDoesNotContainSeedCaseConstruction()
    {
        var servicePath = Path.Combine(
            global::AssistantEngineer.Tests.TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.EquipmentDiagnostics",
            "Application",
            "Services",
            "InMemoryEquipmentDiagnosticsService.cs");

        var text = File.ReadAllText(servicePath);
        var forbiddenFragments = new[]
        {
            "BuildSeedCases",
            "CreateGreeGmvCase",
            "CreateGreeChillerCase",
            "GMV protection alarm H5",
            "GMV communication or configuration alarm C7",
            "Chiller protection alarm E6"
        };

        var violations = forbiddenFragments
            .Where(fragment => text.Contains(fragment, StringComparison.Ordinal))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Service should not contain direct seed construction: {string.Join(", ", violations)}.");
    }

    [Fact]
    public void KnowledgeCatalogDoesNotContainDirectSeedConstruction()
    {
        var catalogPath = Path.Combine(
            global::AssistantEngineer.Tests.TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.EquipmentDiagnostics",
            "Application",
            "Knowledge",
            "EquipmentDiagnosticsKnowledgeCatalog.cs");

        var text = File.ReadAllText(catalogPath);
        var forbiddenFragments = new[]
        {
            "GMV protection alarm H5",
            "GMV communication or configuration alarm C7",
            "Chiller protection alarm E6",
            "CreateGreeGmvEntry"
        };

        var violations = forbiddenFragments
            .Where(fragment => text.Contains(fragment, StringComparison.Ordinal))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Knowledge catalog helper should not contain direct seed data: {string.Join(", ", violations)}.");
    }

    [Fact]
    public void JsonKnowledgeResourcesAreEmbedded()
    {
        var resources = EquipmentDiagnosticsJsonKnowledgeSource.GetEmbeddedKnowledgeResourceNames();

        Assert.Contains(resources, name => name.EndsWith("Knowledge.equipment-diagnostics.schema.json", StringComparison.Ordinal));
        Assert.Contains(resources, name => name.EndsWith("Knowledge.gree.gree-gmv.json", StringComparison.Ordinal));
        Assert.Contains(resources, name => name.EndsWith("Knowledge.gree.gree-chiller.json", StringComparison.Ordinal));
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddEquipmentDiagnosticsModule();
        return services.BuildServiceProvider();
    }

    private static IReadOnlyList<string> GetKnowledgeJsonFiles()
    {
        var knowledgeRoot = Path.Combine(
            global::AssistantEngineer.Tests.TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.EquipmentDiagnostics",
            "Knowledge");

        return Directory.GetFiles(knowledgeRoot, "*.json", SearchOption.AllDirectories)
            .Where(path => !path.EndsWith(".schema.json", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private static EquipmentDiagnosticsKnowledgeEntry CreateKnowledgeEntry(
        string manufacturer,
        string seriesName,
        EquipmentCategory category,
        string code) =>
        new(
            Manufacturer: manufacturer,
            SeriesName: seriesName,
            ModelCode: null,
            Category: category,
            Code: code,
            Title: "Test diagnostic",
            Meaning: "Test diagnostic meaning.",
            Severity: "Service attention required",
            Confidence: DiagnosticConfidence.Low,
            LikelyCauses: ["Test cause."],
            DiagnosticSteps:
            [
                new DiagnosticStep(
                    Order: 1,
                    Title: "Confirm test condition",
                    Instruction: "Record the displayed test code.",
                    ExpectedResult: "The test code is confirmed.",
                    IfFailedAction: "Stop classification and obtain correct equipment information.")
            ],
            RequiredMeasurements:
            [
                new RequiredMeasurement(
                    Name: "Test measurement",
                    Unit: "V",
                    Description: "Test measurement description.",
                    RequiredBeforeConclusion: true)
            ],
            SafetyNotes: ["Electrical checks must be performed by a qualified technician."],
            ManualReferences:
            [
                new ManualReference(
                    Manufacturer: manufacturer,
                    ManualTitle: "Test service manual",
                    ManualVersion: null,
                    Page: null,
                    Notes: "Test source.")
            ],
            Source: new EquipmentDiagnosticsKnowledgeSourceInfo(
                SourceType: "SeededEngineeringKnowledge",
                EvidenceLevel: "UnverifiedSeed",
                ManualTitle: null,
                ManualVersion: null,
                ManualDocumentCode: null,
                Page: null,
                Section: null,
                Quote: null,
                Notes: "Test source.",
                Limitations: ["Use as preliminary diagnostic guidance only."],
                ApplicableModels: [],
                ApplicableSeries: [seriesName]),
            Tags: ["test"]);

    private static IReadOnlyList<EquipmentDiagnosticsKnowledgeEntry> GetSeededGreeEntries(
        IReadOnlyCollection<EquipmentDiagnosticsKnowledgeEntry> entries) =>
        entries
            .Where(entry =>
                entry.Manufacturer == "Gree" &&
                ((entry.SeriesName == "GMV" && entry.Code is "H5" or "C7") ||
                 (entry.SeriesName == "Chiller" && entry.Code == "E6")))
            .OrderBy(entry => entry.Code, StringComparer.Ordinal)
            .ToArray();

    private static string CreateSingleEntryJson(
        string confidence = "Low",
        string? sourceJson = null)
    {
        var sourceProperty = sourceJson is null
            ? string.Empty
            : $"""
                  "source": {sourceJson},
              """;

        return $$"""
        {
          "entries": [
            {
              "manufacturer": "Test",
              "seriesName": "Alpha",
              "modelCode": null,
              "category": "SplitSystem",
              "code": "T1",
              "title": "Test diagnostic",
              "meaning": "Test diagnostic meaning.",
              "severity": "Service attention required",
              "confidence": "{{confidence}}",
              "likelyCauses": [
                "Test cause."
              ],
              "diagnosticSteps": [
                {
                  "order": 1,
                  "title": "Confirm test condition",
                  "instruction": "Record the displayed test code.",
                  "expectedResult": "The test code is confirmed.",
                  "ifFailedAction": "Stop classification and obtain correct equipment information."
                }
              ],
              "requiredMeasurements": [
                {
                  "name": "Test measurement",
                  "unit": "V",
                  "description": "Test measurement description.",
                  "requiredBeforeConclusion": true
                }
              ],
              "safetyNotes": [
                "Electrical checks must be performed by a qualified technician."
              ],
              "manualReferences": [],
        {{sourceProperty}}
              "tags": [
                "test"
              ]
            }
          ]
        }
        """;
    }

    private static string CreateSourceJson(string evidenceLevel = "UnverifiedSeed") =>
        $$"""
        {
          "sourceType": "SeededEngineeringKnowledge",
          "evidenceLevel": "{{evidenceLevel}}",
          "manualTitle": null,
          "manualVersion": null,
          "manualDocumentCode": null,
          "page": null,
          "section": null,
          "quote": null,
          "notes": "Seeded deterministic diagnostic guidance. Not manually page-verified.",
          "limitations": [
            "Use as preliminary diagnostic guidance only."
          ],
          "applicableModels": [],
          "applicableSeries": [
            "Alpha"
          ]
        }
        """;

    private sealed class StubKnowledgeSource : IEquipmentDiagnosticsKnowledgeSource
    {
        private readonly IReadOnlyCollection<EquipmentDiagnosticsKnowledgeEntry> _entries;

        public StubKnowledgeSource(IReadOnlyCollection<EquipmentDiagnosticsKnowledgeEntry> entries)
        {
            _entries = entries;
        }

        public IReadOnlyCollection<EquipmentDiagnosticsKnowledgeEntry> GetEntries() => _entries;
    }
}
