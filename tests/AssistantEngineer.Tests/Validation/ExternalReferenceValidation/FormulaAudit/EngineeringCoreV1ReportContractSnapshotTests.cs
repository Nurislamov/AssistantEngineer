using System.Text.Json;
using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Validation.ExternalReferenceValidation.FormulaAudit;

public class EngineeringCoreV1ReportContractSnapshotTests
{
    [Fact]
    public void ReportSnapshotFilesAndGeneratorExist()
    {
        var requiredFiles = new[]
        {
            HeatingReportSnapshotPath,
            CoolingReportSnapshotPath,
            AnnualEnergyDisclosureSnapshotPath,
            ReadmePath,
            ConsumerGuidePath,
            GeneratorScriptPath
        };

        foreach (var requiredFile in requiredFiles)
        {
            Assert.True(
                File.Exists(requiredFile),
                $"Required Engineering Core V1 report contract file is missing: {requiredFile}");
        }
    }

    [Fact]
    public void HeatingReportSnapshotContainsRequiredDisclosure()
    {
        using var document = ReadJson(HeatingReportSnapshotPath);
        var root = document.RootElement;

        Assert.Equal("EngineeringCoreV1.DesignPointHeating", root.GetProperty("calculationMethod").GetString());
        AssertRequiredDisclosure(root.GetProperty("calculationDisclosure"));

        var disclosure = root.GetProperty("calculationDisclosure");

        Assert.Equal(
            "Engineering-core v1 heating design-point report.",
            disclosure.GetProperty("calculationScope").GetString());

        AssertContainsAny(
            disclosure.GetProperty("assumptions"),
            "Transmission uses steady-state U*A*");

        AssertContainsAny(
            disclosure.GetProperty("assumptions"),
            "Ventilation and infiltration use sensible-only airflow heat transfer.");
    }

    [Fact]
    public void CoolingReportSnapshotContainsRequiredDisclosure()
    {
        using var document = ReadJson(CoolingReportSnapshotPath);
        var root = document.RootElement;

        Assert.Equal("EngineeringCoreV1.DesignPointCooling", root.GetProperty("calculationMethod").GetString());
        AssertRequiredDisclosure(root.GetProperty("calculationDisclosure"));

        var disclosure = root.GetProperty("calculationDisclosure");

        Assert.Equal(
            "Engineering-core v1 cooling design-point report.",
            disclosure.GetProperty("calculationScope").GetString());

        AssertContainsAny(
            disclosure.GetProperty("assumptions"),
            "Window solar gains use simplified SHGC/shading based engineering model.");

        AssertContainsAny(
            disclosure.GetProperty("assumptions"),
            "Surface irradiance uses ISO52010-inspired solar geometry and isotropic sky transposition.");
    }

    [Fact]
    public void AnnualEnergyDisclosureSnapshotContainsTrueHourly8760Requirements()
    {
        using var document = ReadJson(AnnualEnergyDisclosureSnapshotPath);
        var root = document.RootElement;

        Assert.Equal("TrueHourlySimulation", root.GetProperty("energyDataSource").GetString());
        Assert.True(root.GetProperty("isTrueHourly8760").GetBoolean());
        Assert.Equal(8760, root.GetProperty("hourlyRecordCount").GetInt32());

        AssertRequiredDisclosure(root.GetProperty("calculationDisclosure"));

        var flags = root
            .GetProperty("requiredAnnual8760Flags")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains("EnergyDataSource = TrueHourlySimulation", flags);
        Assert.Contains("IsTrueHourly8760 = true", flags);
        Assert.Contains("HourlyRecordCount = 8760", flags);

        AssertContainsAny(
            root.GetProperty("calculationDisclosure").GetProperty("warnings"),
            "Annual energy is true hourly 8760 only when EnergyDataSource=TrueHourlySimulation");
    }

    [Fact]
    public void ReportSnapshotsKeepNonClaimsAndOutOfScopeItemsVisible()
    {
        var snapshotPaths = new[]
        {
            HeatingReportSnapshotPath,
            CoolingReportSnapshotPath,
            AnnualEnergyDisclosureSnapshotPath
        };

        foreach (var snapshotPath in snapshotPaths)
        {
            using var document = ReadJson(snapshotPath);
            var disclosure = document.RootElement.GetProperty("calculationDisclosure");

            AssertContainsAny(
                disclosure.GetProperty("explicitNonClaims"),
                "No exact EnergyPlus numerical equivalence claim.");

            AssertContainsAny(
                disclosure.GetProperty("explicitNonClaims"),
                "No exact StandardReference numerical equivalence claim.");

            AssertContainsAny(
                disclosure.GetProperty("explicitNonClaims"),
                "No ASHRAE 140 / BESTEST-style validation anchor coverage claim.");

            AssertContainsAny(
                disclosure.GetProperty("explicitNonClaims"),
                "No full ISO 52016 node/matrix solver equivalence claim.");

            AssertContainsAny(
                disclosure.GetProperty("outOfScopeV1"),
                "HVAC.LATENT_LOAD");

            AssertContainsAny(
                disclosure.GetProperty("outOfScopeV1"),
                "HVAC.MOISTURE_BALANCE");
        }
    }

