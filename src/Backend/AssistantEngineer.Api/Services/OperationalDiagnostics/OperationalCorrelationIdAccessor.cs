using System.Threading;

namespace AssistantEngineer.Api.Services.OperationalDiagnostics;

public sealed class OperationalCorrelationIdAccessor : IOperationalCorrelationIdAccessor
{
    private static readonly AsyncLocal<string?> Current = new();

    public string? CorrelationId
    {
        get => Current.Value;
        set => Current.Value = value;
    }
}
