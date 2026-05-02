using System.Text.Json;
using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Parity.EnergyCalculationParity.FormulaAudit;

public class EngineeringCoreV1FormulaAuditDiagnosticsCatalogTests
{
    [Fact]
    public void DiagnosticsCatalogJsonExists()
    {
        Assert.True(
            File.Exists(DiagnosticsCatalogJsonPath),
            $"Diagnostics catalog JSON must exist: {DiagnosticsCatalogJsonPath}");
    }

    [Fact]
    public void DiagnosticsCatalogMarkdownExists()
    {
        Assert.True(
            File.Exists(DiagnosticsCatalogMarkdownPath),
            $"Diagnostics catalog markdown must exist: {DiagnosticsCatalogMarkdownPath}");
    }

    [Fact]
    public void DiagnosticsUxDocumentExists()
    {
        Assert.True(
            File.Exists(DiagnosticsUxDocumentPath),
            $"Diagnostics UX document must exist: {DiagnosticsUxDocumentPath}");
    }

    [Fact]
    public void DiagnosticsCatalogDeclaresSeverityRulesAndSuccessRule()
    {
        using var catalog = ReadCatalog();
        var root = catalog.RootElement;

        Assert.Equal(
            "Engineering Core V1 Diagnostics Catalog",
            root.GetProperty("catalogName").GetString());

        Assert.Equal("v1", root.GetProperty("version").GetString());
        Assert.Equal("ClosedV1", root.GetProperty("status").GetString());

        var rules = root.GetProperty("rules");

        Assert.Contains(
            "Calculation must fail",
            rules.GetProperty("error").GetString(),
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "Calculation may succeed",
            rules.GetProperty("warning").GetString(),
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "CalculationDiagnosticSeverity.Error",
            rules.GetProperty("successRule").GetString(),
            StringComparison.Ordinal);
    }

    [Fact]
    public void DiagnosticsCatalogCodesAreUnique()
    {
        var duplicateCodes = GetDiagnostics()
            .GroupBy(item => item.Code, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            duplicateCodes.Length == 0,
            $"Diagnostic codes must be unique: {string.Join(", ", duplicateCodes)}.");
    }

    [Fact]
    public void DiagnosticsCatalogItemsHaveRequiredUserFacingFields()
    {
        var violations = GetDiagnostics()
            .Where(item =>
                string.IsNullOrWhiteSpace(item.Code) ||
                string.IsNullOrWhiteSpace(item.Severity) ||
                string.IsNullOrWhiteSpace(item.Category) ||
                string.IsNullOrWhiteSpace(item.UserMessage) ||
                string.IsNullOrWhiteSpace(item.UserAction) ||
                string.IsNullOrWhiteSpace(item.ClosedV1Gate))
            .Select(item => item.Code)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Diagnostics must include code, severity, category, userMessage, userAction and closedV1Gate: {string.Join(", ", violations)}.");
    }

