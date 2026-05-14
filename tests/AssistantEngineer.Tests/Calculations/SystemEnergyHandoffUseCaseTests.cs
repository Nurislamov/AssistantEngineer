using AssistantEngineer.Modules.Buildings.Domain.Enums;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Pipeline;
using AssistantEngineer.Modules.Calculations.Application.Contracts.AnnualEnergy;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Calculations;
using AssistantEngineer.Modules.Calculations.Application.Contracts.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy.En15316;
using AssistantEngineer.Modules.Calculations.Application.Options;
using AssistantEngineer.Modules.Calculations.Application.Services.Pipeline;
using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy.En15316;
using AssistantEngineer.SharedKernel.Primitives;
using Microsoft.Extensions.Options;

namespace AssistantEngineer.Tests.Calculations;

public sealed class SystemEnergyHandoffUseCaseTests
{
    [Fact]
    public async Task CalculateBuildingSystemEnergyFromUsefulDemandAsync_ReturnsValidation_WhenServicesAreNotConfigured()
    {
        var useCase = new SystemEnergyHandoffUseCase(
            new StubUsefulDemandProvider(_ => Task.FromResult(Result<BuildingEnergyBalanceResult>.Success(CreateUsefulDemand()))),
            systemEnergyEngine: null,
            systemEnergyHandoffBuilder: null,
            Options.Create(new SystemEnergyOptions
            {
                UseEn15316InspiredChain = true,
                UseEn15316CircuitLevelCalculator = true
            }));

        var result = await useCase.CalculateBuildingSystemEnergyFromUsefulDemandAsync(buildingId: 10);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
        Assert.Equal("System-energy handoff services are not configured.", result.Error);
    }

    [Fact]
    public async Task CalculateBuildingSystemEnergyFromUsefulDemandAsync_ReturnsValidation_WhenOptionsAreDisabled()
    {
        var options = new SystemEnergyOptions();
        var useCase = CreateUseCase(
            _ => Task.FromResult(Result<BuildingEnergyBalanceResult>.Success(CreateUsefulDemand())),
            options);

        var result = await useCase.CalculateBuildingSystemEnergyFromUsefulDemandAsync(buildingId: 10);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
        Assert.Equal(
            "System-energy circuit-level handoff requires explicit opt-in in Calculations:SystemEnergy options.",
            result.Error);
    }

    [Fact]
    public async Task CalculateBuildingSystemEnergyFromUsefulDemandAsync_PropagatesUsefulDemandFailure()
    {
        var useCase = CreateUseCase(
            _ => Task.FromResult(Result<BuildingEnergyBalanceResult>.Validation("useful-demand-failed")),
            new SystemEnergyOptions
            {
                UseEn15316InspiredChain = true,
                UseEn15316CircuitLevelCalculator = true
            });

        var result = await useCase.CalculateBuildingSystemEnergyFromUsefulDemandAsync(buildingId: 10);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
        Assert.Equal("useful-demand-failed", result.Error);
    }

