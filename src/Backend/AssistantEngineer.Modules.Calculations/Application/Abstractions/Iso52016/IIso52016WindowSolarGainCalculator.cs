using AssistantEngineer.Modules.Calculations.Application.Contracts.Iso52016;
using AssistantEngineer.SharedKernel.Primitives;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Iso52016;

public interface IIso52016WindowSolarGainCalculator
{
    Result<Iso52016WindowSolarGainResult> Calculate(
        Iso52016WindowSolarGainRequest request);
}