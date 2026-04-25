using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Calculations.Application.Abstractions.Performance;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Performance;
using AssistantEngineer.Modules.Calculations.Application.Services.CoolingSystems;
using AssistantEngineer.Modules.Calculations.Application.Services.DomesticHotWater;
using AssistantEngineer.Modules.Calculations.Application.Services.HeatingSystems;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Performance;

public sealed class BuildingEnergyPerformanceSummaryService
{
    private readonly HeatingSystemEnergyService _heatingSystemEnergy;
    private readonly CoolingSystemEnergyService _coolingSystemEnergy;
    private readonly DomesticHotWaterDemandService _domesticHotWater;
    private readonly IEnergyCarrierFactorProvider _carrierFactors;

    public BuildingEnergyPerformanceSummaryService(
        HeatingSystemEnergyService heatingSystemEnergy,
        CoolingSystemEnergyService coolingSystemEnergy,
        DomesticHotWaterDemandService domesticHotWater,
        IEnergyCarrierFactorProvider carrierFactors)
    {
        _heatingSystemEnergy = heatingSystemEnergy;
        _coolingSystemEnergy = coolingSystemEnergy;
        _domesticHotWater = domesticHotWater;
        _carrierFactors = carrierFactors;
    }

    public Result<BuildingEnergyPerformanceSummary> Calculate(
        Building building,
        Iso52016AnnualEnergyNeedResult energyNeed,
        BuildingEnergyPerformanceRequest request)
    {
        var heating = _heatingSystemEnergy.Calculate(energyNeed, request.HeatingSystem);
        if (heating.IsFailure)
            return Result<BuildingEnergyPerformanceSummary>.Failure(heating);

        var cooling = _coolingSystemEnergy.Calculate(energyNeed, request.CoolingSystem);
        if (cooling.IsFailure)
            return Result<BuildingEnergyPerformanceSummary>.Failure(cooling);

        var endUses = new List<BuildingEnergyEndUseSummary>
        {
            CreateEndUse(
                BuildingEnergyEndUse.Heating,
                request.HeatingCarrier,
                usefulEnergyKWh: heating.Value.UsefulHeatingDemandKWh,
                finalEnergyKWh: heating.Value.FinalHeatingEnergyKWh,
                request.CarrierFactorOverrides),
            CreateEndUse(
                BuildingEnergyEndUse.Cooling,
                request.CoolingCarrier,
                usefulEnergyKWh: cooling.Value.UsefulCoolingDemandKWh,
                finalEnergyKWh: cooling.Value.FinalCoolingElectricityKWh,
                request.CarrierFactorOverrides)
        };

        if (request.IncludeDomesticHotWater)
        {
            if (request.DomesticHotWater is null)
                return Result<BuildingEnergyPerformanceSummary>.Validation("Domestic hot water request is required when DHW is included.");

            if (request.DomesticHotWaterSystem.GenerationEfficiency <= 0 ||
                request.DomesticHotWaterSystem.GenerationEfficiency > 1.5)
                return Result<BuildingEnergyPerformanceSummary>.Validation("DHW generation efficiency must be between 0 and 1.5.");

            var dhw = _domesticHotWater.Calculate(request.DomesticHotWater);
            if (dhw.IsFailure)
                return Result<BuildingEnergyPerformanceSummary>.Failure(dhw);

            var finalDhw = dhw.Value.AnnualEnergyKWh / request.DomesticHotWaterSystem.GenerationEfficiency;
            endUses.Add(CreateEndUse(
                BuildingEnergyEndUse.DomesticHotWater,
                request.DomesticHotWaterCarrier,
                usefulEnergyKWh: dhw.Value.AnnualEnergyKWh,
                finalEnergyKWh: finalDhw,
                request.CarrierFactorOverrides));
        }

        if (endUses.Any(endUse => endUse.HasInvalidCarrier))
        {
            var invalid = endUses.First(endUse => endUse.HasInvalidCarrier);
            return Result<BuildingEnergyPerformanceSummary>.Validation(
                $"Energy carrier '{invalid.Carrier}' is not supported.");
        }

        var floorArea = building.Floors.SelectMany(floor => floor.Rooms).Sum(room => room.Area.SquareMeters);
        var totalFinal = endUses.Sum(endUse => endUse.FinalEnergyKWh);
        var totalPrimary = endUses.Sum(endUse => endUse.PrimaryEnergyKWh);
        var totalCo2 = endUses.Sum(endUse => endUse.Co2Kg);
        var safeArea = floorArea > 0 ? floorArea : 0;

        return Result<BuildingEnergyPerformanceSummary>.Success(new BuildingEnergyPerformanceSummary(
            BuildingId: building.Id,
            BuildingName: building.Name,
            Year: energyNeed.Year,
            FloorAreaM2: Round(floorArea),
            EndUses: endUses,
            TotalUsefulEnergyKWh: Round(endUses.Sum(endUse => endUse.UsefulEnergyKWh)),
            TotalFinalEnergyKWh: Round(totalFinal),
            TotalPrimaryEnergyKWh: Round(totalPrimary),
            TotalCo2Kg: Round(totalCo2),
            FinalEnergyIntensityKWhPerM2Year: safeArea > 0 ? Round(totalFinal / safeArea) : 0,
            PrimaryEnergyIntensityKWhPerM2Year: safeArea > 0 ? Round(totalPrimary / safeArea) : 0,
            Co2IntensityKgPerM2Year: safeArea > 0 ? Round(totalCo2 / safeArea) : 0));
    }

