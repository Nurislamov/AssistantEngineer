using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;

namespace AssistantEngineer.Tests.Calculations.SystemEnergy;

public sealed class SystemEnergyUsefulLoadValidatorTests
{
    private readonly SystemEnergyUsefulLoadValidator _validator = new();

    [Fact]
    public void AcceptsValidUsefulLoadSet()
    {
        var input = SystemEnergyTestData.CreateUsefulLoadSet();

        var result = _validator.Validate(input);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void RejectsMissingCalculationId()
    {
        var input = SystemEnergyTestData.CreateUsefulLoadSet() with { CalculationId = string.Empty };

        var result = _validator.Validate(input);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-USEFUL-CALCULATION-ID-MISSING");
    }

    [Fact]
    public void RejectsMissingUsefulAndAuxiliaryLoads()
    {
        var input = SystemEnergyTestData.CreateUsefulLoadSet(usefulLoads: [], auxiliaryLoads: []);

        var result = _validator.Validate(input);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-USEFUL-LOADS-MISSING");
    }

    [Fact]
    public void RejectsInvalidHourlyUsefulProfile()
    {
        var usefulLoad = SystemEnergyTestData.CreateUsefulLoad() with
        {
            HourlyUsefulEnergyKWh8760 = Enumerable.Repeat(1.0, 8759).ToArray()
        };
        var input = SystemEnergyTestData.CreateUsefulLoadSet(usefulLoads: [usefulLoad]);

        var result = _validator.Validate(input);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-USEFUL-HOURLY-PROFILE-INVALID");
    }

    [Fact]
    public void RejectsUnknownEndUse()
    {
        var usefulLoad = SystemEnergyTestData.CreateUsefulLoad() with
        {
            EndUse = SystemEnergyEndUse.Unknown
        };
        var input = SystemEnergyTestData.CreateUsefulLoadSet(usefulLoads: [usefulLoad]);

        var result = _validator.Validate(input);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-USEFUL-ENDUSE-UNKNOWN");
    }

    [Fact]
    public void RejectsInvalidAuxiliaryCarrier()
    {
        var auxiliary = new SystemEnergyAuxiliaryLoadInput(
            AuxiliaryId: "A1",
            BuildingId: "B1",
            ZoneId: "Z1",
            RoomId: "R1",
            EndUse: SystemEnergyEndUse.Auxiliary,
            Carrier: SystemEnergyCarrier.Unknown,
            HourlyAuxiliaryEnergyKWh8760: SystemEnergyTestData.HourlyConstant(0.1),
            Source: "test",
            Diagnostics: []);
        var input = SystemEnergyTestData.CreateUsefulLoadSet(auxiliaryLoads: [auxiliary]);

        var result = _validator.Validate(input);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Code == "AE-SYS-AUXILIARY-CARRIER-UNKNOWN");
    }
}
