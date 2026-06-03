using System.Reflection;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Contracts;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge;
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
            "disabling protections"
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
                })))
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

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddEquipmentDiagnosticsModule();
        return services.BuildServiceProvider();
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
            Tags: ["test"]);

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
