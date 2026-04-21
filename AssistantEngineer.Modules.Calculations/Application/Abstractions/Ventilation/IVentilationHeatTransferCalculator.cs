using AssistantEngineer.Modules.Buildings.Domain.Entities;
using AssistantEngineer.Modules.Calculations.Application.Models.Ventilation;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;

public interface IVentilationHeatTransferCalculator
{
    double Calculate(Room room, VentilationCalculationContext context);
    double CalculateMechanical(Room room, VentilationCalculationContext context);
    double CalculateInfiltration(Room room, VentilationCalculationContext context);
}