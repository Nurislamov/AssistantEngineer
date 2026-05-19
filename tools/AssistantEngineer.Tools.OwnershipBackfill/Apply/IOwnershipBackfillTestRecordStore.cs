namespace AssistantEngineer.Tools.OwnershipBackfill.Apply;

public interface IOwnershipBackfillTestRecordStore
{
    bool TryGetRecord(string recordType, string recordId, out OwnershipBackfillTestRecordState state);
    void UpsertRecord(OwnershipBackfillTestRecordState state);
    IReadOnlyList<OwnershipBackfillTestRecordState> ListRecords();
}

