import { Alert, Chip, Stack, Typography } from "@mui/material";
import type { EngineeringAssumptionsSummaryViewModel } from "@/entities/engineering-workflow/model/engineeringWorkflowTrust";
import { DataCard } from "@/shared/ui/DataCard";

interface EngineeringAssumptionsSummaryPanelProps {
  summary: EngineeringAssumptionsSummaryViewModel;
}

export function EngineeringAssumptionsSummaryPanel({
  summary,
}: EngineeringAssumptionsSummaryPanelProps): JSX.Element {
  return (
    <DataCard compact>
      <Stack spacing={1.5}>
        <Typography variant="subtitle1">Assumptions summary</Typography>

        <Stack direction="row" spacing={1} flexWrap="wrap">
          <Chip size="small" label={`Total: ${summary.totalCount}`} />
          <Chip size="small" variant="outlined" label={`Active defaults: ${summary.activeDefaultCount}`} />
          <Chip size="small" variant="outlined" label={`Validation-only: ${summary.validationOnlyCount}`} />
          <Chip size="small" variant="outlined" label={`Unknown needs audit: ${summary.unknownNeedsAuditCount}`} />
        </Stack>

        {!summary.available ? (
          <Alert severity="info">
            Assumptions registry foundation exists, but direct workflow API mapping is not connected in this step.
          </Alert>
        ) : null}

        <Typography variant="caption" color="text.secondary">
          Assumptions registry documents engineering defaults and fixture assumptions.
        </Typography>
      </Stack>
    </DataCard>
  );
}
