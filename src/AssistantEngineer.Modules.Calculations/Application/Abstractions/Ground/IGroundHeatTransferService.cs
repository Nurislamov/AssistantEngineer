using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Calculations.Application.Models.Ground;
using AssistantEngineer.Modules.Calculations.Application.Models.ReferenceData;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Ground;

public interface IGroundHeatTransferService
{
    GroundBoundaryCondition CalculateBoundaryCondition(
        Room room,
        BuildingEnvelopeDefaults envelopeDefaults);
}