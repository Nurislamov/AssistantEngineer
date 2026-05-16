import { Stack, Typography } from "@mui/material";
import type { EngineeringTrustOverviewViewModel } from "@/entities/engineering-workflow/model/engineeringWorkflowTrust";
import { EngineeringAssumptionsSummaryPanel } from "./EngineeringAssumptionsSummaryPanel";
import { EngineeringCalculationTraceSummaryPanel } from "./EngineeringCalculationTraceSummaryPanel";
import { EngineeringInputQualitySummaryPanel } from "./EngineeringInputQualitySummaryPanel";
import { EngineeringValidationReadinessPanel } from "./EngineeringValidationReadinessPanel";

interface EngineeringTrustOverviewPanelProps {
  model: EngineeringTrustOverviewViewModel;
}

export function EngineeringTrustOverviewPanel({
  model,
}: EngineeringTrustOverviewPanelProps): JSX.Element {
  return (
    <Stack spacing={1.5}>
      <Typography variant="h6">Engineering trust overview</Typography>
      <EngineeringInputQualitySummaryPanel summary={model.inputQuality} />
      <EngineeringCalculationTraceSummaryPanel summary={model.traceSummary} />
      <EngineeringAssumptionsSummaryPanel summary={model.assumptionsSummary} />
      <EngineeringValidationReadinessPanel readiness={model.validationReadiness} />
    </Stack>
  );
}
