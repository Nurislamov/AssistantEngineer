using AssistantEngineer.Modules.Calculations.Application.Contracts.CoolingSystems;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.CoolingSystems;

public sealed class CoolingSystemEnergyService
{
    private readonly SystemEnergyEngine _systemEnergy;

    public CoolingSystemEnergyService(SystemEnergyEngine? systemEnergy = null)
    {
        _systemEnergy = systemEnergy ?? new SystemEnergyEngine();
    }

    public Result<CoolingSystemEnergyResult> Calculate(
        Iso52016AnnualEnergyNeedResult energyNeed,
        CoolingSystemEnergyRequest request)
    {
        if (request.SeasonalCop <= 0 || request.SeasonalCop > 15)
            return Result<CoolingSystemEnergyResult>.Validation("Seasonal COP must be between 0 and 15.");

        if (request.DistributionEfficiency <= 0 || request.DistributionEfficiency > 1)
            return Result<CoolingSystemEnergyResult>.Validation("Distribution efficiency must be between 0 and 1.");

        if (request.EmissionEfficiency <= 0 || request.EmissionEfficiency > 1)
            return Result<CoolingSystemEnergyResult>.Validation("Emission efficiency must be between 0 and 1.");

        if (request.AuxiliaryEnergyKWh < 0)
            return Result<CoolingSystemEnergyResult>.Validation("Auxiliary energy cannot be negative.");

        var effectiveCop = request.SeasonalCop *
            request.DistributionEfficiency *
            request.EmissionEfficiency;
        var systemEnergy = _systemEnergy.Calculate(new SystemEnergyInput(
            UsefulCoolingEnergyKWh: energyNeed.AnnualCoolingDemandKWh,
            CoolingCop: effectiveCop,
            FanEnergyKWh: request.AuxiliaryEnergyKWh,
            DiagnosticsContext: $"Building {energyNeed.BuildingId} cooling system energy"));
        if (systemEnergy.IsFailure)
            return Result<CoolingSystemEnergyResult>.Failure(systemEnergy);

        var deliveredCooling = energyNeed.AnnualCoolingDemandKWh /
            (request.DistributionEfficiency * request.EmissionEfficiency);
        var compressorElectricity = systemEnergy.Value.FinalCoolingEnergyKWh;
        var finalElectricity = systemEnergy.Value.TotalFinalEnergyKWh;

        return Result<CoolingSystemEnergyResult>.Success(new CoolingSystemEnergyResult(
            UsefulCoolingDemandKWh: Round(energyNeed.AnnualCoolingDemandKWh),
            DeliveredCoolingKWh: Round(deliveredCooling),
            CompressorElectricityKWh: Round(compressorElectricity),
            AuxiliaryElectricityKWh: Round(request.AuxiliaryEnergyKWh),
            FinalCoolingElectricityKWh: Round(finalElectricity),
            SeasonalCop: Round(request.SeasonalCop),
            DistributionLossKWh: Round(deliveredCooling * (1 - request.DistributionEfficiency)),
            EmissionLossKWh: Round(deliveredCooling * request.DistributionEfficiency * (1 - request.EmissionEfficiency))));
    }

    private static double Round(double value) =>
        Math.Round(Math.Max(0, value), 2, MidpointRounding.AwayFromZero);
}
