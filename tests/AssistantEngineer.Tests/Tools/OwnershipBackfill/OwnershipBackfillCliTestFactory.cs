using AssistantEngineer.Tools.OwnershipBackfill.Apply;
using AssistantEngineer.Tools.OwnershipBackfill.Cli;
using AssistantEngineer.Tools.OwnershipBackfill.Evidence;
using AssistantEngineer.Tools.OwnershipBackfill.Gates;
using AssistantEngineer.Tools.OwnershipBackfill.Plan;
using AssistantEngineer.Tools.OwnershipBackfill.Scanning;
using AssistantEngineer.Tools.OwnershipBackfill.Signoff;

namespace AssistantEngineer.Tests.Tools.OwnershipBackfill;

internal static class OwnershipBackfillCliTestFactory
{
    public static OwnershipBackfillCli Create()
    {
        return new OwnershipBackfillCli(
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
    }
}

