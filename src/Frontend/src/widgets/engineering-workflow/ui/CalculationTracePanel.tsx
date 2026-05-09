import AccountTreeIcon from "@mui/icons-material/AccountTree";
import ExpandMoreIcon from "@mui/icons-material/ExpandMore";
import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Alert,
  Button,
  Chip,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Typography,
} from "@mui/material";
import { useState } from "react";
import type {
  WorkflowCalculationTraceSummary,
  WorkflowTraceDetailLevel,
} from "@/entities/engineering-workflow/types";
import { DataCard } from "@/shared/ui/DataCard";
import { EmptyState } from "@/shared/ui/EmptyState";

interface CalculationTracePanelProps {
  trace: WorkflowCalculationTraceSummary | undefined;
  onDetailLevelChange?: (level: WorkflowTraceDetailLevel) => Promise<void>;
}

export function CalculationTracePanel({ trace, onDetailLevelChange }: CalculationTracePanelProps): JSX.Element {
  const [detailLevel, setDetailLevel] = useState<WorkflowTraceDetailLevel>(trace?.detailLevel ?? "Summary");

  const changeLevel = async (next: WorkflowTraceDetailLevel) => {
    setDetailLevel(next);
    if (onDetailLevelChange) {
      await onDetailLevelChange(next);
    }
  };

  return (
    <DataCard>
      <Stack spacing={2}>
        <Stack direction={{ xs: "column", sm: "row" }} spacing={2} alignItems={{ xs: "flex-start", sm: "center" }} justifyContent="space-between">
          <Stack direction="row" spacing={1} alignItems="center">
            <AccountTreeIcon color="primary" fontSize="small" />
            <Typography variant="h6">Calculation trace summary</Typography>
          </Stack>
          <FormControl size="small" sx={{ minWidth: 180 }}>
            <InputLabel>Detail level</InputLabel>
            <Select
              label="Detail level"
              value={detailLevel}
              onChange={(event) => void changeLevel(event.target.value as WorkflowTraceDetailLevel)}
            >
              <MenuItem value="Summary">Summary</MenuItem>
              <MenuItem value="Standard">Standard</MenuItem>
              <MenuItem value="Detailed">Detailed</MenuItem>
            </Select>
          </FormControl>
        </Stack>

        {!trace ? (
          <EmptyState
            title="Trace summary is unavailable"
            description="This workflow run did not receive a trace document. Keep using diagnostics and report summary."
          />
        ) : (
          <>
            <Stack direction="row" spacing={1} flexWrap="wrap">
              {(trace.modules ?? []).map((module) => (
                <Chip key={module} label={module} size="small" variant="outlined" sx={{ mb: 1 }} />
              ))}
            </Stack>
            {trace.warnings.length > 0 ? (
              <Alert severity="warning">
                {trace.warnings.length} trace warning(s) in compact preview mode.
              </Alert>
            ) : null}

            {trace.steps
              .slice()
              .sort((left, right) => left.sequence - right.sequence)
              .map((step) => (
                <Accordion key={step.stepId} disableGutters>
                  <AccordionSummary expandIcon={<ExpandMoreIcon />}>
                    <Stack direction={{ xs: "column", sm: "row" }} spacing={1}>
                      <Typography variant="subtitle2">{step.sequence}. {step.stepName}</Typography>
                      <Chip size="small" label={step.moduleKind} variant="outlined" />
                      <Chip size="small" label={`${step.diagnosticsCount} diagnostics`} />
                    </Stack>
                  </AccordionSummary>
                  <AccordionDetails>
                    <Stack spacing={1}>
                      {step.assumptions.length > 0 ? (
                        <>
                          <Typography variant="body2" sx={{ fontWeight: 600 }}>Assumptions</Typography>
                          {step.assumptions.map((assumption) => (
                            <Typography key={assumption} variant="body2">- {assumption}</Typography>
                          ))}
                        </>
                      ) : null}
                      {step.warnings.length > 0 ? (
                        <>
                          <Typography variant="body2" sx={{ fontWeight: 600 }}>Warnings</Typography>
                          {step.warnings.map((warning) => (
                            <Typography key={warning} variant="body2">- {warning}</Typography>
                          ))}
                        </>
                      ) : null}
                    </Stack>
                  </AccordionDetails>
                </Accordion>
              ))}

            <Button size="small" variant="text" disabled>
              Detailed JSON endpoint pending backend wiring
            </Button>
          </>
        )}
      </Stack>
    </DataCard>
  );
}
