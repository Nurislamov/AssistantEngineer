import WarningAmberIcon from "@mui/icons-material/WarningAmber";
import {
  Alert,
  Box,
  Chip,
  Divider,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Typography,
} from "@mui/material";
import { useMemo, useState } from "react";
import type { WorkflowDiagnostic, WorkflowDiagnosticSeverity, WorkflowStepKind } from "@/entities/engineering-workflow/types";
import { DataCard } from "@/shared/ui/DataCard";
import { EmptyState } from "@/shared/ui/EmptyState";

interface WorkflowDiagnosticsPanelProps {
  diagnostics: WorkflowDiagnostic[];
  onSelectStep?: (step: WorkflowStepKind) => void;
}

const severityOrder: WorkflowDiagnosticSeverity[] = ["error", "warning", "assumption", "info"];

export function WorkflowDiagnosticsPanel({ diagnostics, onSelectStep }: WorkflowDiagnosticsPanelProps): JSX.Element {
  const [severityFilter, setSeverityFilter] = useState<"all" | WorkflowDiagnosticSeverity>("all");

  const filtered = useMemo(() => {
    const selected = severityFilter === "all"
      ? diagnostics
      : diagnostics.filter((item) => item.severity === severityFilter);

    return selected.slice().sort((left, right) => {
      const severityDelta = severityOrder.indexOf(left.severity) - severityOrder.indexOf(right.severity);
      if (severityDelta !== 0) {
        return severityDelta;
      }

      const stepDelta = left.sourceStep.localeCompare(right.sourceStep, "en-US");
      if (stepDelta !== 0) {
        return stepDelta;
      }

      const codeDelta = left.code.localeCompare(right.code, "en-US");
      if (codeDelta !== 0) {
        return codeDelta;
      }

      return left.message.localeCompare(right.message, "en-US");
    });
  }, [diagnostics, severityFilter]);

  return (
    <DataCard>
      <Stack spacing={2}>
        <Stack direction={{ xs: "column", sm: "row" }} alignItems={{ xs: "flex-start", sm: "center" }} justifyContent="space-between" spacing={2}>
          <Stack direction="row" spacing={1} alignItems="center">
            <WarningAmberIcon color="warning" fontSize="small" />
            <Typography variant="h6">Validation diagnostics</Typography>
          </Stack>
          <FormControl size="small" sx={{ minWidth: 190 }}>
            <InputLabel>Severity filter</InputLabel>
            <Select
              label="Severity filter"
              value={severityFilter}
              onChange={(event) => setSeverityFilter(event.target.value as "all" | WorkflowDiagnosticSeverity)}
            >
              <MenuItem value="all">All severities</MenuItem>
              <MenuItem value="error">Errors</MenuItem>
              <MenuItem value="warning">Warnings</MenuItem>
              <MenuItem value="assumption">Assumptions</MenuItem>
              <MenuItem value="info">Info</MenuItem>
            </Select>
          </FormControl>
        </Stack>

        {filtered.length === 0 ? (
          <EmptyState title="No diagnostics" description="Workflow diagnostics panel is empty for the selected filter." />
        ) : (
          <Stack divider={<Divider flexItem />} spacing={1}>
            {filtered.map((diagnostic) => (
              <Box
                key={`${diagnostic.sourceStep}-${diagnostic.code}-${diagnostic.message}`}
                sx={{ py: 1, cursor: onSelectStep ? "pointer" : "default" }}
                onClick={() => onSelectStep?.(diagnostic.sourceStep)}
              >
                <Stack direction={{ xs: "column", sm: "row" }} spacing={1} alignItems={{ xs: "flex-start", sm: "center" }}>
                  <Chip label={diagnostic.severity.toUpperCase()} size="small" color={chipColor(diagnostic.severity)} />
                  <Chip label={diagnostic.sourceStep} size="small" variant="outlined" />
                  <Typography variant="subtitle2">{diagnostic.code}</Typography>
                </Stack>
                <Typography variant="body2" sx={{ mt: 1 }}>{diagnostic.message}</Typography>
                {diagnostic.suggestedCorrection ? (
                  <Alert severity="info" sx={{ mt: 1 }}>
                    Suggested correction: {diagnostic.suggestedCorrection}
                  </Alert>
                ) : null}
                {diagnostic.targetField ? (
                  <Typography variant="caption" color="text.secondary">Target field: {diagnostic.targetField}</Typography>
                ) : null}
              </Box>
            ))}
          </Stack>
        )}
      </Stack>
    </DataCard>
  );
}

function chipColor(severity: WorkflowDiagnosticSeverity): "default" | "error" | "warning" | "info" {
  if (severity === "error") {
    return "error";
  }

  if (severity === "warning") {
    return "warning";
  }

  return "info";
}
