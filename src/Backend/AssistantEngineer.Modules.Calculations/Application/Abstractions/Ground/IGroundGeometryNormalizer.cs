using AssistantEngineer.Modules.Calculations.Application.Contracts.Ground;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Ground;

public interface IGroundGeometryNormalizer
{
    GroundContactGeometry Normalize(
        GroundContactKind contactKind,
        GroundContactGeometry geometry);
}
