using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Trace;

public interface ICalculationTraceSanitizer
{
    CalculationTraceDocument Sanitize(
        CalculationTraceDocument trace,
        CalculationTraceDetailLevel detailLevel = CalculationTraceDetailLevel.Standard,
        int maxCollectionItems = 24);
}
