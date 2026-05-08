using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Validation.ExternalReferenceValidation.FormulaAudit;

public class EngineeringCoreV1ContributionGuardTests
{
    [Fact]
    public void PullRequestTemplateExists()
    {
        Assert.True(
            File.Exists(PullRequestTemplatePath),
            $"Pull request template must exist: {PullRequestTemplatePath}");
    }

    [Fact]
    public void EngineeringCoreFormulaIssueTemplateExists()
    {
        Assert.True(
            File.Exists(FormulaIssueTemplatePath),
            $"Engineering Core formula issue template must exist: {FormulaIssueTemplatePath}");
    }

    [Fact]
    public void EnergyPlusValidationIssueTemplateExists()
    {
        Assert.True(
            File.Exists(ValidationIssueTemplatePath),
            $"EnergyPlus validation issue template must exist: {ValidationIssueTemplatePath}");
    }

    [Fact]
    public void ContributionGuideExists()
    {
        Assert.True(
            File.Exists(ContributionGuidePath),
            $"Engineering Core contribution guide must exist: {ContributionGuidePath}");
    }

    [Fact]
    public void PullRequestTemplateRequiresEngineeringCoreChecklistAndVerificationScript()
    {
        var content = ReadPullRequestTemplate();

        Assert.Contains("Engineering Core V1 impact", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FormulaAuditMatrix", content, StringComparison.Ordinal);
        Assert.Contains("CalculationDiagnosticSeverity.Error", content, StringComparison.Ordinal);
        Assert.Contains("EnergyDataSource = TrueHourlySimulation", content, StringComparison.Ordinal);
        Assert.Contains("IsTrueHourly8760 = true", content, StringComparison.Ordinal);
        Assert.Contains("HourlyRecordCount = 8760", content, StringComparison.Ordinal);
        Assert.Contains(".\\scripts\\engineering-core\\verify-engineering-core-v1.ps1", content, StringComparison.Ordinal);
    }

    [Fact]
    public void PullRequestTemplateKeepsNonClaimsVisible()
    {
        var content = ReadPullRequestTemplate();

        var requiredNonClaims = new[]
        {
            "No exact EnergyPlus comparison workflow claim is introduced",
            "No exact StandardReference equivalence claim is introduced",
            "No ASHRAE 140 / BESTEST-style validation anchor coverage claim is introduced",
            "No full ISO/EN implementation claim is introduced"
        };

        foreach (var requiredNonClaim in requiredNonClaims)
        {
            Assert.Contains(
                requiredNonClaim,
                content,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void FormulaIssueTemplateRequiresFormulaUnitsDiagnosticsTestsAndNonClaims()
    {
        var content = ReadFormulaIssueTemplate();

        var requiredPhrases = new[]
        {
            "CalculationId",
            "Formula / algorithm",
            "Units",
            "Source principle",
            "Diagnostics requirements",
            "Invalid mandatory input fails with Error",
            "Successful result does not contain Error diagnostics",
            "Does not claim exact EnergyPlus numerical equivalence",
            "Required tests",
            "User-visible disclosure impact"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(
                requiredPhrase,
                content,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void ValidationIssueTemplateRequiresReferenceAssumptionsMetricsTolerancesAndNonClaims()
    {
        var content = ReadValidationIssueTemplate();

        var requiredPhrases = new[]
        {
            "EnergyPlus / ASHRAE 140 / BESTEST-style validation anchor case",
            "Validation is comparative with documented tolerances",
            "Reference model",
            "Assumptions",
            "Metrics and tolerances",
            "Does not claim exact EnergyPlus numerical equivalence",
            "Does not claim ASHRAE 140 / BESTEST-style validation anchor coverage",
            "Fixture plan"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(
                requiredPhrase,
                content,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void ContributionGuideDocumentsFormulaDiagnosticsDisclosureValidationAndCiRules()
    {
        var content = ReadContributionGuide();

        var requiredPhrases = new[]
        {
            "Engineering Core V1 is closed as an engineering formula gate",
            "FormulaAuditMatrix",
            "A successful calculation result must not contain CalculationDiagnosticSeverity.Error",
            "EnergyDataSource = TrueHourlySimulation",
            "IsTrueHourly8760 = true",
            "HourlyRecordCount = 8760",
            "warnings, assumptions, explicit non-claims and out-of-scope items remain visible",
            "Validation cases must be comparative with documented tolerances",
            ".github/workflows/engineering-core-v1.yml",
            ".\\scripts\\engineering-core\\verify-engineering-core-v1.ps1"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(
                requiredPhrase,
                content,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string PullRequestTemplatePath =>
        Path.Combine(
            TestPaths.RepoRoot,
            ".github",
            "pull_request_template.md");

    private static string FormulaIssueTemplatePath =>
        Path.Combine(
            TestPaths.RepoRoot,
            ".github",
            "ISSUE_TEMPLATE",
            "engineering-core-formula.yml");

    private static string ValidationIssueTemplatePath =>
        Path.Combine(
            TestPaths.RepoRoot,
            ".github",
            "ISSUE_TEMPLATE",
            "energyplus-validation-case.yml");

    private static string ContributionGuidePath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "contributing",
            "EngineeringCoreV1ContributionGuide.md");

    private static string ReadPullRequestTemplate() =>
        File.ReadAllText(PullRequestTemplatePath);

    private static string ReadFormulaIssueTemplate() =>
        File.ReadAllText(FormulaIssueTemplatePath);

    private static string ReadValidationIssueTemplate() =>
        File.ReadAllText(ValidationIssueTemplatePath);

    private static string ReadContributionGuide() =>
        File.ReadAllText(ContributionGuidePath);
}