    [Fact]
    public void DiagnosticsCatalogUsesOnlyAllowedSeverities()
    {
        var allowed = new HashSet<string>(
            ["Error", "Warning", "Info"],
            StringComparer.Ordinal);

        var invalidSeverities = GetDiagnostics()
            .Where(item => !allowed.Contains(item.Severity))
            .Select(item => $"{item.Code}:{item.Severity}")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            invalidSeverities.Length == 0,
            $"Diagnostics contain invalid severities: {string.Join(", ", invalidSeverities)}.");
    }

    [Fact]
    public void DiagnosticsCatalogReferencesKnownFormulaAuditGates()
    {
        var knownGateIds = FormulaAuditMatrix.Features
            .Select(feature => feature.CalculationId)
            .ToHashSet(StringComparer.Ordinal);

        var unknownGateReferences = GetDiagnostics()
            .Where(item => !knownGateIds.Contains(item.ClosedV1Gate))
            .Select(item => $"{item.Code}:{item.ClosedV1Gate}")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            unknownGateReferences.Length == 0,
            $"Diagnostics reference unknown FormulaAuditMatrix gates: {string.Join(", ", unknownGateReferences)}.");
    }

    [Fact]
    public void DiagnosticsCatalogCoversCoreAnnualEnergyWarningsAndErrors()
    {
        var codes = GetDiagnosticCodes();

        var requiredCodes = new[]
        {
            "AnnualEnergy.InvalidArea",
            "AnnualEnergy.NoHourlyInputs",
            "AnnualEnergy.InvalidHourDuration",
            "AnnualEnergy.InvalidMonth",
            "AnnualEnergy.Not8760",
            "AnnualEnergy.SyntheticWeather",
            "SolarWeather.SyntheticWeatherUsed",
            "AnnualEnergy.TrueHourlySimulationUsed",
            "AnnualEnergy.MonthlyBalanceAdapter",
            "AnnualEnergy.SourceUnavailable"
        };

        foreach (var requiredCode in requiredCodes)
        {
            Assert.Contains(requiredCode, codes);
        }
    }

    [Fact]
    public void DiagnosticsCatalogCoversSystemEnergyEquipmentAggregationAndAdjacentDiagnostics()
    {
        var codes = GetDiagnosticCodes();

        var requiredCodes = new[]
        {
            "SystemEnergy.InvalidUsefulHeating",
            "SystemEnergy.InvalidCoolingCop",
            "SystemEnergy.HeatingAssumptionMissing",
            "SystemEnergy.CoolingAssumptionMissing",
            "EquipmentSizing.InvalidSafetyFactor",
            "EquipmentSizing.NoEquipmentFound",
            "EquipmentSizing.NoRecommendedEquipment",
            "Aggregation.InvalidRoomArea",
            "Aggregation.HourlyUnavailable",
            "Transmission.MissingBoundaryTemperature"
        };

        foreach (var requiredCode in requiredCodes)
        {
            Assert.Contains(requiredCode, codes);
        }
    }

    [Fact]
    public void ErrorDiagnosticsHaveBlockingUserActions()
    {
        var weakErrorActions = GetDiagnostics()
            .Where(item => item.Severity == "Error")
            .Where(item =>
                !ContainsAny(
                    item.UserAction,
                    "correct",
                    "check",
                    "enter",
                    "provide",
                    "generate",
                    "remove"))
            .Select(item => $"{item.Code}: {item.UserAction}")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            weakErrorActions.Length == 0,
            $"Error diagnostics should have clear corrective user actions: {string.Join("; ", weakErrorActions)}.");
    }

    [Fact]
    public void Annual8760WarningsTellUserNotToPresentAsTrue8760WhenApplicable()
    {
        var diagnostics = GetDiagnostics()
            .Where(item => item.Code is
                "AnnualEnergy.Not8760" or
                "AnnualEnergy.TrueHourlySimulationPartial" or
                "AnnualEnergy.MonthlyBalanceAdapter")
            .ToArray();

        Assert.NotEmpty(diagnostics);

        foreach (var diagnostic in diagnostics)
        {
            Assert.Contains(
                "not",
                diagnostic.UserAction,
                StringComparison.OrdinalIgnoreCase);

            Assert.Contains(
                "8760",
                diagnostic.UserAction,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void DiagnosticsMarkdownDocumentsSeverityRulesAnnual8760BlockingDiagnosticsAndNonClaims()
    {
        var content = File.ReadAllText(DiagnosticsCatalogMarkdownPath);

        var requiredPhrases = new[]
        {
            "A successful calculation result must not contain CalculationDiagnosticSeverity.Error",
            "AnnualEnergy.Not8760",
            "AnnualEnergy.MonthlyBalanceAdapter",
            "SystemEnergy.HeatingAssumptionMissing",
            "EquipmentSizing.DefaultHeatingSafetyFactorUsed",
            "Transmission.MissingBoundaryTemperature",
            "exact EnergyPlus numerical parity",
            "ASHRAE 140 validation coverage"
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
    public void DiagnosticsUxDocumentRequiresVisibleWarningsAndAnnual8760Treatment()
    {
        var content = File.ReadAllText(DiagnosticsUxDocumentPath);

        Assert.Contains(
            "Warnings must not be hidden",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "EnergyDataSource = TrueHourlySimulation",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "HourlyRecordCount = 8760",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "calculationDisclosure",
            content,
            StringComparison.Ordinal);
    }

    private static bool ContainsAny(
        string value,
        params string[] tokens) =>
        tokens.Any(token => value.Contains(token, StringComparison.OrdinalIgnoreCase));

    private static HashSet<string> GetDiagnosticCodes() =>
        GetDiagnostics()
            .Select(item => item.Code)
            .ToHashSet(StringComparer.Ordinal);

    private static IReadOnlyList<DiagnosticCatalogItem> GetDiagnostics()
    {
        using var catalog = ReadCatalog();

        return catalog
            .RootElement
            .GetProperty("diagnostics")
            .EnumerateArray()
            .Select(item => new DiagnosticCatalogItem(
                Code: item.GetProperty("code").GetString() ?? string.Empty,
                Severity: item.GetProperty("severity").GetString() ?? string.Empty,
                Category: item.GetProperty("category").GetString() ?? string.Empty,
                UserMessage: item.GetProperty("userMessage").GetString() ?? string.Empty,
                UserAction: item.GetProperty("userAction").GetString() ?? string.Empty,
                ClosedV1Gate: item.GetProperty("closedV1Gate").GetString() ?? string.Empty))
            .ToArray();
    }

    private static JsonDocument ReadCatalog() =>
        JsonDocument.Parse(File.ReadAllText(DiagnosticsCatalogJsonPath));

    private sealed record DiagnosticCatalogItem(
        string Code,
        string Severity,
        string Category,
        string UserMessage,
        string UserAction,
        string ClosedV1Gate);

    private static string DiagnosticsCatalogJsonPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "calculations",
            "EngineeringCoreV1DiagnosticsCatalog.json");

    private static string DiagnosticsCatalogMarkdownPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "calculations",
            "EngineeringCoreV1DiagnosticsCatalog.md");

    private static string DiagnosticsUxDocumentPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "frontend",
            "EngineeringCoreV1DiagnosticsUx.md");
}