    private BuildingEnergyEndUseSummary CreateEndUse(
        BuildingEnergyEndUse endUse,
        EnergyCarrierType carrier,
        double usefulEnergyKWh,
        double finalEnergyKWh,
        IReadOnlyDictionary<EnergyCarrierType, EnergyCarrierFactors>? overrides)
    {
        var factors = GetFactors(carrier, overrides);
        if (factors.IsFailure)
        {
            return new BuildingEnergyEndUseSummary(
                endUse,
                carrier,
                UsefulEnergyKWh: Round(usefulEnergyKWh),
                FinalEnergyKWh: Round(finalEnergyKWh),
                PrimaryEnergyFactor: 0,
                Co2KgPerKWh: 0,
                PrimaryEnergyKWh: 0,
                Co2Kg: 0,
                HasInvalidCarrier: true);
        }

        return new BuildingEnergyEndUseSummary(
            endUse,
            carrier,
            UsefulEnergyKWh: Round(usefulEnergyKWh),
            FinalEnergyKWh: Round(finalEnergyKWh),
            PrimaryEnergyFactor: Round(factors.Value.PrimaryEnergyFactor),
            Co2KgPerKWh: Round(factors.Value.Co2KgPerKWh),
            PrimaryEnergyKWh: Round(finalEnergyKWh * factors.Value.PrimaryEnergyFactor),
            Co2Kg: Round(finalEnergyKWh * factors.Value.Co2KgPerKWh),
            HasInvalidCarrier: false);
    }

    private Result<EnergyCarrierFactors> GetFactors(
        EnergyCarrierType carrier,
        IReadOnlyDictionary<EnergyCarrierType, EnergyCarrierFactors>? overrides)
    {
        if (overrides is not null && overrides.TryGetValue(carrier, out var factors))
        {
            if (factors.PrimaryEnergyFactor < 0 || factors.Co2KgPerKWh < 0)
                return Result<EnergyCarrierFactors>.Validation("Energy carrier factors cannot be negative.");

            return Result<EnergyCarrierFactors>.Success(factors);
        }

        return _carrierFactors.Get(carrier);
    }

    private static double Round(double value) =>
        Math.Round(Math.Max(0, value), 2, MidpointRounding.AwayFromZero);
}