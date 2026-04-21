using AssistantEngineer.Modules.Calculations.Application.Services.Iso52016;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.CoolingSystems;

public sealed class CoolingSystemEnergyService
{
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

        var deliveredCooling = energyNeed.AnnualCoolingDemandKWh /
            (request.DistributionEfficiency * request.EmissionEfficiency);
        var compressorElectricity = deliveredCooling / request.SeasonalCop;
        var finalElectricity = compressorElectricity + request.AuxiliaryEnergyKWh;

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

public sealed class CoolingSystemEnergyRequest
{
    public double SeasonalCop { get; set; } = 3.2;
    public double DistributionEfficiency { get; set; } = 0.95;
    public double EmissionEfficiency { get; set; } = 0.98;
    public double AuxiliaryEnergyKWh { get; set; }
}

public sealed record CoolingSystemEnergyResult(
    double UsefulCoolingDemandKWh,
    double DeliveredCoolingKWh,
    double CompressorElectricityKWh,
    double AuxiliaryElectricityKWh,
    double FinalCoolingElectricityKWh,
    double SeasonalCop,
    double DistributionLossKWh,
    double EmissionLossKWh);
