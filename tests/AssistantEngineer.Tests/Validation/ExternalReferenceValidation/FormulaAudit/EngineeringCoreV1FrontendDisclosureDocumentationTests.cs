using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Validation.ExternalReferenceValidation.FormulaAudit;

public class EngineeringCoreV1FrontendDisclosureDocumentationTests
{
    [Fact]
    public void FrontendReportDisclosureDocumentExists()
    {
        Assert.True(
            File.Exists(FrontendDisclosureDocumentPath),
            $"Frontend report disclosure document must exist: {FrontendDisclosureDocumentPath}");
    }

    [Fact]
    public void FrontendReportDisclosureDocumentReferencesComponentAndIntegrationPoint()
    {
        var content = ReadFrontendDisclosureDocument();

        Assert.Contains(
            "EngineeringCoreDisclosurePanel.tsx",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "BuildingWorkspace.tsx",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "calculationDisclosure",
            content,
            StringComparison.Ordinal);
    }

    [Fact]
    public void FrontendReportDisclosureDocumentRequiresVisibleWarningsAssumptionsAndNonClaims()
    {
        var content = ReadFrontendDisclosureDocument();

        Assert.Contains(
            "Warnings, assumptions and non-claims must be visible",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "must not be hidden only inside raw JSON",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "explicitNonClaims",
            content,
            StringComparison.Ordinal);
    }

    [Fact]
    public void FrontendReportDisclosureDocumentListsRequiredNonClaimsAndOutOfScopeItems()
    {
        var content = ReadFrontendDisclosureDocument();

        Assert.Contains(
            "no exact EnergyPlus numerical equivalence claim",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "no exact StandardReference numerical equivalence claim",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "no ASHRAE 140 / BESTEST-style validation anchor coverage claim",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "HVAC.LATENT_LOAD",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "HVAC.MOISTURE_BALANCE",
            content,
            StringComparison.Ordinal);
    }

    private static string FrontendDisclosureDocumentPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "frontend",
            "EngineeringCoreV1ReportDisclosurePanel.md");

    private static string ReadFrontendDisclosureDocument() =>
        File.ReadAllText(FrontendDisclosureDocumentPath);
}