    [Fact]
    public async Task CalculateBuildingSystemEnergyFromUsefulDemandAsync_PropagatesSystemEnergyFailure()
    {
        var options = new SystemEnergyOptions
        {
            UseEn15316InspiredChain = true,
            UseEn15316CircuitLevelCalculator = true
        };
        var useCase = CreateUseCase(
            _ => Task.FromResult(Result<BuildingEnergyBalanceResult>.Success(CreateUsefulDemandWithInvalidMonthlyValue())),
            options);

        var result = await useCase.CalculateBuildingSystemEnergyFromUsefulDemandAsync(buildingId: 10);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultErrorType.Validation, result.ErrorType);
        Assert.Contains("must be finite and non-negative", result.Error, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CalculateBuildingSystemEnergyFromUsefulDemandAsync_ReturnsSuccess_OnHappyPath()
    {
        var options = new SystemEnergyOptions
        {
            UseEn15316InspiredChain = true,
            UseEn15316CircuitLevelCalculator = true,
            DefaultHeatingTechnology = En15316GenerationTechnology.DirectElectric,
            DefaultHeatingCarrier = En15316EnergyCarrier.Electricity,
            DefaultCoolingTechnology = En15316GenerationTechnology.Chiller,
            DefaultCoolingCarrier = En15316EnergyCarrier.Electricity
        };
        var useCase = CreateUseCase(
            _ => Task.FromResult(Result<BuildingEnergyBalanceResult>.Success(CreateUsefulDemand())),
            options);

        var result = await useCase.CalculateBuildingSystemEnergyFromUsefulDemandAsync(
            buildingId: 10,
            dhwHandoff: CreateDhwHandoff(hourlyUseful: 0.1));

        Assert.True(result.IsSuccess, result.Error);
        Assert.Equal("Standard-Based Calculation", result.Value.CalculationMethodLabel);
        Assert.NotNull(result.Value.SystemEnergyResult);
        Assert.True(result.Value.SystemEnergyInput.UsefulHeatingEnergyKWh > 0);
        Assert.Contains(result.Value.Diagnostics, diagnostic => diagnostic.Code == "SystemEnergy.Handoff.Built");
    }

    private static SystemEnergyHandoffUseCase CreateUseCase(
        Func<int, Task<Result<BuildingEnergyBalanceResult>>> usefulDemandFactory,
        SystemEnergyOptions options)
    {
        var provider = new StubUsefulDemandProvider(usefulDemandFactory);
        var referenceData = new En15316SystemEnergyReferenceDataProvider();
        var engine = new SystemEnergyEngine(
            Options.Create(options),
            new En15316SystemEnergyChainCalculator(referenceData),
            new En15316SystemEnergyApplicationAdapter(),
            new En15316HeatingSystemCircuitCalculator(
                new En15316HeatingSystemInputValidator(),
                referenceData));
        var handoffBuilder = new SystemEnergyUsefulEnergyHandoffBuilder(referenceData);

        return new SystemEnergyHandoffUseCase(
            provider,
            engine,
            handoffBuilder,
            Options.Create(options));
    }

    private static BuildingEnergyBalanceResult CreateUsefulDemand()
    {
        return new BuildingEnergyBalanceResult
        {
            BuildingId = 10,
            BuildingName = "Building 10",
            EnergyDataSource = "MonthlyBalanceAdapter",
            IsTrueHourly8760 = false,
            HourlyRecordCount = 0,
            MonthlyBalances =
            [
                new MonthlyEnergyBalance { Month = 1, HeatingDemandKWh = 120, CoolingDemandKWh = 0 },
                new MonthlyEnergyBalance { Month = 7, HeatingDemandKWh = 0, CoolingDemandKWh = 60 }
            ],
            AnnualHeatingDemandKWh = 120,
            AnnualCoolingDemandKWh = 60,
            AnnualTotalDemandKWh = 180
        };
    }

    private static BuildingEnergyBalanceResult CreateUsefulDemandWithInvalidMonthlyValue()
    {
        return new BuildingEnergyBalanceResult
        {
            BuildingId = 10,
            BuildingName = "Building 10",
            EnergyDataSource = "MonthlyBalanceAdapter",
            IsTrueHourly8760 = false,
            HourlyRecordCount = 0,
            MonthlyBalances =
            [
                new MonthlyEnergyBalance { Month = 1, HeatingDemandKWh = double.NaN, CoolingDemandKWh = 0 }
            ],
            AnnualHeatingDemandKWh = 0,
            AnnualCoolingDemandKWh = 0,
            AnnualTotalDemandKWh = 0
        };
    }

    private static DomesticHotWaterEn15316Handoff CreateDhwHandoff(double hourlyUseful)
    {
        var useful = Enumerable.Repeat(hourlyUseful, 8760).ToArray();
        var zeros = Enumerable.Repeat(0.0, 8760).ToArray();

        return new DomesticHotWaterEn15316Handoff(
            CalculationId: "DHW-HANDOFF-TEST",
            EndUse: "DomesticHotWater",
            UsefulEnergySource: "Internal deterministic test anchor",
            AnnualUsefulDhwEnergyKWh: useful.Sum(),
            AnnualDhwSystemHeatRequirementKWh: useful.Sum(),
            AnnualDhwAuxiliaryElectricityKWh: 0.0,
            HourlyUsefulDhwEnergyKWh8760: useful,
            HourlyDhwSystemHeatRequirementKWh8760: useful,
            HourlyDhwAuxiliaryElectricityKWh8760: zeros,
            HourlyRecoverableLossKWh8760: zeros,
            HourlyNonRecoverableLossKWh8760: zeros,
            Diagnostics: []);
    }

    private sealed class StubUsefulDemandProvider : ISystemEnergyHandoffUsefulDemandProvider
    {
        private readonly Func<int, Task<Result<BuildingEnergyBalanceResult>>> _factory;

        public StubUsefulDemandProvider(
            Func<int, Task<Result<BuildingEnergyBalanceResult>>> factory)
        {
            _factory = factory;
        }

        public Task<Result<BuildingEnergyBalanceResult>> CalculateUsefulDemandAsync(
            int buildingId,
            CoolingLoadCalculationMethod coolingMethod,
            HeatingLoadCalculationMethod heatingMethod,
            CancellationToken cancellationToken) =>
            _factory(buildingId);
    }
}
