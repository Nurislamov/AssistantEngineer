namespace AssistantEngineer.Tools.OwnershipBackfill.Gates;

public interface IOwnershipBackfillGateResultWriter
{
    Task WriteAsync(
        OwnershipBackfillGateResult result,
        string outputDirectory,
        CancellationToken cancellationToken = default);
}
