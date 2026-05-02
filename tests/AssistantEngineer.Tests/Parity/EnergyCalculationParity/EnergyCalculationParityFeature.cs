namespace AssistantEngineer.Tests.Parity.EnergyCalculationParity;

public sealed record EnergyCalculationParityFeature(
    string Code,
    string Name,
    ReferenceFeatureStatus ReferenceStatus,
    AssistantEngineerFeatureStatus AssistantEngineerStatus,
    EnergyCalculationParityPriority Priority,
    string AssistantEngineerArea,
    string Notes);

public enum ReferenceFeatureStatus
{
    Implemented,
    NotImplemented
}

public enum AssistantEngineerFeatureStatus
{
    NotStarted,
    Partial,
    InternalDeterministicTested,
    BenchmarkCompared,
    ExternalParityCovered,
    Covered,
    OutOfScope
}

public enum EnergyCalculationParityPriority
{
    P0,
    P1,
    P2,
    P3
}
