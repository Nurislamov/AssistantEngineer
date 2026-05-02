using System.Text.Json;
using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Parity.EnergyCalculationParity.FormulaAudit;

public class EngineeringCoreV1OpenApiContractTests
{
    [Fact]
    public void OpenApiPostmanConsumerGuideAndChangelogExist()
    {
        var requiredFiles = new[]
        {
            OpenApiPath,
            PostmanPath,
            ConsumerGuidePath,
            ChangelogPath
        };

        foreach (var requiredFile in requiredFiles)
        {
            Assert.True(
                File.Exists(requiredFile),
                $"Required Engineering Core V1 API contract artifact is missing: {requiredFile}");
        }
    }

    [Fact]
    public void OpenApiFragmentDocumentsBothEndpointsAndOperations()
    {
        var content = File.ReadAllText(OpenApiPath);

        Assert.Contains("openapi: 3.0.3", content, StringComparison.Ordinal);
        Assert.Contains("/api/v1/calculations/engineering-core/v1/status", content, StringComparison.Ordinal);
        Assert.Contains("/api/v1/calculations/engineering-core/v1/diagnostics-catalog", content, StringComparison.Ordinal);
        Assert.Contains("operationId: getEngineeringCoreV1Status", content, StringComparison.Ordinal);
        Assert.Contains("operationId: getEngineeringCoreV1DiagnosticsCatalog", content, StringComparison.Ordinal);
        Assert.Contains("EngineeringCoreV1StatusResponse", content, StringComparison.Ordinal);
        Assert.Contains("EngineeringCoreV1DiagnosticsCatalogResponse", content, StringComparison.Ordinal);
    }

    [Fact]
    public void OpenApiFragmentDocumentsRequiredStatusFields()
    {
        var content = File.ReadAllText(OpenApiPath);

        var requiredFields = new[]
        {
            "coreName",
            "version",
            "status",
            "formulaGatesClosed",
            "weather8760GatesClosed",
            "annualHourly8760GateClosed",
            "successfulResultsMustNotContainErrorDiagnostics",
            "formulaGates",
            "explicitNonClaims",
            "outOfScopeV1",
            "plannedValidation",
            "requiredAnnual8760Flags",
            "documentationFiles"
        };

        foreach (var requiredField in requiredFields)
        {
            Assert.Contains(requiredField, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void OpenApiFragmentDocumentsRequiredDiagnosticsFieldsAndSeverityEnum()
    {
        var content = File.ReadAllText(OpenApiPath);

        var requiredFields = new[]
        {
            "EngineeringCoreV1DiagnosticCatalogItem",
            "code",
            "severity",
            "category",
            "userMessage",
            "userAction",
            "closedV1Gate",
            "- Error",
            "- Warning",
            "- Info",
            "CalculationDiagnosticSeverity.Error"
        };

        foreach (var requiredField in requiredFields)
        {
            Assert.Contains(requiredField, content, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void OpenApiFragmentKeepsAnnual8760FlagsAndNonClaimsVisible()
    {
        var content = File.ReadAllText(OpenApiPath);

        Assert.Contains("EnergyDataSource = TrueHourlySimulation", content, StringComparison.Ordinal);
        Assert.Contains("IsTrueHourly8760 = true", content, StringComparison.Ordinal);
        Assert.Contains("HourlyRecordCount = 8760", content, StringComparison.Ordinal);

        Assert.Contains("No exact EnergyPlus numerical parity claim.", content, StringComparison.Ordinal);
        Assert.Contains("No exact pyBuildingEnergy numerical parity claim.", content, StringComparison.Ordinal);
        Assert.Contains("No ASHRAE 140 validation coverage claim.", content, StringComparison.Ordinal);
        Assert.Contains("No full ISO 52016 node/matrix solver parity claim.", content, StringComparison.Ordinal);
    }

    [Fact]
    public void PostmanCollectionIsValidJsonAndDocumentsBothEndpoints()
    {
        using var postman = JsonDocument.Parse(File.ReadAllText(PostmanPath));
        var root = postman.RootElement;

        Assert.Equal(
            "AssistantEngineer Engineering Core V1",
            root.GetProperty("info").GetProperty("name").GetString());

        var rawUrls = root
            .GetProperty("item")
            .EnumerateArray()
            .Select(item => item.GetProperty("request").GetProperty("url").GetProperty("raw").GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains("{{baseUrl}}/api/v1/calculations/engineering-core/v1/status", rawUrls);
        Assert.Contains("{{baseUrl}}/api/v1/calculations/engineering-core/v1/diagnostics-catalog", rawUrls);
    }

    [Fact]
    public void ConsumerGuideDocumentsUsageCompatibilityAnnual8760AndNonClaims()
    {
        var content = File.ReadAllText(ConsumerGuidePath);

        Assert.Contains("GET /api/v1/calculations/engineering-core/v1/status", content, StringComparison.Ordinal);
        Assert.Contains("GET /api/v1/calculations/engineering-core/v1/diagnostics-catalog", content, StringComparison.Ordinal);
        Assert.Contains("Annual 8760 UI rule", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("EnergyDataSource = TrueHourlySimulation", content, StringComparison.Ordinal);
        Assert.Contains("Removing or renaming these fields is a breaking contract change", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no exact EnergyPlus numerical parity", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no ASHRAE 140 validation coverage", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ContractChangelogDocumentsInitialV1EndpointsAndBreakingChangeRules()
    {
        var content = File.ReadAllText(ChangelogPath);

        Assert.Contains("## v1", content, StringComparison.Ordinal);
        Assert.Contains("GET /api/v1/calculations/engineering-core/v1/status", content, StringComparison.Ordinal);
        Assert.Contains("GET /api/v1/calculations/engineering-core/v1/diagnostics-catalog", content, StringComparison.Ordinal);
        Assert.Contains("removing required fields is a breaking change", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("weakening annual 8760 requirements is a breaking change", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("removing explicit non-claims is a breaking change", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void OpenApiEndpointPathsMatchHttpSnapshotFile()
    {
        var openApi = File.ReadAllText(OpenApiPath);
        var http = File.ReadAllText(HttpSnapshotPath);

        Assert.Contains("/api/v1/calculations/engineering-core/v1/status", openApi, StringComparison.Ordinal);
        Assert.Contains("/api/v1/calculations/engineering-core/v1/diagnostics-catalog", openApi, StringComparison.Ordinal);

        Assert.Contains("GET {{baseUrl}}/api/v1/calculations/engineering-core/v1/status", http, StringComparison.Ordinal);
        Assert.Contains("GET {{baseUrl}}/api/v1/calculations/engineering-core/v1/diagnostics-catalog", http, StringComparison.Ordinal);
    }

    private static string OpenApiPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "api", "engineering-core-v1", "openapi.fragment.yml");

    private static string PostmanPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "api", "engineering-core-v1", "postman_collection.json");

    private static string ConsumerGuidePath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "api", "engineering-core-v1", "ConsumerGuide.md");

    private static string ChangelogPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "api", "engineering-core-v1", "CHANGELOG.md");

    private static string HttpSnapshotPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "api", "engineering-core-v1", "engineering-core-v1.http");
}
