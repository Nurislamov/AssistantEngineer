using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Matrix;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Matrix;

public interface ISo52016InternalGainReferenceDataProvider
{
    IReadOnlyList<Iso52016InternalGainReferenceData> GetAll();

    Result<Iso52016InternalGainReferenceData> GetByUseType(
        string useType);

    Result<Iso52016InternalGainCalculationResult> CalculatePeakSensibleGain(
        string useType,
        double floorAreaM2,
        double occupancyFactor = 1.0,
        double lightingFactor = 1.0,
        double equipmentFactor = 1.0);
}