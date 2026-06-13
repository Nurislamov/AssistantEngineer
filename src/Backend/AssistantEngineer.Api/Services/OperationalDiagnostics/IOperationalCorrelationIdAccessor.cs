namespace AssistantEngineer.Api.Services.OperationalDiagnostics;

public interface IOperationalCorrelationIdAccessor
{
    string? CorrelationId { get; set; }
}
