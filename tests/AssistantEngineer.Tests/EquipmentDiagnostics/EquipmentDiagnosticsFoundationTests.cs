using System.Reflection;
using System.Text.RegularExpressions;
using System.Text.Json;
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
    public async Task SearchCodeNormalizationAllowsHyphenatedCodeInput()
    {
        var facade = CreateServiceProvider().GetRequiredService<IEquipmentDiagnosticsFacade>();

        var results = await facade.SearchErrorCodesAsync(
            new SearchEquipmentErrorCodesQuery(
                Manufacturer: "Gree",
                ErrorCode: " e-1 ",
                Series: "GMV"),
            CancellationToken.None);

        var result = Assert.Single(results);
        Assert.Equal("E1", result.Code);
        Assert.Equal("GMV", result.SeriesName);
    }

    [Theory]
    [InlineData("h5")]
    [InlineData("H-5")]
    [InlineData("H 5")]
    public async Task QuerySearchNormalizesCommonCodeFormattingForH5(string queryText)
    {
        var service = CreateServiceProvider().GetRequiredService<IEquipmentDiagnosticsService>();

        var results = await service.SearchErrorCodesAsync(
            new SearchEquipmentErrorCodesQuery(
                Manufacturer: "Gree",
                Series: "GMV",
                Query: queryText),
            CancellationToken.None);

        var result = Assert.Single(results);
        Assert.Equal("H5", result.Code);
        Assert.Equal("GMV", result.SeriesName);
    }

    [Fact]
    public async Task QuerySearchFindsGreeGmvH5FromNaturalPhrase()
    {
        var service = CreateServiceProvider().GetRequiredService<IEquipmentDiagnosticsService>();

        var results = await service.SearchErrorCodesAsync(
            new SearchEquipmentErrorCodesQuery(Query: "gree gmv h5"),
            CancellationToken.None);

        var result = Assert.Single(results);
        Assert.Equal("Gree", result.Manufacturer);
        Assert.Equal("GMV", result.SeriesName);
        Assert.Equal("H5", result.Code);
    }

    [Fact]
    public async Task QuerySearchUsesSeriesAndFriendlyCategoryWordsToDisambiguateSharedCodes()
    {
        var service = CreateServiceProvider().GetRequiredService<IEquipmentDiagnosticsService>();

        var results = await service.SearchErrorCodesAsync(
            new SearchEquipmentErrorCodesQuery(
                Manufacturer: "Gree",
                Query: "gmv outdoor e1"),
            CancellationToken.None);

        var result = Assert.Single(results);
        Assert.Equal("GMV", result.SeriesName);
        Assert.Equal("E1", result.Code);
        Assert.Equal(EquipmentCategory.VrfOutdoorUnit, result.Category);
    }

    [Fact]
    public async Task QuerySearchFindsChillerAndIndoorCodesFromFriendlyWords()
    {
        var service = CreateServiceProvider().GetRequiredService<IEquipmentDiagnosticsService>();

        var chillerResults = await service.SearchErrorCodesAsync(
            new SearchEquipmentErrorCodesQuery(
                Manufacturer: "Gree",
                Query: "chiller e6"),
            CancellationToken.None);
        var indoorResults = await service.SearchErrorCodesAsync(
            new SearchEquipmentErrorCodesQuery(
                Manufacturer: "Gree",
                Query: "indoor h6"),
            CancellationToken.None);

        var chiller = Assert.Single(chillerResults);
        Assert.Equal("Chiller", chiller.SeriesName);
        Assert.Equal("E6", chiller.Code);
        Assert.Equal(EquipmentCategory.Chiller, chiller.Category);

        var indoor = Assert.Single(indoorResults);
        Assert.Equal("Indoor", indoor.SeriesName);
        Assert.Equal("H6", indoor.Code);
        Assert.Equal(EquipmentCategory.VrfIndoorUnit, indoor.Category);
    }

    [Theory]
    [InlineData("gree gmv h5", "GMV", "H5")]
    [InlineData("gmv outdoor e1", "GMV", "E1")]
    [InlineData("chiller e6", "Chiller", "E6")]
    [InlineData("indoor h6", "Indoor", "H6")]
    public async Task AssistantBotReadinessExamplesUseDeterministicSearchAndOperatorFields(
        string queryText,
        string expectedSeries,
        string expectedCode)
    {
        var service = CreateServiceProvider().GetRequiredService<IEquipmentDiagnosticsService>();

        var searchResults = await service.SearchErrorCodesAsync(
            new SearchEquipmentErrorCodesQuery(
                Manufacturer: "Gree",
                Query: queryText),
            CancellationToken.None);

        var summary = Assert.Single(searchResults);
        Assert.Equal(expectedSeries, summary.SeriesName);
        Assert.Equal(expectedCode, summary.Code);

        var diagnosticCase = await service.GetDiagnosticCaseAsync(
            manufacturer: summary.Manufacturer,
            errorCode: summary.Code,
            series: summary.SeriesName,
            modelCode: summary.ModelCode,
            CancellationToken.None);

        Assert.NotNull(diagnosticCase);
        Assert.False(string.IsNullOrWhiteSpace(diagnosticCase.ShortSummary));
        Assert.NotEmpty(diagnosticCase.RecommendedNextChecks);
        Assert.False(string.IsNullOrWhiteSpace(diagnosticCase.SourceSummary));
        Assert.False(string.IsNullOrWhiteSpace(diagnosticCase.ConfidenceExplanation));
        Assert.True(diagnosticCase.VerificationRequired);
        Assert.True(diagnosticCase.IsSeedKnowledge);
        Assert.Contains("UnverifiedSeed", diagnosticCase.SourceSummary, StringComparison.Ordinal);
    }

    [Fact]
    public async Task QuerySearchUnknownTextReturnsEmptyResult()
    {
        var service = CreateServiceProvider().GetRequiredService<IEquipmentDiagnosticsService>();

        var results = await service.SearchErrorCodesAsync(
            new SearchEquipmentErrorCodesQuery(
                Manufacturer: "Gree",
                Query: "unknown diagnostic phrase"),
            CancellationToken.None);

        Assert.Empty(results);
    }

    [Fact]
    public async Task SeriesFilterAcceptsHyphenatedGmvInput()
    {
        var service = CreateServiceProvider().GetRequiredService<IEquipmentDiagnosticsService>();

        var results = await service.SearchErrorCodesAsync(
            new SearchEquipmentErrorCodesQuery(
                Manufacturer: "Gree",
                ErrorCode: "H5",
                Series: "G-M-V"),
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
    public async Task DiagnosticCaseIncludesOperatorFacingResponseGuidance()
    {
        var service = CreateServiceProvider().GetRequiredService<IEquipmentDiagnosticsService>();

        var diagnosticCase = await service.GetDiagnosticCaseAsync(
            manufacturer: "Gree",
            errorCode: "H5",
            series: "GMV",
            modelCode: null,
            CancellationToken.None);

        Assert.NotNull(diagnosticCase);
        Assert.Contains("Gree", diagnosticCase.ShortSummary, StringComparison.Ordinal);
        Assert.Contains("H5", diagnosticCase.ShortSummary, StringComparison.Ordinal);
        Assert.NotEmpty(diagnosticCase.RecommendedNextChecks);
        Assert.Contains(diagnosticCase.RecommendedNextChecks, check =>
            check.Contains("Step 1", StringComparison.Ordinal) ||
            check.Contains("Supply voltage", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("preliminary", diagnosticCase.ConfidenceExplanation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("verify", diagnosticCase.ConfidenceExplanation, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SeededEngineeringKnowledge", diagnosticCase.SourceSummary, StringComparison.Ordinal);
        Assert.Contains("UnverifiedSeed", diagnosticCase.SourceSummary, StringComparison.Ordinal);
        Assert.Contains("Applicable series", diagnosticCase.ApplicabilitySummary, StringComparison.Ordinal);
        Assert.Contains("qualified-technician", diagnosticCase.SafetyBoundary, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(diagnosticCase.OperatorNotes, note =>
            note.Contains("not manual page verified", StringComparison.OrdinalIgnoreCase));
        Assert.False(diagnosticCase.IsManualVerified);
        Assert.True(diagnosticCase.IsSeedKnowledge);
        Assert.True(diagnosticCase.VerificationRequired);
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
        foreach (var code in Ed05GreeGmvSeedCodes)
        {
            Assert.Contains(entries, entry =>
                entry.Manufacturer == "Gree" &&
                entry.SeriesName == "GMV" &&
                entry.Code == code &&
                entry.Category == EquipmentCategory.VrfOutdoorUnit);
        }

        foreach (var code in Ed09AGreeGmvSeedCodes)
        {
            Assert.Contains(entries, entry =>
                entry.Manufacturer == "Gree" &&
                entry.SeriesName == "GMV" &&
                entry.Code == code &&
                entry.Category == EquipmentCategory.VrfOutdoorUnit);
        }

        foreach (var code in Ed09AGreeChillerSeedCodes)
        {
            Assert.Contains(entries, entry =>
                entry.Manufacturer == "Gree" &&
                entry.SeriesName == "Chiller" &&
                entry.Code == code &&
                entry.Category == EquipmentCategory.Chiller);
        }

        foreach (var code in Ed09AGreeIndoorSeedCodes)
        {
            Assert.Contains(entries, entry =>
                entry.Manufacturer == "Gree" &&
                entry.SeriesName == "Indoor" &&
                entry.Code == code &&
                entry.Category == EquipmentCategory.VrfIndoorUnit);
        }
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
    public async Task CatalogIndexReturnsGreeGmvVrfOutdoorUnitAndKnownGmvCodes()
    {
        var service = CreateServiceProvider().GetRequiredService<IEquipmentDiagnosticsService>();

        var index = await service.GetCatalogIndexAsync(CancellationToken.None);

        Assert.True(index.TotalEntries >= 23);
        Assert.Contains(index.Manufacturers, facet =>
            facet.Manufacturer == "Gree" &&
            facet.NormalizedManufacturer == "GREE" &&
            facet.Count >= 23);
        Assert.Contains(index.Series, facet =>
            facet.Manufacturer == "Gree" &&
            facet.SeriesName == "GMV" &&
            facet.Count >= 14);
        Assert.Contains(index.Series, facet =>
            facet.Manufacturer == "Gree" &&
            facet.SeriesName == "Chiller" &&
            facet.Count >= 5);
        Assert.Contains(index.Series, facet =>
            facet.Manufacturer == "Gree" &&
            facet.SeriesName == "Indoor" &&
            facet.Count >= 4);
        Assert.Contains(index.Categories, facet =>
            facet.Category == EquipmentCategory.VrfOutdoorUnit &&
            facet.Count >= 14);
        Assert.Contains(index.Categories, facet =>
            facet.Category == EquipmentCategory.Chiller &&
            facet.Count >= 5);
        Assert.Contains(index.Categories, facet =>
            facet.Category == EquipmentCategory.VrfIndoorUnit &&
            facet.Count >= 4);

        foreach (var code in new[] { "H5", "C7", "E1", "E3", "E4", "E5" }.Concat(Ed09AGreeGmvSeedCodes))
        {
            Assert.Contains(index.Codes, facet =>
                facet.Manufacturer == "Gree" &&
                facet.SeriesName == "GMV" &&
                facet.Category == EquipmentCategory.VrfOutdoorUnit &&
                facet.Code == code &&
                facet.Count == 1);
        }

        foreach (var code in Ed09AGreeChillerSeedCodes)
        {
            Assert.Contains(index.Codes, facet =>
                facet.Manufacturer == "Gree" &&
                facet.SeriesName == "Chiller" &&
                facet.Category == EquipmentCategory.Chiller &&
                facet.Code == code &&
                facet.Count == 1);
        }

        foreach (var code in Ed09AGreeIndoorSeedCodes)
        {
            Assert.Contains(index.Codes, facet =>
                facet.Manufacturer == "Gree" &&
                facet.SeriesName == "Indoor" &&
                facet.Category == EquipmentCategory.VrfIndoorUnit &&
                facet.Code == code &&
                facet.Count == 1);
        }

        Assert.Contains("SeededEngineeringKnowledge", index.SourceTypes);
        Assert.Contains("UnverifiedSeed", index.EvidenceLevels);
    }

    [Fact]
    public async Task CatalogIndexOutputIsDeterministicallySorted()
    {
        var service = CreateServiceProvider().GetRequiredService<IEquipmentDiagnosticsService>();

        var index = await service.GetCatalogIndexAsync(CancellationToken.None);

        Assert.Equal(
            index.Manufacturers.OrderBy(facet => facet.Manufacturer, StringComparer.Ordinal),
            index.Manufacturers);
        Assert.Equal(
            index.Series
                .OrderBy(facet => facet.Manufacturer, StringComparer.Ordinal)
                .ThenBy(facet => facet.SeriesName ?? string.Empty, StringComparer.Ordinal),
            index.Series);
        Assert.Equal(
            index.Categories.OrderBy(facet => facet.Category.ToString(), StringComparer.Ordinal),
            index.Categories);
        Assert.Equal(
            index.Codes
                .OrderBy(facet => facet.Manufacturer, StringComparer.Ordinal)
                .ThenBy(facet => facet.SeriesName ?? string.Empty, StringComparer.Ordinal)
                .ThenBy(facet => facet.Category.ToString(), StringComparer.Ordinal)
                .ThenBy(facet => facet.NormalizedCode, StringComparer.Ordinal)
                .ThenBy(facet => facet.ModelCode ?? string.Empty, StringComparer.Ordinal),
            index.Codes);
    }

    [Fact]
    public async Task CatalogIndexRejectsDuplicateManufacturerSeriesCategoryCodeCombinations()
    {
        var service = new InMemoryEquipmentDiagnosticsService(
            new StubKnowledgeSource(
            [
                CreateKnowledgeEntry(
                    manufacturer: "Test Manufacturer",
                    seriesName: "Alpha",
                    category: EquipmentCategory.SplitSystem,
                    code: "T1"),
                CreateKnowledgeEntry(
                    manufacturer: " test manufacturer ",
                    seriesName: "A l p h a",
                    category: EquipmentCategory.SplitSystem,
                    code: "t-1")
            ]));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetCatalogIndexAsync(CancellationToken.None));

        Assert.Contains("duplicate manufacturer/series/category/code", exception.Message);
    }

    [Fact]
    public void KnowledgeEntriesDoNotContainDuplicateManufacturerSeriesCategoryCodeCombinations()
    {
        var source = new EquipmentDiagnosticsJsonKnowledgeSource();

        var duplicates = source.GetEntries()
            .GroupBy(
                entry => string.Join(
                    "/",
                    entry.Manufacturer,
                    entry.SeriesName ?? string.Empty,
                    entry.Category,
                    entry.Code),
                StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        Assert.True(
            duplicates.Length == 0,
            $"Equipment diagnostics catalog contains duplicate entries: {string.Join(", ", duplicates)}.");
    }

    [Fact]
    public void KnowledgeEntriesDoNotContainDuplicateNormalizedManufacturerSeriesCategoryCodeCombinations()
    {
        var source = new EquipmentDiagnosticsJsonKnowledgeSource();

        var duplicates = source.GetEntries()
            .GroupBy(
                entry => string.Join(
                    "/",
                    NormalizeTestIdentifier(entry.Manufacturer),
                    NormalizeTestIdentifier(entry.SeriesName),
                    entry.Category,
                    NormalizeTestCode(entry.Code)),
                StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();

        Assert.True(
            duplicates.Length == 0,
            $"Equipment diagnostics catalog contains duplicate normalized entries: {string.Join(", ", duplicates)}.");
    }

    [Fact]
    public void KnowledgeEntriesUseDocumentedTagStyle()
    {
        var tagPattern = new Regex("^[a-z0-9]+(-[a-z0-9]+)*$", RegexOptions.CultureInvariant);
        var source = new EquipmentDiagnosticsJsonKnowledgeSource();

        var violations = source.GetEntries()
            .SelectMany(entry => (entry.Tags ?? [])
                .Where(tag => !tagPattern.IsMatch(tag))
                .Select(tag => $"{entry.Manufacturer}/{entry.SeriesName}/{entry.Code}:{tag}"))
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Equipment diagnostics catalog tags must use lowercase kebab-case: {string.Join(", ", violations)}.");
    }

    [Fact]
    public void RuntimeKnowledgeEntriesMeetEd09BCatalogQaMinimums()
    {
        var source = new EquipmentDiagnosticsJsonKnowledgeSource();

        foreach (var entry in source.GetEntries())
        {
            Assert.True(
                entry.SafetyNotes.Count >= 2,
                $"{entry.Manufacturer}/{entry.SeriesName}/{entry.Code} must include at least two safety notes.");
            Assert.NotEmpty(entry.RequiredMeasurements);
            Assert.True(
                entry.DiagnosticSteps.Count >= 2,
                $"{entry.Manufacturer}/{entry.SeriesName}/{entry.Code} must include at least two diagnostic steps.");
            Assert.NotEmpty(entry.Source.Limitations);
            Assert.NotNull(entry.Source.ApplicableSeries);
        }
    }

    [Fact]
    public void EveryGreeEntryHasProvenanceSource()
    {
        var greeEntries = new EquipmentDiagnosticsJsonKnowledgeSource()
            .GetEntries()
            .Where(entry => entry.Manufacturer == "Gree")
            .ToArray();

        Assert.NotEmpty(greeEntries);
        foreach (var entry in greeEntries)
        {
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
    public void Ed05GreeGmvSeedEntriesRemainUnverifiedLowConfidence()
    {
        var entries = new EquipmentDiagnosticsJsonKnowledgeSource().GetEntries();

        foreach (var code in Ed05GreeGmvSeedCodes.Concat(Ed09AGreeGmvSeedCodes))
        {
            var entry = Assert.Single(entries, candidate =>
                candidate.Manufacturer == "Gree" &&
                candidate.SeriesName == "GMV" &&
                candidate.Category == EquipmentCategory.VrfOutdoorUnit &&
                candidate.Code == code);

            Assert.Equal(DiagnosticConfidence.Low, entry.Confidence);
            Assert.Equal("SeededEngineeringKnowledge", entry.Source.SourceType);
            Assert.Equal("UnverifiedSeed", entry.Source.EvidenceLevel);
            Assert.Empty(entry.ManualReferences);
            Assert.Null(entry.Source.ManualTitle);
            Assert.Null(entry.Source.Page);
            Assert.Null(entry.Source.Quote);
            Assert.NotEmpty(entry.Source.Limitations);
            Assert.Contains("GMV", entry.Source.ApplicableSeries);
        }
    }

    [Fact]
    public void Ed09ASeedEntriesRemainUnverifiedLowConfidence()
    {
        var entries = new EquipmentDiagnosticsJsonKnowledgeSource().GetEntries();

        foreach (var entry in GetEd09ASeedEntries(entries))
        {
            Assert.Equal(DiagnosticConfidence.Low, entry.Confidence);
            Assert.Equal("SeededEngineeringKnowledge", entry.Source.SourceType);
            Assert.Equal("UnverifiedSeed", entry.Source.EvidenceLevel);
            Assert.Empty(entry.ManualReferences);
            Assert.Null(entry.Source.ManualTitle);
            Assert.Null(entry.Source.ManualVersion);
            Assert.Null(entry.Source.ManualDocumentCode);
            Assert.Null(entry.Source.Page);
            Assert.Null(entry.Source.Section);
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
    public void ManualVerifiedConfidenceRequiresVerifiedOrCrossCheckedEvidence()
    {
        var source = new EquipmentDiagnosticsJsonKnowledgeSource();

        var violations = source.GetEntries()
            .Where(entry =>
                entry.Confidence == DiagnosticConfidence.ManualVerified &&
                entry.Source.EvidenceLevel is not ("ManualPageVerified" or "CrossChecked"))
            .Select(entry => $"{entry.Manufacturer}/{entry.SeriesName}/{entry.Code}/{entry.Source.EvidenceLevel}")
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"ManualVerified entries require ManualPageVerified or CrossChecked evidence: {string.Join(", ", violations)}.");
    }

    [Fact]
    public async Task ServiceCanFindNewlyAddedGreeGmvSeedCode()
    {
        var service = CreateServiceProvider().GetRequiredService<IEquipmentDiagnosticsService>();

        var results = await service.SearchErrorCodesAsync(
            new SearchEquipmentErrorCodesQuery(
                Manufacturer: "Gree",
                ErrorCode: "E1",
                Series: "GMV"),
            CancellationToken.None);

        var result = Assert.Single(results);
        Assert.Equal("Gree", result.Manufacturer);
        Assert.Equal("GMV", result.SeriesName);
        Assert.Equal("E1", result.Code);
        Assert.Equal(DiagnosticConfidence.Low, result.Confidence);
    }

    [Fact]
    public async Task ServiceCanFindEd09AChillerAndIndoorSeedCodes()
    {
        var service = CreateServiceProvider().GetRequiredService<IEquipmentDiagnosticsService>();

        var chillerResults = await service.SearchErrorCodesAsync(
            new SearchEquipmentErrorCodesQuery(
                Manufacturer: "Gree",
                ErrorCode: "E2",
                Series: "Chiller"),
            CancellationToken.None);
        var indoorResults = await service.SearchErrorCodesAsync(
            new SearchEquipmentErrorCodesQuery(
                Manufacturer: "Gree",
                ErrorCode: "H6",
                Series: "Indoor",
                Category: EquipmentCategory.VrfIndoorUnit),
            CancellationToken.None);

        var chiller = Assert.Single(chillerResults);
        Assert.Equal("Chiller", chiller.SeriesName);
        Assert.Equal("E2", chiller.Code);
        Assert.Equal(EquipmentCategory.Chiller, chiller.Category);
        Assert.Equal(DiagnosticConfidence.Low, chiller.Confidence);

        var indoor = Assert.Single(indoorResults);
        Assert.Equal("Indoor", indoor.SeriesName);
        Assert.Equal("H6", indoor.Code);
        Assert.Equal(EquipmentCategory.VrfIndoorUnit, indoor.Category);
        Assert.Equal(DiagnosticConfidence.Low, indoor.Confidence);
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
        Assert.Contains(resources, name => name.EndsWith("Knowledge.gree.gree-indoor.json", StringComparison.Ordinal));
        Assert.DoesNotContain(resources, name => name.Contains(".Knowledge.staging.", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void StagingSchemaAndTemplateFilesExist()
    {
        Assert.True(File.Exists(StagingReadmePath), $"Missing staging README: {StagingReadmePath}");
        Assert.True(File.Exists(StagingSchemaPath), $"Missing staging schema: {StagingSchemaPath}");
        Assert.True(File.Exists(GreeManualEntryTemplatePath), $"Missing staging template: {GreeManualEntryTemplatePath}");
        Assert.True(File.Exists(GreeReadyForReviewSamplePath), $"Missing staging sample: {GreeReadyForReviewSamplePath}");
        Assert.True(File.Exists(GreeInvalidInsufficientEvidenceSamplePath), $"Missing staging sample: {GreeInvalidInsufficientEvidenceSamplePath}");
    }

    [Fact]
    public void StagingTemplateIsValidJsonAndDoesNotClaimManualVerified()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(GreeManualEntryTemplatePath));

        var candidate = Assert.Single(document.RootElement.GetProperty("candidates").EnumerateArray());

        Assert.Equal("Draft", candidate.GetProperty("reviewStatus").GetString());
        Assert.Equal("Low", candidate.GetProperty("proposedConfidence").GetString());
        Assert.NotEqual("ManualVerified", candidate.GetProperty("proposedConfidence").GetString());

        var source = candidate.GetProperty("source");
        Assert.Equal("SeededEngineeringKnowledge", source.GetProperty("sourceType").GetString());
        Assert.Equal("UnverifiedSeed", source.GetProperty("evidenceLevel").GetString());
        Assert.Equal(JsonValueKind.Null, source.GetProperty("manualTitle").ValueKind);
        Assert.Equal(JsonValueKind.Null, source.GetProperty("page").ValueKind);
        Assert.Equal(JsonValueKind.Null, source.GetProperty("quote").ValueKind);
        Assert.NotEmpty(source.GetProperty("limitations").EnumerateArray());
    }

    [Fact]
    public async Task RuntimeCatalogIndexCountIgnoresStagingTemplate()
    {
        var loader = new EquipmentDiagnosticsKnowledgeJsonLoader();
        var expectedRuntimeEntries = GetKnowledgeJsonFiles()
            .SelectMany(file => loader.LoadFromJson(
                File.ReadAllText(file),
                Path.GetRelativePath(global::AssistantEngineer.Tests.TestPaths.RepoRoot, file)))
            .Count();
        var service = CreateServiceProvider().GetRequiredService<IEquipmentDiagnosticsService>();

        var index = await service.GetCatalogIndexAsync(CancellationToken.None);

        Assert.Equal(expectedRuntimeEntries, index.TotalEntries);

        using var template = JsonDocument.Parse(File.ReadAllText(GreeManualEntryTemplatePath));
        var templateCode = template.RootElement
            .GetProperty("candidates")
            .EnumerateArray()
            .Single()
            .GetProperty("code")
            .GetString();

        Assert.DoesNotContain(index.Codes, code => code.Code == templateCode);
    }

    [Fact]
    public void RuntimeKnowledgeFileEnumerationExcludesStagingFiles()
    {
        var runtimeKnowledgeFiles = GetKnowledgeJsonFiles();

        Assert.DoesNotContain(runtimeKnowledgeFiles, IsStagingKnowledgePath);
        Assert.Contains(runtimeKnowledgeFiles, path => path.EndsWith("gree-gmv.json", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(runtimeKnowledgeFiles, path => path.EndsWith("gree-chiller.json", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void StagingSchemaIncludesReviewStatusAndProvenanceRequirements()
    {
        var schema = File.ReadAllText(StagingSchemaPath);

        foreach (var requiredFragment in new[]
                 {
                     "\"reviewStatus\"",
                     "\"Draft\"",
                     "\"NeedsManualCheck\"",
                     "\"ReadyForReview\"",
                     "\"ApprovedForCatalog\"",
                     "\"Rejected\"",
                     "\"sourceType\"",
                     "\"evidenceLevel\"",
                     "\"manualTitle\"",
                     "\"manualVersion\"",
                     "\"manualDocumentCode\"",
                     "\"page\"",
                     "\"section\"",
                     "\"quote\"",
                     "\"limitations\"",
                     "\"applicableModels\"",
                     "\"applicableSeries\""
                 })
        {
            Assert.Contains(requiredFragment, schema, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void StagingSchemaDocumentsManualVerifiedAndApprovedForCatalogRules()
    {
        var schema = File.ReadAllText(StagingSchemaPath);

        Assert.Contains("\"proposedConfidence\"", schema, StringComparison.Ordinal);
        Assert.Contains("\"ManualVerified\"", schema, StringComparison.Ordinal);
        Assert.Contains("\"ManualPageVerified\"", schema, StringComparison.Ordinal);
        Assert.Contains("\"CrossChecked\"", schema, StringComparison.Ordinal);
        Assert.Contains("\"ApprovedForCatalog\"", schema, StringComparison.Ordinal);
        Assert.Contains("\"ManualReferenced\"", schema, StringComparison.Ordinal);
        Assert.Contains("\"manualTitle\"", schema, StringComparison.Ordinal);
        Assert.Contains("\"page\"", schema, StringComparison.Ordinal);
    }

    [Fact]
    public void StagingDocsAndTemplateDoNotContainUnsafeDiagnosticWording()
    {
        var checkedFiles = new[] { StagingReadmePath }
            .Concat(Directory.GetFiles(StagingRootPath, "*.json", SearchOption.AllDirectories)
                .Where(path => !path.EndsWith(".schema.json", StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        var forbiddenFragments = new[]
        {
            "bypass",
            "disable protection",
            "disable protections",
            "force run",
            "short protection",
            "ignore protection"
        };

        var violations = checkedFiles
            .SelectMany(file =>
            {
                var text = File.ReadAllText(file);
                return forbiddenFragments
                    .Where(fragment => text.Contains(fragment, StringComparison.OrdinalIgnoreCase))
                    .Select(fragment => $"{Path.GetFileName(file)}:{fragment}");
            })
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Staging docs/templates contain unsafe diagnostic wording fragments: {string.Join(", ", violations)}.");
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
            .Where(path => !IsStagingKnowledgePath(path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsStagingKnowledgePath(string path) =>
        path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Contains("staging", StringComparer.OrdinalIgnoreCase);

    private static string NormalizeTestIdentifier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return new string(value
            .Where(character => !char.IsWhiteSpace(character) && character != '-')
            .Select(char.ToUpperInvariant)
            .ToArray());
    }

    private static string NormalizeTestCode(string value) =>
        new(value
            .Where(character => !char.IsWhiteSpace(character) && character != '-')
            .Select(char.ToUpperInvariant)
            .ToArray());

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
                ((entry.SeriesName == "GMV" && (entry.Code is "H5" or "C7" || Ed05GreeGmvSeedCodes.Contains(entry.Code))) ||
                 (entry.SeriesName == "Chiller" && entry.Code == "E6")))
            .OrderBy(entry => entry.Code, StringComparer.Ordinal)
            .ToArray();

    private static readonly string[] Ed05GreeGmvSeedCodes =
    [
        "E1",
        "E3",
        "E4",
        "E5"
    ];

    private static readonly string[] Ed09AGreeGmvSeedCodes =
    [
        "F0",
        "F1",
        "F2",
        "F3",
        "L1",
        "L2",
        "P0",
        "P1"
    ];

    private static readonly string[] Ed09AGreeChillerSeedCodes =
    [
        "E1",
        "E2",
        "E3",
        "E4"
    ];

    private static readonly string[] Ed09AGreeIndoorSeedCodes =
    [
        "C5",
        "E1",
        "F0",
        "H6"
    ];

    private static IReadOnlyList<EquipmentDiagnosticsKnowledgeEntry> GetEd09ASeedEntries(
        IReadOnlyCollection<EquipmentDiagnosticsKnowledgeEntry> entries) =>
        entries
            .Where(entry =>
                entry.Manufacturer == "Gree" &&
                ((entry.SeriesName == "GMV" && Ed09AGreeGmvSeedCodes.Contains(entry.Code)) ||
                 (entry.SeriesName == "Chiller" && Ed09AGreeChillerSeedCodes.Contains(entry.Code)) ||
                 (entry.SeriesName == "Indoor" && Ed09AGreeIndoorSeedCodes.Contains(entry.Code))))
            .OrderBy(entry => entry.SeriesName, StringComparer.Ordinal)
            .ThenBy(entry => entry.Code, StringComparer.Ordinal)
            .ToArray();

    private static string EquipmentDiagnosticsModuleRoot =>
        Path.Combine(
            global::AssistantEngineer.Tests.TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.EquipmentDiagnostics");

    private static string StagingRootPath =>
        Path.Combine(
            EquipmentDiagnosticsModuleRoot,
            "Knowledge",
            "staging");

    private static string StagingReadmePath =>
        Path.Combine(
            StagingRootPath,
            "README.md");

    private static string StagingSchemaPath =>
        Path.Combine(
            StagingRootPath,
            "equipment-diagnostics-staging.schema.json");

    private static string GreeManualEntryTemplatePath =>
        Path.Combine(
            StagingRootPath,
            "templates",
            "gree-manual-entry.template.json");

    private static string GreeReadyForReviewSamplePath =>
        Path.Combine(
            StagingRootPath,
            "examples",
            "gree-gmv-ready-for-review.sample.json");

    private static string GreeInvalidInsufficientEvidenceSamplePath =>
        Path.Combine(
            StagingRootPath,
            "examples",
            "gree-gmv-invalid-insufficient-evidence.sample.json");

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
