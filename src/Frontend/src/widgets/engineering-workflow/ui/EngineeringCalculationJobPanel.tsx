import { Alert, Button, Chip, Stack, Table, TableBody, TableCell, TableHead, TableRow, Typography } from "@mui/material";
import type {
  EngineeringCalculationJobEvent,
  EngineeringCalculationJobResult,
} from "@/entities/engineering-workflow/types";
import { DataCard } from "@/shared/ui/DataCard";
import { EmptyState } from "@/shared/ui/EmptyState";

interface EngineeringCalculationJobPanelProps {
  currentJob: EngineeringCalculationJobResult | null;
  jobs: EngineeringCalculationJobResult[];
  events: EngineeringCalculationJobEvent[];
  onRefreshJob: (jobId: string) => void;
  onRefreshEvents: (jobId: string) => void;
  onCancelJob: (jobId: string) => void;
  onSelectJob: (jobId: string) => void;
}

export function EngineeringCalculationJobPanel({
  currentJob,
  jobs,
  events,
  onRefreshJob,
  onRefreshEvents,
  onCancelJob,
  onSelectJob,
}: EngineeringCalculationJobPanelProps): JSX.Element {
  return (
    <DataCard>
      <Stack spacing={1.5}>
        <Typography variant="h6">Calculation jobs</Typography>
        {currentJob ? (
          <Stack spacing={1}>
            <Alert severity={resolveSeverity(currentJob.status)}>
              Job ID: {currentJob.jobId}. Status: {currentJob.status}. Progress: {currentJob.progressPercent}%.
            </Alert>
            <Typography variant="body2">Current step: {currentJob.currentStep}</Typography>
            <Typography variant="body2">Scenario ID: {currentJob.scenarioId}</Typography>
            <Stack direction="row" spacing={1}>
              <Button size="small" variant="outlined" onClick={() => onRefreshJob(currentJob.jobId)}>
                Refresh status
              </Button>
              <Button size="small" variant="outlined" onClick={() => onRefreshEvents(currentJob.jobId)}>
                Refresh events
              </Button>
              <Button size="small" variant="outlined" color="warning" onClick={() => onCancelJob(currentJob.jobId)}>
                Cancel job
              </Button>
            </Stack>
          </Stack>
        ) : (
          <EmptyState
            title="No active job selected"
            description="Run calculation job to see lifecycle status, progress and events."
          />
        )}

        {jobs.length > 0 ? (
          <Table size="small">
            <TableHead>
              <TableRow>
                <TableCell>Job ID</TableCell>
                <TableCell>Status</TableCell>
                <TableCell>Progress</TableCell>
                <TableCell>Created</TableCell>
                <TableCell>Action</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {jobs.slice(0, 10).map((item) => (
                <TableRow key={item.jobId}>
                  <TableCell sx={{ fontFamily: "monospace", fontSize: 12 }}>{item.jobId}</TableCell>
                  <TableCell>
                    <Chip size="small" label={item.status} color={resolveChipColor(item.status)} />
                  </TableCell>
                  <TableCell>{item.progressPercent}%</TableCell>
                  <TableCell>{new Date(item.queuedAtUtc).toLocaleString()}</TableCell>
                  <TableCell>
                    <Button size="small" variant="text" onClick={() => onSelectJob(item.jobId)}>
                      Select
                    </Button>
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        ) : null}

        <Typography variant="body2" sx={{ fontWeight: 600 }}>
          Job events
        </Typography>
        {events.length === 0 ? (
          <Typography variant="body2" color="text.secondary">
            No job events yet.
          </Typography>
        ) : (
          <Stack spacing={0.5}>
            {events.slice(-10).map((item) => (
              <Typography key={item.eventId} variant="caption">
                {new Date(item.createdAtUtc).toLocaleTimeString()} · {item.status} · {item.message}
              </Typography>
            ))}
          </Stack>
        )}
      </Stack>
    </DataCard>
  );
}

function resolveSeverity(status: EngineeringCalculationJobResult["status"]): "success" | "info" | "warning" | "error" {
  if (status === "FailedExecution" || status === "FailedValidation") {
    return "error";
  }

  if (status === "CompletedWithWarnings" || status === "CancelRequested" || status === "Cancelled") {
    return "warning";
  }

  if (status === "Completed") {
    return "success";
  }

  return "info";
}

function resolveChipColor(status: EngineeringCalculationJobResult["status"]): "default" | "success" | "warning" | "error" {
  if (status === "FailedExecution" || status === "FailedValidation") {
    return "error";
  }

  if (status === "Completed") {
    return "success";
  }

  if (status === "CompletedWithWarnings" || status === "CancelRequested" || status === "Cancelled") {
    return "warning";
  }

  return "default";
}
