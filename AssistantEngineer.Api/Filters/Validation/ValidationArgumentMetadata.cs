namespace AssistantEngineer.Api.Filters.Validation;

internal sealed record ValidationArgumentMetadata(
    Type ValidatorType,
    Type ValidationContextType);