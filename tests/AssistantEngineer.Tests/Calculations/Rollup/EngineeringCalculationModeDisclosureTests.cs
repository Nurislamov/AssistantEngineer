using AssistantEngineer.Modules.Calculations.Application.Services.Rollup;

namespace AssistantEngineer.Tests.Calculations.Rollup;

public sealed class EngineeringCalculationModeDisclosureTests
{
    private static readonly string[] ForbiddenTokens =
    [
        "full ISO compliance",
        "full EN compliance",
        "validated against pyBuildingEnergy",
        "pyBuildingEnergy parity",
        "EnergyPlus parity",
        "ASHRAE 140 validated",
        "ExternalParityCovered",
        "certified"
    ];

    [Fact]
    public void CatalogDisclosure_ContainsDefaultOptInStageAndClaimBoundary()
    {
        var catalog = new EngineeringCalculationModeCatalogProvider().GetCatalog();
        Assert.NotEmpty(catalog);

        foreach (var mode in catalog)
        {
            Assert.False(string.IsNullOrWhiteSpace(mode.Disclosure.DefaultOrOptInStatus));
            Assert.NotEmpty(mode.Stages);
            Assert.NotEmpty(mode.ClaimBoundary.RequiredClaims);
            Assert.NotEmpty(mode.ClaimBoundary.ForbiddenClaims);
        }
    }

    [Fact]
    public void CatalogDisclosure_DoesNotContainPositiveParityClaims()
    {
        var catalog = new EngineeringCalculationModeCatalogProvider().GetCatalog();
        var allText = string.Join(
            "\n",
            catalog.SelectMany(mode => mode.ClaimBoundary.RequiredClaims)
                .Concat(catalog.SelectMany(mode => mode.Disclosure.ClaimBoundary)));

        foreach (var token in ForbiddenTokens)
        {
            AssertTokenAppearsOnlyAsNegatedClaim(allText, token);
        }
    }

    private static void AssertTokenAppearsOnlyAsNegatedClaim(string text, string token)
    {
        var lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            if (!line.Contains(token, StringComparison.OrdinalIgnoreCase))
                continue;

            Assert.True(
                line.Contains("No ", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("not ", StringComparison.OrdinalIgnoreCase) ||
                line.Contains("forbidden", StringComparison.OrdinalIgnoreCase),
                $"Token '{token}' appears without negation in line: {line}");
        }
    }
}
