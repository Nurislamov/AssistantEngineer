using System.Text.Json;
using AssistantEngineer.Modules.Calculations.Application.Services.Governance;

namespace AssistantEngineer.Tests.Validation.Governance;

public sealed class UnitsGovernanceTests
{
    [Fact]
    public void UnitsGovernanceMarkdownExistsAndHasRequiredSections()
    {
        Assert.True(File.Exists(UnitsGovernancePath), $"Units governance markdown is missing: {UnitsGovernancePath}");

        var content = File.ReadAllText(UnitsGovernancePath);
        var requiredSections = new[]
        {
            "## Purpose",
            "## Scope",
            "## Non-claims",
            "## Canonical units",
            "## Field naming rule",
            "## Conversion rules",
            "## Temperature rule",
            "## Validation fixture units rule",
            "## Assumptions registry units rule",
            "## Forbidden patterns",
            "## Future improvement"
        };

        foreach (var section in requiredSections)
            Assert.Contains(section, content, StringComparison.Ordinal);
    }

    [Fact]
    public void UnitsGovernanceMarkdownContainsRequiredNonClaims()
    {
        var content = File.ReadAllText(UnitsGovernancePath);

        var py = "pyBuilding";
        var energy = "Energy";
        var exactPyPhrase = $"No {py}{energy} parity claim";
        var escapedPyPhrase = "No pyBuilding\\u0045nergy parity claim";

        Assert.Contains("No ASHRAE 140 compliance claim", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No exact EnergyPlus equivalence claim", content, StringComparison.OrdinalIgnoreCase);
        Assert.True(
            content.Contains(exactPyPhrase, StringComparison.OrdinalIgnoreCase) ||
            content.Contains(escapedPyPhrase, StringComparison.OrdinalIgnoreCase),
            "Units governance must include external-calculator parity non-claim wording.");
        Assert.Contains("No full ISO/EN compliance claim", content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("No certified/certification claim", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UnitsGovernanceMarkdownContainsRequiredUnitsAndFormulas()
    {
        var content = File.ReadAllText(UnitsGovernancePath);

        var requiredUnits = new[]
        {
            "W",
            "kW",
            "Wh",
            "kWh",
            "\u00B0C",
            "K",
            "m",
            "m\u00B2",
            "m\u00B3",
            "m\u00B3/h",
            "ACH",
            "W/(m\u00B2\u00B7K)",
            "W/m\u00B2",
            "kWh/(kg\u00B7K)",
            "Wh/(m\u00B3\u00B7K)",
            "kg/L",
            "dimensionless"
        };

        foreach (var unit in requiredUnits)
            Assert.Contains(unit, content, StringComparison.Ordinal);

        var requiredFormulas = new[]
        {
            "kW = W / 1000.0",
            "W = kW * 1000.0",
            "kWh = Wh / 1000.0",
            "Wh = kWh * 1000.0",
            "airflowM3PerH = ACH * volumeM3"
        };

        foreach (var formula in requiredFormulas)
            Assert.Contains(formula, content, StringComparison.Ordinal);
    }

    [Fact]
    public void UnitsRegistryJsonExistsParsesAndContainsRequiredEntries()
    {
        Assert.True(File.Exists(UnitsRegistryPath), $"Units registry json is missing: {UnitsRegistryPath}");

        using var document = JsonDocument.Parse(File.ReadAllText(UnitsRegistryPath));
        var root = document.RootElement;

        Assert.True(root.TryGetProperty("units", out var units));
        Assert.Equal(JsonValueKind.Array, units.ValueKind);

        var requiredFields = new[]
        {
            "unitId",
            "symbol",
            "quantity",
            "canonical",
            "allowedPropertySuffixes",
            "description"
        };

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var entry in units.EnumerateArray())
        {
            foreach (var field in requiredFields)
                Assert.True(entry.TryGetProperty(field, out _), $"Unit entry is missing field: {field}");

            var unitId = entry.GetProperty("unitId").GetString() ?? string.Empty;
            Assert.False(string.IsNullOrWhiteSpace(unitId));
            Assert.True(ids.Add(unitId), $"Duplicate unitId found: {unitId}");
        }

        var requiredIds = new[]
        {
            "UNIT-POWER-W",
            "UNIT-POWER-KW",
            "UNIT-ENERGY-WH",
            "UNIT-ENERGY-KWH",
            "UNIT-TEMPERATURE-C",
            "UNIT-TEMPERATURE-DELTA-K",
            "UNIT-LENGTH-M",
            "UNIT-AREA-M2",
            "UNIT-VOLUME-M3",
            "UNIT-AIRFLOW-M3-PER-H",
            "UNIT-AIRCHANGE-ACH",
            "UNIT-UVALUE-W-PER-M2K",
            "UNIT-IRRADIANCE-W-PER-M2",
            "UNIT-SPECIFIC-HEAT-KWH-PER-KGK",
            "UNIT-AIR-SENSIBLE-COEFFICIENT-WH-PER-M3K",
            "UNIT-DENSITY-KG-PER-L",
            "UNIT-DENSITY-KG-PER-M3",
            "UNIT-DIMENSIONLESS-EFFICIENCY",
            "UNIT-DIMENSIONLESS-SHGC",
            "UNIT-DIMENSIONLESS-SHADING-FACTOR",
            "UNIT-DIMENSIONLESS-PRIMARY-ENERGY-FACTOR"
        };

        foreach (var requiredId in requiredIds)
            Assert.Contains(requiredId, ids);
    }

    [Fact]
    public void UnitsRegistrySchemaDescriptorExistsAndContainsRequiredKeys()
    {
        Assert.True(File.Exists(UnitsRegistrySchemaPath), $"Units registry schema descriptor is missing: {UnitsRegistrySchemaPath}");

        using var schema = JsonDocument.Parse(File.ReadAllText(UnitsRegistrySchemaPath));
        var root = schema.RootElement;

        var requiredFields = root
            .GetProperty("requiredFields")
            .EnumerateArray()
            .Select(item => item.GetString() ?? string.Empty)
            .ToArray();

        Assert.Contains("unitId", requiredFields);
        Assert.Contains("symbol", requiredFields);
        Assert.Contains("quantity", requiredFields);
        Assert.Contains("canonical", requiredFields);
        Assert.Contains("allowedPropertySuffixes", requiredFields);
        Assert.Contains("description", requiredFields);
    }

    [Fact]
    public void CrossDocReferencesToUnitsGovernanceExist()
    {
        var tolerancePolicy = File.ReadAllText(TolerancePolicyPath);
        var assumptionsRegistry = File.ReadAllText(AssumptionsRegistryPath);
        var manualFixtures = File.ReadAllText(ManualFixturesPath);

        Assert.Contains("docs/engineering/units-governance.md", tolerancePolicy, StringComparison.Ordinal);
        Assert.Contains("docs/engineering/units-governance.md", assumptionsRegistry, StringComparison.Ordinal);
        Assert.Contains("docs/engineering/units-governance.md", manualFixtures, StringComparison.Ordinal);
    }

    [Fact]
    public void ManualFixtureFilesContainExpectedUnitSuffixedKeysForKeyOutputs()
    {
        var manualRoot = Path.Combine(TestPaths.RepoRoot, "tests", "fixtures", "validation", "manual");
        Assert.True(Directory.Exists(manualRoot), $"Manual fixtures directory not found: {manualRoot}");

        var expectedKeys = new[]
        {
            "totalHeatingLoadW",
            "totalHeatingLoadKw",
            "totalVentilationInfiltrationSensibleLoadW",
            "netSolarGainW",
            "groundBoundaryHeatLossW",
            "dailyUsefulDhwEnergyKWh",
            "totalPrimaryEnergyKWh",
            "windowAreaM2",
            "roomVolumeM3",
            "deltaTemperatureK",
            "indoorDesignTemperatureC"
        };

        var availableKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var expectedPath in Directory.EnumerateFiles(manualRoot, "expected-output.json", SearchOption.AllDirectories))
            CollectJsonPropertyNames(expectedPath, availableKeys);

        foreach (var inputPath in Directory.EnumerateFiles(manualRoot, "input.json", SearchOption.AllDirectories))
            CollectJsonPropertyNames(inputPath, availableKeys);

        foreach (var key in expectedKeys)
            Assert.Contains(key, availableKeys);
    }

    [Fact]
    public void UnitsGovernanceDocumentsPassClaimBoundaryScanner()
    {
        var scanner = new EngineeringClaimBoundaryScanner();
        var result = scanner.ScanRepository(
            repositoryRoot: TestPaths.RepoRoot,
            explicitFiles:
            [
                UnitsGovernancePath,
                UnitsRegistryPath,
                UnitsRegistrySchemaPath,
                TolerancePolicyPath,
                AssumptionsRegistryPath,
                ManualFixturesPath
            ]);

        Assert.Equal(0, result.ErrorCount);
    }

    private static void CollectJsonPropertyNames(string path, ISet<string> sink)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        CollectFromElement(document.RootElement, sink);
    }

    private static void CollectFromElement(JsonElement element, ISet<string> sink)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    sink.Add(property.Name);
                    CollectFromElement(property.Value, sink);
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                    CollectFromElement(item, sink);

                break;
        }
    }

    private static string UnitsGovernancePath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "engineering", "units-governance.md");

    private static string UnitsRegistryPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "engineering", "units-registry.json");

    private static string UnitsRegistrySchemaPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "engineering", "units-registry.schema.json");

    private static string TolerancePolicyPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "validation-tolerance-policy.md");

    private static string AssumptionsRegistryPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "engineering", "engineering-assumptions-registry.md");

    private static string ManualFixturesPath =>
        Path.Combine(TestPaths.RepoRoot, "docs", "validation", "manual-engineering-fixtures.md");
}
