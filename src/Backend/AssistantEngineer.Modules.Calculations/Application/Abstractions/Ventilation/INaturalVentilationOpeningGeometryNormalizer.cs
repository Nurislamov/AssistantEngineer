using AssistantEngineer.Modules.Calculations.Application.Contracts.Ventilation;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Ventilation;

public interface INaturalVentilationOpeningGeometryNormalizer
{
    NaturalVentilationOpeningGeometry Normalize(NaturalVentilationOpeningGeometry opening);
}
