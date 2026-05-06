using AssistantEngineer.Modules.Calculations.Application.Contracts.Rollup;
using AssistantEngineer.Modules.Calculations.Application.Services.Rollup;

namespace AssistantEngineer.Tests.Calculations.Rollup;

public sealed class EngineeringCalculationModeCatalogProviderTests
{
    private readonly EngineeringCalculationModeCatalogProvider _provider = new();

    [Fact]
    public void Catalog_ContainsExpectedStageIds()
    {
        var catalog = _provider.GetCatalog();
        var stageIds = catalog
            .SelectMany(mode => mode.Stages.Select(stage => stage.StageId))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        var expected = new[]
        {
            "AE-ISO52016-CONSTRUCTION-001",
            "AE-VENT-001",
            "AE-VENT-002",
            "AE-GROUND-001",
            "AE-GROUND-002",
            "AE-DHW-001",
            "AE-DHW-002",
            "AE-EN15316-001",
            "AE-EN15316-002",
            "AE-VALIDATION-ISO52016-001",
            "AE-VALIDATION-ISO52016-002",
            "AE-VALIDATION-PYBE-001"
        };

        foreach (var stageId in expected)
        {
            Assert.Contains(stageId, stageIds);
        }
    }

    [Fact]
    public void Catalog_CompatibilityModes_AreMarkedAsDefault()
    {
        var catalog = _provider.GetCatalog()
            .Where(mode => mode.Kind == EngineeringCalculationModeKind.CompatibilityDefault)
            .ToArray();

        Assert.NotEmpty(catalog);
        Assert.All(catalog, mode =>
        {
            Assert.True(mode.IsDefault);
            Assert.False(mode.IsOptIn);
            Assert.True(
                mode.Status is EngineeringCalculationModeStatus.Default or EngineeringCalculationModeStatus.ClosedInternalGate);
        });
    }

    [Fact]
    public void Catalog_InspiredModes_AreMarkedAsOptInAndExposeFlags()
    {
        var catalog = _provider.GetCatalog()
            .Where(mode => mode.Kind == EngineeringCalculationModeKind.InspiredOptIn)
            .ToArray();

        Assert.NotEmpty(catalog);
        Assert.All(catalog, mode =>
        {
            Assert.False(mode.IsDefault);
            Assert.True(mode.IsOptIn);
            Assert.Equal(EngineeringCalculationModeStatus.AvailableOptIn, mode.Status);
            Assert.False(string.IsNullOrWhiteSpace(mode.OptionFlagName));
        });

        var optionFlags = catalog.Select(mode => mode.OptionFlagName).ToArray();
        Assert.Contains("NaturalVentilationOptions.UseIso16798InspiredCalculator", optionFlags);
        Assert.Contains("Iso13370GroundHeatTransferOptions.UseIso13370InspiredBoundaryCalculator", optionFlags);
        Assert.Contains("DomesticHotWaterOptions.UseIso12831InspiredCalculator", optionFlags);
        Assert.Contains("SystemEnergyOptions.UseEn15316InspiredChain", optionFlags);
    }
}
