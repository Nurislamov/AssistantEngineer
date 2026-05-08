using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Validation.ExternalReferenceValidation.FormulaAudit;

public class EngineeringCoreV1ProjectDocumentationTests
{
    [Fact]
    public void ApiExamplesDocumentExists()
    {
        Assert.True(
            File.Exists(ApiExamplesPath),
            $"Engineering Core V1 API examples must exist: {ApiExamplesPath}");
    }

    [Fact]
    public void DeveloperGuideDocumentExists()
    {
        Assert.True(
            File.Exists(DeveloperGuidePath),
            $"Engineering Core V1 developer guide must exist: {DeveloperGuidePath}");
    }

    [Fact]
    public void ReleaseSummaryDocumentExists()
    {
        Assert.True(
            File.Exists(ReleaseSummaryPath),
            $"Engineering Core V1 release summary must exist: {ReleaseSummaryPath}");
    }

    [Fact]
    public void ApiExamplesDocumentStatusEndpointAndReportDisclosure()
    {
        var content = ReadApiExamples();

        Assert.Contains(
            "GET /api/v1/calculations/engineering-core/v1/status",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "calculationDisclosure",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "EnergyDataSource = TrueHourlySimulation",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "IsTrueHourly8760 = true",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "HourlyRecordCount = 8760",
            content,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ApiExamplesDocumentNonClaimsAndDiagnosticsRule()
    {
        var content = ReadApiExamples();

        Assert.Contains(
            "exact EnergyPlus comparison workflow",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "exact StandardReference equivalence",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "ASHRAE 140 / BESTEST-style validation anchor coverage",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "A successful calculation result must not contain",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "CalculationDiagnosticSeverity.Error",
            content,
            StringComparison.Ordinal);
    }

    [Fact]
    public void DeveloperGuideDocumentsFormulaAuditMatrixAsSourceOfTruth()
    {
        var content = ReadDeveloperGuide();

        Assert.Contains(
            "FormulaAuditMatrix.cs",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "main source of truth",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "ClosedV1",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "OutOfScopeV1",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "PlannedValidation",
            content,
            StringComparison.Ordinal);
    }

    [Fact]
    public void DeveloperGuideDocumentsForbiddenParityClaims()
    {
        var content = ReadDeveloperGuide();

        var forbiddenClaims = new[]
        {
            "full ISO 52016 implementation",
            "full ISO 13370 implementation",
            "EnergyPlus comparison workflow",
            "ASHRAE 140 covered",
            "StandardReference equivalence",
            "ExternalReferenceCovered"
        };

        foreach (var forbiddenClaim in forbiddenClaims)
        {
            Assert.Contains(
                forbiddenClaim,
                content,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void DeveloperGuideDocumentsReportAndApiDisclosureRequirements()
    {
        var content = ReadDeveloperGuide();

        Assert.Contains(
            "GET /api/v1/calculations/engineering-core/v1/status",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "CalculationDisclosure",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "warnings",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "assumptions",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "explicit non-claims",
            content,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ReleaseSummaryStatesEngineeringCoreV1IsClosedWithLimitations()
    {
        var content = ReadReleaseSummary();

        Assert.Contains(
            "Engineering Core V1 is closed as an engineering formula gate",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "Closed formula gate does not mean exact equivalence",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "GET /api/v1/calculations/engineering-core/v1/status",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "calculationDisclosure",
            content,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ReleaseSummaryListsFutureValidationAndNextWork()
    {
        var content = ReadReleaseSummary();

        Assert.Contains(
            "EnergyPlus / ASHRAE 140 / BESTEST-style validation anchor is planned",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "comparative engineering validation",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "frontend diagnostics panel",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "latent/moisture psychrometrics future module",
            content,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string ApiExamplesPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "calculations",
            "EngineeringCoreV1ApiExamples.md");

    private static string DeveloperGuidePath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "calculations",
            "EngineeringCoreV1DeveloperGuide.md");

    private static string ReleaseSummaryPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "releases",
            "EngineeringCoreV1.md");

    private static string ReadApiExamples() =>
        File.ReadAllText(ApiExamplesPath);

    private static string ReadDeveloperGuide() =>
        File.ReadAllText(DeveloperGuidePath);

    private static string ReadReleaseSummary() =>
        File.ReadAllText(ReleaseSummaryPath);
}
