import { Alert, Chip, Stack, Typography } from "@mui/material";
import type { EngineeringTraceSummaryViewModel } from "@/entities/engineering-workflow/model/engineeringWorkflowTrust";
import { DataCard } from "@/shared/ui/DataCard";
import { EmptyState } from "@/shared/ui/EmptyState";

interface EngineeringCalculationTraceSummaryPanelProps {
  summary: EngineeringTraceSummaryViewModel;
}

export function EngineeringCalculationTraceSummaryPanel({
  summary,
}: EngineeringCalculationTraceSummaryPanelProps): JSX.Element {
  return (
    <DataCard compact>
      <Stack spacing={1.5}>
        <Typography variant="subtitle1">Calculation trace summary</Typography>

        {!summary.available ? (
          <EmptyState
            title="Trace is unavailable"
            description="Calculation trace has not been generated for this workflow step yet."
          />
        ) : (
          <>
            <Stack direction="row" spacing={1} flexWrap="wrap">
              <Chip size="small" label={`Trace ID: ${summary.traceId ?? "n/a"}`} />
              <Chip size="small" variant="outlined" label={`Sections: ${summary.sectionCount}`} />
              <Chip size="small" variant="outlined" label={`Assumptions: ${summary.assumptionCount}`} />
              <Chip size="small" variant="outlined" label={`Excluded effects: ${summary.excludedEffectCount}`} />
              <Chip size="small" variant="outlined" label={`Diagnostic refs: ${summary.diagnosticReferenceCount}`} />
            </Stack>
            <Alert severity="info">
              Detailed trace viewer is planned. Current panel shows trace availability and summary metadata.
            </Alert>
          </>
        )}
      </Stack>
    </DataCard>
  );
}
