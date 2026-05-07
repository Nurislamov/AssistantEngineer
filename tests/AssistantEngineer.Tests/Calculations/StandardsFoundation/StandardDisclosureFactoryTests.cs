using AssistantEngineer.Modules.Calculations.Application.Contracts.Standards;
using AssistantEngineer.Modules.Calculations.Application.Services.Standards;

namespace AssistantEngineer.Tests.Calculations.StandardsFoundation;

public sealed class StandardDisclosureFactoryTests
{
    private static readonly string[] RequiredForbiddenClaims =
    [
        "Full ISO compliance",
        "Full EN compliance",
        "pyBuildingEnergy parity",
        "EnergyPlus parity",
        "ASHRAE 140 validation"
    ];

    private readonly StandardCalculationDisclosureFactory _factory = new();

    [Fact]
    public void DefaultDisclosuresExistForAllPlannedLanes()
    {
        var disclosures = new[]
        {
            _factory.CreateThermalZonesDisclosure(),
            _factory.CreateNaturalVentilationEn16798Disclosure(),
            _factory.CreateGroundIso13370Disclosure(),
            _factory.CreateDomesticHotWaterIso12831Disclosure(),
            _factory.CreateSystemEnergyEn15316Disclosure()
        };

        Assert.Equal(5, disclosures.Length);
        Assert.All(disclosures, disclosure =>
        {
            Assert.False(string.IsNullOrWhiteSpace(disclosure.CalculationPath));
            Assert.NotNull(disclosure.ClaimBoundary);
            Assert.NotEmpty(disclosure.Diagnostics);
        });
    }

    [Fact]
    public void DefaultDisclosuresContainRequiredForbiddenClaims()
    {
        var disclosures = new[]
        {
            _factory.CreateThermalZonesDisclosure(),
            _factory.CreateNaturalVentilationEn16798Disclosure(),
            _factory.CreateGroundIso13370Disclosure(),
            _factory.CreateDomesticHotWaterIso12831Disclosure(),
            _factory.CreateSystemEnergyEn15316Disclosure()
        };

        foreach (var disclosure in disclosures)
        {
            foreach (var requiredClaim in RequiredForbiddenClaims)
            {
                Assert.Contains(
                    requiredClaim,
                    disclosure.ClaimBoundary.ForbiddenClaims,
                    StringComparer.Ordinal);
            }
        }
    }

    [Fact]
    public void DefaultDisclosuresUseInternalOrStandardInspiredModes()
    {
        var disclosures = new[]
        {
            _factory.CreateThermalZonesDisclosure(),
            _factory.CreateNaturalVentilationEn16798Disclosure(),
            _factory.CreateGroundIso13370Disclosure(),
            _factory.CreateDomesticHotWaterIso12831Disclosure(),
            _factory.CreateSystemEnergyEn15316Disclosure()
        };

        Assert.All(disclosures, disclosure =>
        {
            Assert.Contains(
                disclosure.Mode,
                new[]
                {
                    StandardCalculationMode.InternalEngineering,
                    StandardCalculationMode.StandardInspired
                });
            Assert.NotEqual(StandardCalculationMode.ExternalAnchor, disclosure.Mode);
        });
    }
}
