using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

namespace AssistantEngineer.Tests.Calculations.SystemEnergy;

public sealed class SystemEnergyFactorSetValidatorTests
{
    private readonly SystemEnergyFactorSetValidator _validator = new();

    [Fact]
    public void AcceptsValidFactorSet()
    {
        var factorSet = SystemEnergyPrimaryTestData.CreateFactorSet(
            [
                SystemEnergyPrimaryTestData.CreatePrimaryFactor(
                    SystemEnergyCarrier.Electricity,
                    renewableFactor: 0.2,
                    nonRenewableFactor: 1.8,
                    totalFactor: 2.0)
            ]);

        var result = _validator.Validate(factorSet);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void RejectsMissingFactorSetId()
    {
        var factorSet = SystemEnergyPrimaryTestData.CreateFactorSet(
            [SystemEnergyPrimaryTestData.CreatePrimaryFactor(SystemEnergyCarrier.Electricity, 0.2, 1.8, 2.0)],
            factorSetId: string.Empty);

        var result = _validator.Validate(factorSet);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-FACTORSET-ID-MISSING");
    }

    [Fact]
    public void RejectsMissingPrimaryFactors()
    {
        var factorSet = SystemEnergyPrimaryTestData.CreateFactorSet([]);

        var result = _validator.Validate(factorSet);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-PRIMARY-FACTORS-MISSING");
    }

    [Fact]
    public void RejectsUnknownCarrier()
    {
        var factorSet = SystemEnergyPrimaryTestData.CreateFactorSet(
            [SystemEnergyPrimaryTestData.CreatePrimaryFactor(SystemEnergyCarrier.Unknown, 0.2, 1.8, 2.0)]);

        var result = _validator.Validate(factorSet);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-FACTOR-CARRIER-UNKNOWN");
    }

    [Fact]
    public void RejectsNegativeFactor()
    {
        var factorSet = SystemEnergyPrimaryTestData.CreateFactorSet(
            [SystemEnergyPrimaryTestData.CreatePrimaryFactor(SystemEnergyCarrier.Electricity, -0.1, 1.8, 1.7)]);

        var result = _validator.Validate(factorSet);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-FACTOR-NEGATIVE");
    }

    [Fact]
    public void ReportsTotalMismatch()
    {
        var factorSet = SystemEnergyPrimaryTestData.CreateFactorSet(
            [SystemEnergyPrimaryTestData.CreatePrimaryFactor(SystemEnergyCarrier.Electricity, 0.2, 1.8, 3.0)]);

        var result = _validator.Validate(factorSet);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-FACTOR-TOTAL-MISMATCH");
    }

    [Fact]
    public void ReportsDuplicateCarrierFactor()
    {
        var factorSet = SystemEnergyPrimaryTestData.CreateFactorSet(
            [
                SystemEnergyPrimaryTestData.CreatePrimaryFactor(SystemEnergyCarrier.Electricity, 0.2, 1.8, 2.0),
                SystemEnergyPrimaryTestData.CreatePrimaryFactor(SystemEnergyCarrier.Electricity, 0.1, 1.9, 2.0)
            ]);

        var result = _validator.Validate(factorSet);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-FACTOR-DUPLICATE-CARRIER");
    }

    [Fact]
    public void ReportsNationalAnnexPlaceholderNotCompliance()
    {
        var factorSet = SystemEnergyPrimaryTestData.CreateFactorSet(
            [
                SystemEnergyPrimaryTestData.CreatePrimaryFactor(
                    SystemEnergyCarrier.Electricity,
                    0.2,
                    1.8,
                    2.0,
                    sourceKind: SystemEnergyFactorSourceKind.NationalAnnexPlaceholder)
            ]);

        var result = _validator.Validate(factorSet);

        Assert.Contains(
            result.Diagnostics,
            diagnostic => diagnostic.Code == "AE-SYS-FACTOR-NATIONAL-ANNEX-PLACEHOLDER-NOT-COMPLIANCE");
    }
}
