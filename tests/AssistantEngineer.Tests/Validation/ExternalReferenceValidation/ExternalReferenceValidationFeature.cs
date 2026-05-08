namespace AssistantEngineer.Tests.Validation.ExternalReferenceValidation;

public sealed record ExternalReferenceValidationFeature(
    string Code,
    string Name,
    ReferenceFeatureStatus ReferenceStatus,
    AssistantEngineerFeatureStatus AssistantEngineerStatus,
    ExternalReferenceValidationPriority Priority,
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
    ExternalReferenceCovered,
    Covered,
    OutOfScope
}

public enum ExternalReferenceValidationPriority
{
    P0,
    P1,
    P2,
    P3
}
