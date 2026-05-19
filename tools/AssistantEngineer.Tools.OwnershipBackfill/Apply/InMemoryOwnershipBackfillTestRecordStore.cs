namespace AssistantEngineer.Tools.OwnershipBackfill.Apply;

public sealed class InMemoryOwnershipBackfillTestRecordStore : IOwnershipBackfillTestRecordStore
{
    private readonly Dictionary<string, OwnershipBackfillTestRecordState> records = new(StringComparer.Ordinal);

    public InMemoryOwnershipBackfillTestRecordStore(IEnumerable<OwnershipBackfillTestRecordState>? seed = null)
    {
        if (seed is null)
            return;

        foreach (var item in seed)
            UpsertRecord(item);
    }

    public bool TryGetRecord(string recordType, string recordId, out OwnershipBackfillTestRecordState state)
    {
        return records.TryGetValue(BuildKey(recordType, recordId), out state!);
    }

    public void UpsertRecord(OwnershipBackfillTestRecordState state)
    {
        records[BuildKey(state.RecordType, state.RecordId)] = state;
    }

    public IReadOnlyList<OwnershipBackfillTestRecordState> ListRecords()
    {
        return records.Values
            .OrderBy(item => item.RecordType, StringComparer.Ordinal)
            .ThenBy(item => item.RecordId, StringComparer.Ordinal)
            .ToArray();
    }

    private static string BuildKey(string recordType, string recordId) => $"{recordType}:{recordId}";
}

