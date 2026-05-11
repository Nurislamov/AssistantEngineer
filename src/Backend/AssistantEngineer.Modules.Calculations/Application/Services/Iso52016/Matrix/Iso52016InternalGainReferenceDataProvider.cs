using AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Matrix;
using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Services.Iso52016.Matrix;

public sealed class Iso52016InternalGainReferenceDataProvider : ISo52016InternalGainReferenceDataProvider
{
    private const string SourceNote = "Seed data for ISO 52016-style sensible internal gains. Replace with national annex, project schedule or verified reference table when available.";

    private static readonly IReadOnlyList<Iso52016InternalGainReferenceData> Items =
    [
        new("Residential", 0.04, 70.0, 4.0, 3.0, 0.50, 0.50, SourceNote),
        new("Office", 0.10, 75.0, 9.0, 12.0, 0.55, 0.45, SourceNote),
        new("HotelGuestRoom", 0.05, 70.0, 7.0, 4.0, 0.50, 0.50, SourceNote),
        new("HotelLobby", 0.20, 75.0, 10.0, 8.0, 0.55, 0.45, SourceNote),
        new("Restaurant", 0.80, 80.0, 12.0, 18.0, 0.60, 0.40, SourceNote),
        new("Retail", 0.20, 75.0, 14.0, 8.0, 0.55, 0.45, SourceNote),
        new("School", 0.40, 75.0, 9.0, 7.0, 0.55, 0.45, SourceNote),
        new("Healthcare", 0.12, 75.0, 10.0, 15.0, 0.55, 0.45, SourceNote),
        new("Storage", 0.02, 70.0, 3.0, 2.0, 0.50, 0.50, SourceNote),
        new("TechnicalRoom", 0.01, 70.0, 4.0, 20.0, 0.70, 0.30, SourceNote)
    ];

    public IReadOnlyList<Iso52016InternalGainReferenceData> GetAll() => Items;

    public Result<Iso52016InternalGainReferenceData> GetByUseType(
        string useType)
    {
        if (string.IsNullOrWhiteSpace(useType))
            return Result<Iso52016InternalGainReferenceData>.Validation("Internal gain use type is required.");

        var item = Items.FirstOrDefault(reference =>
            string.Equals(reference.UseType, useType, StringComparison.OrdinalIgnoreCase));

        return item is null
            ? Result<Iso52016InternalGainReferenceData>.NotFound($"Internal gain reference data for use type '{useType}' was not found.")
            : Result<Iso52016InternalGainReferenceData>.Success(item);
    }

    public Result<Iso52016InternalGainCalculationResult> CalculatePeakSensibleGain(
        string useType,
        double floorAreaM2,
        double occupancyFactor = 1.0,
        double lightingFactor = 1.0,
        double equipmentFactor = 1.0)
    {
        if (floorAreaM2 <= 0)
            return Result<Iso52016InternalGainCalculationResult>.Validation("Floor area for internal gains must be greater than zero.");

        if (occupancyFactor < 0 || lightingFactor < 0 || equipmentFactor < 0)
            return Result<Iso52016InternalGainCalculationResult>.Validation("Internal gain schedule factors cannot be negative.");

        var referenceResult = GetByUseType(useType);

        if (referenceResult.IsFailure)
            return Result<Iso52016InternalGainCalculationResult>.Failure(referenceResult);

        var reference = referenceResult.Value;
        var occupantGainW = floorAreaM2 * reference.OccupantDensityPersonPerM2 * reference.SensibleHeatPerPersonW * occupancyFactor;
        var lightingGainW = floorAreaM2 * reference.LightingPowerDensityWPerM2 * lightingFactor;
        var equipmentGainW = floorAreaM2 * reference.EquipmentPowerDensityWPerM2 * equipmentFactor;
        var totalGainW = occupantGainW + lightingGainW + equipmentGainW;

        return Result<Iso52016InternalGainCalculationResult>.Success(
            new Iso52016InternalGainCalculationResult(
                UseType: reference.UseType,
                FloorAreaM2: floorAreaM2,
                OccupantGainW: occupantGainW,
                LightingGainW: lightingGainW,
                EquipmentGainW: equipmentGainW,
                TotalSensibleGainW: totalGainW,
                ConvectiveGainW: totalGainW * reference.ConvectiveFraction,
                RadiativeGainW: totalGainW * reference.RadiativeFraction));
    }
}