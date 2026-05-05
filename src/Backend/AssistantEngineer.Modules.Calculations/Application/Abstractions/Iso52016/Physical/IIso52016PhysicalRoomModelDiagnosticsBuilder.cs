using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016.Physical;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016.Physical;

/// <summary>
/// Builds diagnostics for the ISO52016-inspired physical room model translation to Matrix solver input.
/// </summary>
public interface IIso52016PhysicalRoomModelDiagnosticsBuilder
{
    Result<Iso52016PhysicalRoomModelDiagnosticsProfile> Build(
        Iso52016PhysicalRoomModelRequest request);
}
