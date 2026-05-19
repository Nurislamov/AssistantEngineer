using AssistantEngineer.Tools.OwnershipBackfill.Cli;
using AssistantEngineer.Tools.OwnershipBackfill.Apply;
using AssistantEngineer.Tools.OwnershipBackfill.Evidence;
using AssistantEngineer.Tools.OwnershipBackfill.Gates;
using AssistantEngineer.Tools.OwnershipBackfill.Plan;
using AssistantEngineer.Tools.OwnershipBackfill.Signoff;
using AssistantEngineer.Tools.OwnershipBackfill.Scanning;

namespace AssistantEngineer.Tools.OwnershipBackfill;

internal static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var cli = new OwnershipBackfillCli(
            new OwnershipBackfillCommandLineParser(),
            new OwnershipBackfillEvidenceWriter(),
            new NoDataOwnershipBackfillDryRunScanner(),
            new DatabaseOwnershipBackfillDryRunScanner(),
            new OwnershipBackfillEvidenceLoader(),
            new OwnershipBackfillEvidenceGateEvaluator(),
            new OwnershipBackfillGateResultWriter(),
            new OwnershipBackfillApplyPreconditionValidator(),
            new OwnershipBackfillApplyPlanGenerator(),
            new OwnershipBackfillApplyPlanWriter(),
            new OwnershipBackfillPlanSignoffValidator(),
            new OwnershipBackfillPlanSignoffWriter());

        return await cli.ExecuteAsync(args, Console.Out, Console.Error, CancellationToken.None);
    }
}
