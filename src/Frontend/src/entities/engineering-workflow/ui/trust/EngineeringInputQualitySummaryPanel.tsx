import {
  Alert,
  Chip,
  Divider,
  Stack,
  Typography,
} from "@mui/material";
import type { EngineeringInputQualitySummaryViewModel } from "@/entities/engineering-workflow/model/engineeringWorkflowTrust";
import {
  getSeverityLabel,
  getSeverityTone,
} from "@/entities/engineering-workflow/model/engineeringWorkflowTrustViewModel";
import { DataCard } from "@/shared/ui/DataCard";
import { EmptyState } from "@/shared/ui/EmptyState";

interface EngineeringInputQualitySummaryPanelProps {
  summary: EngineeringInputQualitySummaryViewModel;
}

function getReadinessLabel(summary: EngineeringInputQualitySummaryViewModel): string {
  if (summary.diagnosticCount === 0) {
    return "Unknown";
  }

  if (!summary.isCalculationReady) {
    return "Not ready";
  }

  if (summary.hasWarnings) {
    return "Ready with warnings";
  }

  return "Ready";
}

function getReadinessTone(summary: EngineeringInputQualitySummaryViewModel): "default" | "success" | "warning" | "error" {
  if (summary.diagnosticCount === 0) {
    return "default";
  }

  if (!summary.isCalculationReady) {
    return "error";
  }

  if (summary.hasWarnings) {
    return "warning";
  }

  return "success";
}

export function EngineeringInputQualitySummaryPanel({
  summary,
}: EngineeringInputQualitySummaryPanelProps): JSX.Element {
  const readinessLabel = getReadinessLabel(summary);

  return (
    <DataCard compact>
      <Stack spacing={1.5}>
        <Stack direction="row" justifyContent="space-between" alignItems="center" spacing={1}>
          <Typography variant="subtitle1">Input quality summary</Typography>
          <Chip
            label={readinessLabel}
            size="small"
            color={getReadinessTone(summary)}
            variant={readinessLabel === "Unknown" ? "outlined" : "filled"}
          />
        </Stack>

        <Stack direction="row" spacing={1} flexWrap="wrap">
          <Chip size="small" variant="outlined" label={`Diagnostics: ${summary.diagnosticCount}`} />
          <Chip
            size="small"
            color={getSeverityTone(summary.highestSeverity)}
            label={`Highest severity: ${getSeverityLabel(summary.highestSeverity)}`}
            variant={summary.highestSeverity === "none" ? "outlined" : "filled"}
          />
        </Stack>

        <Typography variant="caption" color="text.secondary">
          Calculation ready means no blocking input-quality issue was detected by available checks. It does not mean certified validation or full standard compliance.
        </Typography>

        {summary.diagnostics.length === 0 ? (
          <EmptyState
            title="No input quality diagnostics"
            description="Input quality endpoint integration is pending for this workflow context. Status is shown as Unknown."
          />
        ) : (
          <Stack spacing={1} divider={<Divider flexItem />}>
            {summary.diagnostics.slice(0, 4).map((diagnostic) => (
              <Stack key={`${diagnostic.code}-${diagnostic.message}`} spacing={0.5}>
                <Stack direction="row" spacing={1} alignItems="center">
                  <Chip size="small" label={diagnostic.code} variant="outlined" />
                  <Chip size="small" label={diagnostic.category} />
                </Stack>
                <Typography variant="body2">{diagnostic.message}</Typography>
                {diagnostic.field ? (
                  <Typography variant="caption" color="text.secondary">
                    Field: {diagnostic.field}
                  </Typography>
                ) : null}
                {diagnostic.recommendation ? (
                  <Alert severity="info" sx={{ mt: 0.5 }}>
                    Recommendation: {diagnostic.recommendation}
                  </Alert>
                ) : null}
              </Stack>
            ))}
          </Stack>
        )}
      </Stack>
    </DataCard>
  );
}
