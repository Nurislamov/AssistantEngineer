namespace AssistantEngineer.Api.Services.OperationalDiagnostics;

public interface IOperationalDiagnosticsService
{
    OperationalDiagnosticsSnapshot GetSnapshot();
}
