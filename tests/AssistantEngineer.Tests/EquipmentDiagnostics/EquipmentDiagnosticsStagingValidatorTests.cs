using AssistantEngineer.Modules.EquipmentDiagnostics.Application;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Json;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Knowledge.Staging;
using AssistantEngineer.Modules.EquipmentDiagnostics.Application.Services;
using AssistantEngineer.Modules.EquipmentDiagnostics.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace AssistantEngineer.Tests.EquipmentDiagnostics;

public class EquipmentDiagnosticsStagingValidatorTests
{
    [Fact]
    public void StagingTemplateValidatesWithOnlyDraftInformationalIssue()
    {
        var validator = new EquipmentDiagnosticsStagingValidator();

        var result = validator.ValidateJson(
            File.ReadAllText(GreeManualEntryTemplatePath),
            GetProductionEntries(),
            "gree-manual-entry.template.json");

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        var issue = Assert.Single(result.Issues);
        Assert.Equal("CandidateNotReadyForRuntimeCatalog", issue.Code);
        Assert.Equal(EquipmentDiagnosticsStagingValidationIssueSeverity.Info, issue.Severity);
        Assert.NotNull(result.Report);
        Assert.Equal(1, result.Report.TotalCandidates);
        Assert.Equal(0, result.Report.ErrorCount);
        Assert.Equal(1, result.Report.InfoCount);
        Assert.False(result.Report.HasBlockingIssues);
        Assert.False(result.Report.PromotionReady);
        Assert.Contains(result.Report.CandidateKeys, key => key.Contains("REPLACE_WITH_CODE", StringComparison.Ordinal));
    }

