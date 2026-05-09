using AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;

namespace AssistantEngineer.Modules.Calculations.Application.Abstractions.Trace;

public interface ICalculationTraceJsonExporter
{
    string Export(CalculationTraceDocument trace, bool indented = false);
}
