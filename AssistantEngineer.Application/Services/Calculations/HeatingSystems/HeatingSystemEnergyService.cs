using AssistantEngineer.Application.Services.Calculations.Iso52016;
using AssistantEngineer.Domain.Primitives;

namespace AssistantEngineer.Application.Services.Calculations.HeatingSystems;

public sealed class HeatingSystemEnergyService
{
    public Result<HeatingSystemEnergyResult> Calculate(
        Iso52016AnnualEnergyNeedResult energyNeed,
        HeatingSystemEnergyRequest request)
    {
        if (request.GenerationEfficiency <= 0 || request.GenerationEfficiency > 1.5)
            return Result<HeatingSystemEnergyResult>.Validation("Generation efficiency must be between 0 and 1.5.");

        if (request.DistributionEfficiency <= 0 || request.DistributionEfficiency > 1)
            return Result<HeatingSystemEnergyResult>.Validation("Distribution efficiency must be between 0 and 1.");

        if (request.EmissionEfficiency <= 0 || request.EmissionEfficiency > 1)
            return Result<HeatingSystemEnergyResult>.Validation("Emission efficiency must be between 0 and 1.");

        var totalEfficiency = request.GenerationEfficiency *
            request.DistributionEfficiency *
            request.EmissionEfficiency;
        var finalEnergy = energyNeed.AnnualHeatingDemandKWh / totalEfficiency;

        return Result<HeatingSystemEnergyResult>.Success(new HeatingSystemEnergyResult(
            UsefulHeatingDemandKWh: Round(energyNeed.AnnualHeatingDemandKWh),
            FinalHeatingEnergyKWh: Round(finalEnergy),
            TotalSystemEfficiency: Round(totalEfficiency),
            GenerationLossKWh: Round(finalEnergy * (1 - request.GenerationEfficiency)),
            DistributionLossKWh: Round(finalEnergy * request.GenerationEfficiency * (1 - request.DistributionEfficiency)),
            EmissionLossKWh: Round(finalEnergy * request.GenerationEfficiency * request.DistributionEfficiency * (1 - request.EmissionEfficiency))));
    }

    private static double Round(double value) =>
        Math.Round(Math.Max(0, value), 2, MidpointRounding.AwayFromZero);
}

public sealed class HeatingSystemEnergyRequest
{
    public double GenerationEfficiency { get; set; } = 0.92;
    public double DistributionEfficiency { get; set; } = 0.95;
    public double EmissionEfficiency { get; set; } = 0.97;
}

public sealed record HeatingSystemEnergyResult(
    double UsefulHeatingDemandKWh,
    double FinalHeatingEnergyKWh,
    double TotalSystemEfficiency,
    double GenerationLossKWh,
    double DistributionLossKWh,
    double EmissionLossKWh);
