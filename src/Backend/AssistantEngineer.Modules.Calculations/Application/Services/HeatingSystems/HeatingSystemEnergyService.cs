using AssistantEngineer.Modules.Calculations.Application.Contracts.HeatingSystems;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.SystemEnergy;
using AssistantEngineer.Modules.Calculations.Application.Services.SystemEnergy;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.HeatingSystems;

public sealed class HeatingSystemEnergyService
{
    private readonly SystemEnergyEngine _systemEnergy;

    public HeatingSystemEnergyService(SystemEnergyEngine? systemEnergy = null)
    {
        _systemEnergy = systemEnergy ?? new SystemEnergyEngine();
    }

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
        var systemEnergy = _systemEnergy.Calculate(new SystemEnergyInput(
            UsefulHeatingEnergyKWh: energyNeed.AnnualHeatingDemandKWh,
            HeatingEfficiency: totalEfficiency,
            DiagnosticsContext: $"Building {energyNeed.BuildingId} heating system energy"));
        if (systemEnergy.IsFailure)
            return Result<HeatingSystemEnergyResult>.Failure(systemEnergy);

        var finalEnergy = systemEnergy.Value.FinalHeatingEnergyKWh;

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
