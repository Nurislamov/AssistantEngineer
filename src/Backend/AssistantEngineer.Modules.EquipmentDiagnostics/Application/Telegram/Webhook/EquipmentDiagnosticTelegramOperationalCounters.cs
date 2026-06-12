namespace AssistantEngineer.Modules.EquipmentDiagnostics.Application.Telegram.Webhook;

public sealed class EquipmentDiagnosticTelegramOperationalCounters
{
    private long _updatesReceived;
    private long _updatesProcessed;
    private long _updatesIgnored;
    private long _updatesRejectedUnauthorized;
    private long _updatesRejectedSecret;
    private long _invalidUpdates;
    private long _outboundSendFailures;

    public EquipmentDiagnosticTelegramOperationalCounterSnapshot GetSnapshot() =>
        new(
            UpdatesReceived: Interlocked.Read(ref _updatesReceived),
            UpdatesProcessed: Interlocked.Read(ref _updatesProcessed),
            UpdatesIgnored: Interlocked.Read(ref _updatesIgnored),
            UpdatesRejectedUnauthorized: Interlocked.Read(ref _updatesRejectedUnauthorized),
            UpdatesRejectedSecret: Interlocked.Read(ref _updatesRejectedSecret),
            InvalidUpdates: Interlocked.Read(ref _invalidUpdates),
            OutboundSendFailures: Interlocked.Read(ref _outboundSendFailures));

    internal void RecordReceived() => Interlocked.Increment(ref _updatesReceived);
    internal void RecordProcessed() => Interlocked.Increment(ref _updatesProcessed);
    internal void RecordIgnored() => Interlocked.Increment(ref _updatesIgnored);
    internal void RecordRejectedUnauthorized() => Interlocked.Increment(ref _updatesRejectedUnauthorized);
    internal void RecordRejectedSecret() => Interlocked.Increment(ref _updatesRejectedSecret);
    internal void RecordInvalidUpdate() => Interlocked.Increment(ref _invalidUpdates);
    internal void RecordOutboundSendFailure() => Interlocked.Increment(ref _outboundSendFailures);
}

public sealed record EquipmentDiagnosticTelegramOperationalCounterSnapshot(
    long UpdatesReceived,
    long UpdatesProcessed,
    long UpdatesIgnored,
    long UpdatesRejectedUnauthorized,
    long UpdatesRejectedSecret,
    long InvalidUpdates,
    long OutboundSendFailures);