    [Fact]
    public void ReadyForReviewSampleValidatesDeterministicallyWithoutRuntimePromotionReadiness()
    {
        var validator = new EquipmentDiagnosticsStagingValidator();

        var result = validator.ValidateJson(
            File.ReadAllText(GreeReadyForReviewSamplePath),
            GetProductionEntries(),
            "gree-gmv-ready-for-review.sample.json");

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Issues);
        Assert.NotNull(result.Report);
        Assert.Equal(1, result.Report.TotalCandidates);
        Assert.Equal(0, result.Report.ErrorCount);
        Assert.Equal(0, result.Report.WarningCount);
        Assert.Equal(0, result.Report.InfoCount);
        Assert.False(result.Report.HasBlockingIssues);
        Assert.False(result.Report.PromotionReady);
        Assert.Contains("GREE/GMV/VrfOutdoorUnit//SAMPLERFR", result.Report.CandidateKeys);
    }

    [Fact]
    public void InvalidSampleFailsForExpectedPromotionEvidenceReasons()
    {
        var validator = new EquipmentDiagnosticsStagingValidator();

        var result = validator.ValidateJson(
            File.ReadAllText(GreeInvalidInsufficientEvidenceSamplePath),
            GetProductionEntries(),
            "gree-gmv-invalid-insufficient-evidence.sample.json");

        Assert.False(result.IsValid);
        Assert.NotNull(result.Report);
        Assert.True(result.Report.HasBlockingIssues);
        Assert.False(result.Report.PromotionReady);
        Assert.Contains(result.Errors, issue => issue.Code == "ManualVerifiedRequiresVerifiedEvidence");
        Assert.Contains(result.Errors, issue => issue.Code == "ApprovedForCatalogRequiresVerifiedEvidence");
        Assert.Contains(result.Errors, issue => issue.Code == "ApprovedForCatalogRequiresExternalSource");
    }

    [Fact]
    public void ValidationReportGroupsIssuesByCandidateKey()
    {
        var validator = new EquipmentDiagnosticsStagingValidator();

        var result = validator.ValidateJson(
            File.ReadAllText(GreeInvalidInsufficientEvidenceSamplePath),
            GetProductionEntries(),
            "gree-gmv-invalid-insufficient-evidence.sample.json");

        Assert.NotNull(result.Report);
        var group = Assert.Single(result.Report.IssuesByCandidateKey);
        Assert.Equal("GREE/GMV/VrfOutdoorUnit//SAMPLEINVALID", group.CandidateKey);
        Assert.Contains(group.Issues, issue => issue.Code == "ManualVerifiedRequiresVerifiedEvidence");
        Assert.Contains(group.Issues, issue => issue.Code == "ApprovedForCatalogRequiresVerifiedEvidence");
    }

    [Fact]
    public void ManualVerifiedWithUnverifiedSeedFailsValidation()
    {
        var candidate = CreateCandidate(proposedConfidence: "ManualVerified");
        var validator = new EquipmentDiagnosticsStagingValidator();

        var result = validator.ValidateCandidates([candidate], GetProductionEntries());

        Assert.Contains(result.Errors, issue => issue.Code == "ManualVerifiedRequiresVerifiedEvidence");
    }

    [Fact]
    public void ManualPageVerifiedWithoutManualTitleAndPageFailsValidation()
    {
        var candidate = CreateCandidate(
            evidenceLevel: "ManualPageVerified",
            sourceType: "ServiceManual");
        var validator = new EquipmentDiagnosticsStagingValidator();

        var result = validator.ValidateCandidates([candidate], GetProductionEntries());

        Assert.Contains(result.Errors, issue => issue.Code == "ManualPageVerifiedRequiresManualTitle");
        Assert.Contains(result.Errors, issue => issue.Code == "ManualPageVerifiedRequiresPage");
    }

    [Fact]
    public void ApprovedForCatalogWithUnverifiedSeedFailsValidation()
    {
        var candidate = CreateCandidate(reviewStatus: "ApprovedForCatalog");
        var validator = new EquipmentDiagnosticsStagingValidator();

        var result = validator.ValidateCandidates([candidate], GetProductionEntries());

        Assert.Contains(result.Errors, issue => issue.Code == "ApprovedForCatalogRequiresVerifiedEvidence");
        Assert.Contains(result.Errors, issue => issue.Code == "ApprovedForCatalogRequiresExternalSource");
    }

    [Fact]
    public void UnsafeDiagnosticWordingFailsValidation()
    {
        var candidate = CreateCandidate(
            code: "STAGE-UNSAFE",
            likelyCauses: ["Do not bypass manufacturer safeguards."]);
        var validator = new EquipmentDiagnosticsStagingValidator();

        var result = validator.ValidateCandidates([candidate], GetProductionEntries());

        Assert.Contains(result.Errors, issue => issue.Code == "UnsafeDiagnosticWording");
    }

    [Fact]
    public void DuplicateCandidateKeysFailValidation()
    {
        var validator = new EquipmentDiagnosticsStagingValidator();

        var result = validator.ValidateCandidates(
            [
                CreateCandidate(code: "X-1"),
                CreateCandidate(code: "x 1")
            ],
            GetProductionEntries());

        Assert.Contains(result.Errors, issue => issue.Code == "DuplicateCandidateKey");
    }

    [Fact]
    public void CandidateConflictingWithProductionCatalogKeyFailsValidation()
    {
        var validator = new EquipmentDiagnosticsStagingValidator();

        var result = validator.ValidateCandidates(
            [
                CreateCandidate(
                    manufacturer: "Gree",
                    series: "GMV",
                    category: "VrfOutdoorUnit",
                    modelCode: null,
                    code: "H5")
            ],
            GetProductionEntries());

        Assert.Contains(result.Errors, issue => issue.Code == "CandidateConflictsWithProductionCatalog");
    }

    [Fact]
    public async Task RuntimeCatalogEntryCountRemainsIndependentOfStagingFiles()
    {
        var loader = new EquipmentDiagnosticsKnowledgeJsonLoader();
        var productionFileEntries = GetProductionKnowledgeJsonFiles()
            .SelectMany(file => loader.LoadFromJson(
                File.ReadAllText(file),
                Path.GetRelativePath(TestPaths.RepoRoot, file)))
            .Count();
        var service = CreateServiceProvider().GetRequiredService<IEquipmentDiagnosticsService>();

        var index = await service.GetCatalogIndexAsync(CancellationToken.None);

        Assert.Equal(productionFileEntries, index.TotalEntries);
    }

    [Fact]
    public void RuntimeKnowledgeSourceStillExcludesStagingResources()
    {
        var resources = EquipmentDiagnosticsJsonKnowledgeSource.GetEmbeddedKnowledgeResourceNames();

        Assert.DoesNotContain(resources, resource =>
            resource.Contains(".Knowledge.staging.", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(resources, resource =>
            resource.EndsWith(".Knowledge.gree.gree-gmv.json", StringComparison.Ordinal));
        Assert.Contains(resources, resource =>
            resource.EndsWith(".Knowledge.gree.gree-chiller.json", StringComparison.Ordinal));
        Assert.DoesNotContain(resources, resource =>
            resource.Contains("ready-for-review", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(resources, resource =>
            resource.Contains("invalid-insufficient-evidence", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ValidatorAcceptsJsonTextInput()
    {
        var validator = new EquipmentDiagnosticsStagingValidator();

        var result = validator.ValidateJson(
            CreateStagingJson("JSON-1"),
            GetProductionEntries(),
            "candidate.json");

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    private static EquipmentDiagnosticsStagingCandidate CreateCandidate(
        string manufacturer = "Staged Manufacturer",
        string series = "Staged Series",
        string category = "SplitSystem",
        string? modelCode = null,
        string code = "STAGE-1",
        string proposedConfidence = "Low",
        string sourceType = "SeededEngineeringKnowledge",
        string evidenceLevel = "UnverifiedSeed",
        string? manualTitle = null,
        string? page = null,
        string? notes = "Staging candidate notes.",
        IReadOnlyList<string>? likelyCauses = null,
        string reviewStatus = "ReadyForReview") =>
        new()
        {
            Manufacturer = manufacturer,
            Series = series,
            Category = category,
            ModelCode = modelCode,
            Code = code,
            Title = "Staging diagnostic candidate",
            Meaning = "Staging diagnostic meaning pending review.",
            Severity = "Service attention required",
            ProposedConfidence = proposedConfidence,
            Source = new EquipmentDiagnosticsStagingSourceInfo
            {
                SourceType = sourceType,
                EvidenceLevel = evidenceLevel,
                ManualTitle = manualTitle,
                ManualVersion = null,
                ManualDocumentCode = null,
                Page = page,
                Section = null,
                Quote = null,
                Notes = notes,
                Limitations =
                [
                    "Use as staging guidance only.",
                    "Verify against exact source evidence before promotion."
                ],
                ApplicableModels = [],
                ApplicableSeries = [series]
            },
            LikelyCauses = likelyCauses?.ToList() ?? ["Staged likely cause pending evidence review."],
            DiagnosticSteps =
            [
                new EquipmentDiagnosticsStagingDiagnosticStep
                {
                    Order = 1,
                    Title = "Confirm staged equipment identity",
                    Instruction = "Record manufacturer, series, model, serial plate data, and displayed error code.",
                    ExpectedResult = "Equipment identity and displayed code are confirmed before review.",
                    IfFailedAction = "Stop classification and obtain exact equipment information."
                }
            ],
            RequiredMeasurements =
            [
                new EquipmentDiagnosticsStagingRequiredMeasurement
                {
                    Name = "Supply voltage",
                    Unit = "V",
                    Description = "Measured by a qualified technician when source procedure requires it.",
                    RequiredBeforeConclusion = true
                }
            ],
            SafetyNotes =
            [
                "Electrical, compressor, inverter, refrigerant, and protection checks must be performed by a qualified technician.",
                "Keep manufacturer safeguards active during diagnosis."
            ],
            Tags = ["staging"],
            PromotionNotes = ["Promote only after evidence review."],
            ReviewStatus = reviewStatus
        };

    private static string CreateStagingJson(string code) =>
        $$"""
        {
          "candidates": [
            {
              "manufacturer": "Staged Manufacturer",
              "series": "Staged Series",
              "category": "SplitSystem",
              "modelCode": null,
              "code": "{{code}}",
              "title": "Staging diagnostic candidate",
              "meaning": "Staging diagnostic meaning pending review.",
              "severity": "Service attention required",
              "proposedConfidence": "Low",
              "source": {
                "sourceType": "SeededEngineeringKnowledge",
                "evidenceLevel": "UnverifiedSeed",
                "manualTitle": null,
                "manualVersion": null,
                "manualDocumentCode": null,
                "page": null,
                "section": null,
                "quote": null,
                "notes": "Staging candidate notes.",
                "limitations": [
                  "Use as staging guidance only."
                ],
                "applicableModels": [],
                "applicableSeries": [
                  "Staged Series"
                ]
              },
              "likelyCauses": [
                "Staged likely cause pending evidence review."
              ],
              "diagnosticSteps": [
                {
                  "order": 1,
                  "title": "Confirm staged equipment identity",
                  "instruction": "Record manufacturer, series, model, serial plate data, and displayed error code.",
                  "expectedResult": "Equipment identity and displayed code are confirmed before review.",
                  "ifFailedAction": "Stop classification and obtain exact equipment information."
                }
              ],
              "requiredMeasurements": [
                {
                  "name": "Supply voltage",
                  "unit": "V",
                  "description": "Measured by a qualified technician when source procedure requires it.",
                  "requiredBeforeConclusion": true
                }
              ],
              "safetyNotes": [
                "Electrical checks must be performed by a qualified technician."
              ],
              "tags": [
                "staging"
              ],
              "promotionNotes": [
                "Promote only after evidence review."
              ],
              "reviewStatus": "ReadyForReview"
            }
          ]
        }
        """;

    private static IReadOnlyCollection<EquipmentDiagnosticsKnowledgeEntry> GetProductionEntries() =>
        new EquipmentDiagnosticsJsonKnowledgeSource().GetEntries();

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
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddEquipmentDiagnosticsModule();
        return services.BuildServiceProvider();
    }

    private static string EquipmentDiagnosticsModuleRoot =>
        Path.Combine(
            TestPaths.RepoRoot,
            "src",
            "Backend",
            "AssistantEngineer.Modules.EquipmentDiagnostics");

    private static string GreeManualEntryTemplatePath =>
        Path.Combine(
            EquipmentDiagnosticsModuleRoot,
            "Knowledge",
            "staging",
            "templates",
            "gree-manual-entry.template.json");

    private static string GreeReadyForReviewSamplePath =>
        Path.Combine(
            EquipmentDiagnosticsModuleRoot,
            "Knowledge",
            "staging",
            "examples",
            "gree-gmv-ready-for-review.sample.json");

    private static string GreeInvalidInsufficientEvidenceSamplePath =>
        Path.Combine(
            EquipmentDiagnosticsModuleRoot,
            "Knowledge",
            "staging",
            "examples",
            "gree-gmv-invalid-insufficient-evidence.sample.json");
}
