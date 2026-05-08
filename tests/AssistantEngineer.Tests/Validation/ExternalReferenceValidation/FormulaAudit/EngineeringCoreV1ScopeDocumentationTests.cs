using AssistantEngineer.Tests;

namespace AssistantEngineer.Tests.Validation.ExternalReferenceValidation.FormulaAudit;

public class EngineeringCoreV1ScopeDocumentationTests
{
    [Fact]
    public void EngineeringCoreV1ScopeDocumentExists()
    {
        Assert.True(
            File.Exists(ScopeDocumentPath),
            $"Engineering core v1 scope document must exist: {ScopeDocumentPath}");
    }

    [Fact]
    public void EngineeringCoreV1ScopeDocumentStatesMainGoal()
    {
        var content = ReadScopeDocument();

        Assert.Contains(
            "engineering calculation kernel for HVAC heating/cooling loads",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "use ISO/StandardReference as a source of calculation structure and formula principles",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "support normalized 8760 hourly weather profiles through EPW and PVGIS",
            content,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EngineeringCoreV1ScopeDocumentContainsExplicitNonClaims()
    {
        var content = ReadScopeDocument();

        var requiredNonClaims = new[]
        {
            "full ISO 52016 node/matrix solver equivalence",
            "full ISO 13370 implementation",
            "full EN 15316 generation/distribution/storage/emission chain",
            "exact StandardReference numerical equivalence",
            "exact EnergyPlus numerical equivalence",
            "ASHRAE 140 / BESTEST-style validation anchor coverage",
            "full coupled multi-zone heat-balance simulation",
            "latent load calculation",
            "moisture balance"
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
    public void EngineeringCoreV1ScopeDocumentContainsSimplifiedIsoWording()
    {
        var content = ReadScopeDocument();

        var requiredPhrases = new[]
        {
            "ISO52016-inspired simplified hourly RC",
            "ISO13370-inspired simplified ground heat-transfer model",
            "EN15316-inspired simplified final/primary energy reporting model",
            "EN12831-3-inspired simplified DHW demand model",
            "Simplified adjacent boundary model, not a coupled multi-zone solver"
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
    public void EngineeringCoreV1ScopeDocumentDefinesAnnual8760Gate()
    {
        var content = ReadScopeDocument();

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

        Assert.Contains(
            "Monthly adapter, synthetic weather and deterministic short fixtures",
            content,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EngineeringCoreV1ScopeDocumentDefinesDiagnosticsRule()
    {
        var content = ReadScopeDocument();

        Assert.Contains(
            "Error",
            content,
            StringComparison.Ordinal);

        Assert.Contains(
            "calculation must fail",
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
    public void FormulaAuditMatrixAndScopeDocumentAgreeOnClosedWeatherAndAnnualGates()
    {
        var closedIds = new[]
        {
            "WEATHER.EPW_8760",
            "WEATHER.PVGIS_8760",
            "HVAC.ANNUAL_ENERGY.HOURLY_KWH"
        };

        foreach (var closedId in closedIds)
        {
            var feature = Assert.Single(
                FormulaAuditMatrix.Features,
                item => item.CalculationId == closedId);

            Assert.Equal(
                FormulaAuditStatus.ClosedV1,
                feature.Status);
        }

        var content = ReadScopeDocument();

        Assert.Contains(
            "Weather EPW",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "Weather PVGIS",
            content,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "Annual energy",
            content,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FormulaAuditMatrixKeepsSimplifiedStandardNamedModulesHonest()
    {
        var simplifiedIds = new[]
        {
            "HVAC.GROUND.SIMPLIFIED",
            "HVAC.HOURLY_HEAT_BALANCE.SIMPLIFIED_RC",
            "HVAC.SYSTEM_ENERGY.SIMPLIFIED",
            "HVAC.DHW.SIMPLIFIED",
            "HVAC.ADJACENT_ZONE.SIMPLIFIED"
        };

        foreach (var simplifiedId in simplifiedIds)
        {
            var feature = Assert.Single(
                FormulaAuditMatrix.Features,
                item => item.CalculationId == simplifiedId);

            Assert.Contains(
                "does not claim",
                feature.Limitations,
                StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string ScopeDocumentPath =>
        Path.Combine(
            TestPaths.RepoRoot,
            "docs",
            "calculations",
            "EngineeringCoreV1Scope.md");

    private static string ReadScopeDocument() =>
        File.ReadAllText(ScopeDocumentPath);
}