    [Fact]
    public void ReportSnapshotsReferenceCoreDocumentationFiles()
    {
        var snapshotPaths = new[]
        {
            HeatingReportSnapshotPath,
            CoolingReportSnapshotPath,
            AnnualEnergyDisclosureSnapshotPath
        };

        foreach (var snapshotPath in snapshotPaths)
        {
            using var document = ReadJson(snapshotPath);
            var docs = document
                .RootElement
                .GetProperty("calculationDisclosure")
                .GetProperty("documentationFiles")
                .EnumerateArray()
                .Select(item => item.GetString() ?? string.Empty)
                .ToArray();

            Assert.Contains("docs/calculations/EngineeringCoreV1Scope.md", docs);
            Assert.Contains("docs/calculations/EngineeringCoreV1ReleaseNotes.md", docs);
            Assert.Contains("docs/calculations/EnergyPlusAshrae140ValidationPlan.md", docs);
        }
    }

    [Fact]
    public void ReportSnapshotReadmeAndConsumerGuideDocumentDisclosureExportAndNonClaims()
    {
        var combined = string.Join(
            Environment.NewLine,
            File.ReadAllText(ReadmePath),
            File.ReadAllText(ConsumerGuidePath));

        var requiredPhrases = new[]
        {
            "calculationDisclosure",
            "coreStatus",
            "warnings",
            "assumptions",
            "explicitNonClaims",
            "outOfScopeV1",
            "documentationFiles",
            "PDF, Excel, JSON and frontend exports",
            "no exact EnergyPlus numerical equivalence",
            "no ASHRAE 140 / BESTEST-style validation anchor coverage",
            "HourlyRecordCount = 8760"
        };

        foreach (var requiredPhrase in requiredPhrases)
        {
            Assert.Contains(
                requiredPhrase,
                combined,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void ReportSnapshotGeneratorWritesAllExpectedSnapshots()
    {
        var script = File.ReadAllText(GeneratorScriptPath);

        Assert.Contains("AssistantEngineer.Tools.EngineeringCoreContracts.csproj", script, StringComparison.Ordinal);
        Assert.Contains("generate-report-contract-snapshots", script, StringComparison.Ordinal);
        Assert.Contains("dotnet run --project", script, StringComparison.Ordinal);

        Assert.DoesNotContain("ConvertTo-Json", script, StringComparison.Ordinal);
        Assert.DoesNotContain("[ordered]@", script, StringComparison.Ordinal);

        var tool = File.ReadAllText(ToolProgramPath);

        Assert.Contains("heating-report.sample.json", tool, StringComparison.Ordinal);
        Assert.Contains("cooling-report.sample.json", tool, StringComparison.Ordinal);
        Assert.Contains("annual-energy-disclosure.sample.json", tool, StringComparison.Ordinal);
        Assert.Contains("EngineeringCoreV1.DesignPointHeating", tool, StringComparison.Ordinal);
        Assert.Contains("EngineeringCoreV1.DesignPointCooling", tool, StringComparison.Ordinal);
        Assert.Contains("EngineeringCoreV1.TrueHourly8760", tool, StringComparison.Ordinal);
    }
    private static void AssertRequiredDisclosure(JsonElement disclosure)
    {
        Assert.Equal("ClosedV1", disclosure.GetProperty("coreStatus").GetString());

        var requiredProperties = new[]
        {
            "calculationScope",
            "calculationMethod",
            "actualMethod",
            "warnings",
            "assumptions",
            "explicitNonClaims",
            "outOfScopeV1",
            "documentationFiles"
        };

        foreach (var requiredProperty in requiredProperties)
        {
            Assert.True(
                disclosure.TryGetProperty(requiredProperty, out _),
                $"Disclosure is missing required property: {requiredProperty}");
        }

        Assert.NotEmpty(disclosure.GetProperty("warnings").EnumerateArray());
        Assert.NotEmpty(disclosure.GetProperty("assumptions").EnumerateArray());
        Assert.NotEmpty(disclosure.GetProperty("explicitNonClaims").EnumerateArray());
        Assert.NotEmpty(disclosure.GetProperty("outOfScopeV1").EnumerateArray());
        Assert.NotEmpty(disclosure.GetProperty("documentationFiles").EnumerateArray());
    }

    private static void AssertContainsAny(
        JsonElement array,
        string expected)
    {
        var values = array
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains(
            values,
            value => value.Contains(
                expected,
                StringComparison.OrdinalIgnoreCase));
    }

    private static JsonDocument ReadJson(string path) =>
        JsonDocument.Parse(File.ReadAllText(path));

    private static string HeatingReportSnapshotPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "reports", "engineering-core-v1", "heating-report.sample.json");

    private static string CoolingReportSnapshotPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "reports", "engineering-core-v1", "cooling-report.sample.json");

    private static string AnnualEnergyDisclosureSnapshotPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "reports", "engineering-core-v1", "annual-energy-disclosure.sample.json");

    private static string ReadmePath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "reports", "engineering-core-v1", "README.md");

    private static string ConsumerGuidePath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "reports", "engineering-core-v1", "ConsumerGuide.md");

    private static string GeneratorScriptPath =>
        Path.Combine(TestPaths.RepoRoot, "scripts", "engineering-core", "generate-engineering-core-v1-report-contract-snapshots.ps1");
    private static string ToolProgramPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "tools",
            "AssistantEngineer.Tools.EngineeringCoreContracts",
            "Program.cs");
}

