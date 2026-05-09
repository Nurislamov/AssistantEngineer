namespace AssistantEngineer.Modules.Calculations.Application.Contracts.Trace;

public enum CalculationTraceValueKind
{
    Input = 1,
    Output = 2,
    Intermediate = 3,
    Assumption = 4,
    Default = 5,
    Coefficient = 6,
    Formula = 7,
    Diagnostic = 8,
    Warning = 9,
    Error = 10
}
