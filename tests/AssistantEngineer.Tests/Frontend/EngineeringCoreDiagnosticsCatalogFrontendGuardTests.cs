using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Frontend;

public class EngineeringCoreDiagnosticsCatalogFrontendGuardTests
{
    [Fact]
    public void FrontendTypesExposeDiagnosticsCatalogDto()
    {
        var content = ReadRepoFile(
            "src",
            "Frontend",
            "src",
            "entities",
            "calculation",
            "types.ts");

        Assert.Contains("EngineeringCoreV1DiagnosticsCatalogApiResponse", content, StringComparison.Ordinal);
        Assert.Contains("EngineeringCoreV1DiagnosticCatalogItemApiResponse", content, StringComparison.Ordinal);
        Assert.Contains("EngineeringCoreV1DiagnosticsRulesApiResponse", content, StringComparison.Ordinal);
        Assert.Contains("userMessage", content, StringComparison.Ordinal);
        Assert.Contains("userAction", content, StringComparison.Ordinal);
        Assert.Contains("closedV1Gate", content, StringComparison.Ordinal);
    }

    [Fact]
    public void FrontendApiRoutesAndQueryKeysExposeDiagnosticsCatalog()
    {
        var routes = ReadRepoFile(
            "src",
            "Frontend",
            "src",
            "shared",
            "api",
            "apiRoutes.ts");

        var queryKeys = ReadRepoFile(
            "src",
            "Frontend",
            "src",
            "shared",
            "api",
            "queryKeys.ts");

        Assert.Contains("engineeringCoreV1DiagnosticsCatalog", routes, StringComparison.Ordinal);
        Assert.Contains("/calculations/engineering-core/v1/diagnostics-catalog", routes, StringComparison.Ordinal);

        Assert.Contains("engineeringCoreV1DiagnosticsCatalog", queryKeys, StringComparison.Ordinal);
        Assert.Contains("diagnostics-catalog", queryKeys, StringComparison.Ordinal);
    }

    [Fact]
    public void FrontendCalculationsApiAndHookExposeDiagnosticsCatalog()
    {
        var api = ReadRepoFile(
            "src",
            "Frontend",
            "src",
            "entities",
            "calculation",
            "api",
            "calculationsApi.ts");

        var hook = ReadRepoFile(
            "src",
            "Frontend",
            "src",
            "entities",
            "calculation",
            "model",
            "useEngineeringCoreDiagnosticsCatalog.ts");

        Assert.Contains("getEngineeringCoreV1DiagnosticsCatalog", api, StringComparison.Ordinal);
        Assert.Contains("EngineeringCoreV1DiagnosticsCatalogApiResponse", api, StringComparison.Ordinal);
        Assert.Contains("apiRoutes.calculations.engineeringCoreV1DiagnosticsCatalog()", api, StringComparison.Ordinal);

        Assert.Contains("useEngineeringCoreDiagnosticsCatalog", hook, StringComparison.Ordinal);
        Assert.Contains("queryKeys.calculations.engineeringCoreV1DiagnosticsCatalog", hook, StringComparison.Ordinal);
        Assert.Contains("calculationsApi.getEngineeringCoreV1DiagnosticsCatalog", hook, StringComparison.Ordinal);
    }

    [Fact]
    public void DiagnosticsCatalogApiDocumentationExistsAndDocumentsEndpointRulesAndNonClaims()
    {
        var content = ReadRepoFile(
            "docs",
            "calculations",
            "EngineeringCoreV1DiagnosticsCatalogApi.md");

        Assert.Contains("GET /api/v1/calculations/engineering-core/v1/diagnostics-catalog", content, StringComparison.Ordinal);
        Assert.Contains("severity rules", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("AnnualEnergy.Not8760", content, StringComparison.Ordinal);
        Assert.Contains("MonthlyBalanceAdapter", content, StringComparison.Ordinal);
        Assert.Contains("exact EnergyPlus numerical parity", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("ASHRAE 140 validation coverage", content, StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadRepoFile(params string[] parts)
    {
        var path = Path.Combine(parts.Prepend(TestPaths.RepoRoot).ToArray());

        Assert.True(
            File.Exists(path),
            $"Expected file does not exist: {path}");

        return File.ReadAllText(path);
    }
}
