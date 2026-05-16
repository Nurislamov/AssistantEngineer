import PlayArrowIcon from "@mui/icons-material/PlayArrow";
import {
  Alert,
  Button,
  Chip,
  Divider,
  List,
  ListItemButton,
  ListItemText,
  Stack,
  Typography,
} from "@mui/material";
import { EngineeringTrustOverviewPanel } from "@/entities/engineering-workflow/ui/trust/EngineeringTrustOverviewPanel";
import type { WorkflowStepKind, WorkflowStepStatus } from "@/entities/engineering-workflow/types";
import { DataCard } from "@/shared/ui/DataCard";
import { EmptyState } from "@/shared/ui/EmptyState";
import { QueryState } from "@/shared/ui/QueryState";
import { useEngineeringWorkflowTrustOverview } from "../model/useEngineeringWorkflowTrustOverview";
import { useEngineeringWorkflowShell } from "../model/useEngineeringWorkflowShell";
import { stepLabel } from "../model/engineeringWorkflowShellViewModel";
import { CalculationTracePanel } from "./CalculationTracePanel";
import { EngineeringCalculationJobPanel } from "./EngineeringCalculationJobPanel";
import { EngineeringReportPreview } from "./EngineeringReportPreview";
import { EngineeringScenarioHistoryPanel } from "./EngineeringScenarioHistoryPanel";
import { EngineeringWorkflowStepContent } from "./EngineeringWorkflowStepContent";
import { WorkflowDiagnosticsPanel } from "./WorkflowDiagnosticsPanel";

interface EngineeringWorkflowShellProps {
  projectId: number;
  buildingId: number;
}

const orderedSteps: WorkflowStepKind[] = [
  "Project",
  "Building",
  "Zones",
  "Envelope",
  "WeatherSolar",
  "Ventilation",
  "Ground",
  "DomesticHotWater",
  "SystemEnergy",
  "Validation",
  "CalculationTrace",
  "Reports",
  "Review",
];

export function EngineeringWorkflowShell({ projectId, buildingId }: EngineeringWorkflowShellProps): JSX.Element {
  const vm = useEngineeringWorkflowShell(projectId, buildingId);
  const trustOverview = useEngineeringWorkflowTrustOverview({
    workflowState: vm.workflow.state,
    diagnostics: vm.allDiagnostics,
    traceSummary: vm.traceSummary,
  });

  return (
    <Stack spacing={2}>
      <QueryState isLoading={vm.workflow.isLoading} error={vm.workflow.error} onRetry={() => void vm.workflow.refresh()} />

      {vm.workflow.state ? (
        <>
          <Alert severity={vm.workflow.mode === "dev" ? "warning" : "info"}>
            {vm.workflow.state.workflowModeLabel}. Persistence provider: {vm.workflow.state.metadata.persistenceProvider ?? "n/a"} ({vm.workflow.state.metadata.durablePersistenceEnabled === "true" ? "durable foundation" : "foundation"}). This frontend workflow aggregates existing contracts and does not run engineering physics in browser.
          </Alert>

          <Stack direction={{ xs: "column", lg: "row" }} spacing={2} alignItems="stretch">
            <DataCard>
              <Stack spacing={1}>
                <Typography variant="h6">Workflow steps</Typography>
                <List dense>
                  {orderedSteps.map((step) => {
                    const status = vm.stepStatus.get(step) ?? "incomplete";
                    return (
                      <ListItemButton
                        key={step}
                        selected={vm.selectedStep === step}
                        onClick={() => vm.setSelectedStep(step)}
                        sx={{ borderRadius: 1, mb: 0.5 }}
                      >
                        <ListItemText primary={stepLabel(step)} secondary={statusLabel(status)} />
                        <StatusChip status={status} />
                      </ListItemButton>
                    );
                  })}
                </List>
              </Stack>
            </DataCard>

            <Stack spacing={2} sx={{ flex: 1 }}>
              <DataCard>
                <Stack spacing={2}>
                  <Typography variant="h6">{stepLabel(vm.selectedStep)}</Typography>
                  <EngineeringWorkflowStepContent
                    step={vm.selectedStep}
                    state={vm.workflow.state}
                    statusChip={(status) => <StatusChip status={status} />}
                  />
                  <Divider />
                  <Stack direction={{ xs: "column", sm: "row" }} spacing={1}>
                    <Button variant="contained" startIcon={<PlayArrowIcon />} onClick={() => void vm.prepareRequest()}>
                      Prepare calculation request
                    </Button>
                    <Button variant="contained" color="secondary" startIcon={<PlayArrowIcon />} onClick={() => void vm.runAvailableModules()}>
                      Run available modules
                    </Button>
                    <Button variant="outlined" onClick={() => void vm.generateReport(vm.workflow.state!)}>
                      Generate report preview
                    </Button>
                  </Stack>
                  {vm.preparation ? (
                    <Alert severity={vm.preparation.status === "prepared" ? "success" : "error"}>
                      Request status: {vm.preparation.status}. Scenario ID: {vm.preparation.requestId}. {vm.preparation.metadata.note}
                    </Alert>
                  ) : null}
                  {vm.scenarioResult ? (
                    <Alert severity={vm.scenarioResult.status.includes("Failed") ? "error" : vm.scenarioResult.status === "PartiallyExecuted" ? "warning" : "success"}>
                      Scenario ID: {vm.scenarioResult.scenarioId}. Status: {vm.scenarioResult.status}. Executed modules: {vm.scenarioResult.executedModules.length}. Skipped modules: {vm.scenarioResult.skippedModules.length}.
                    </Alert>
                  ) : null}
                  {vm.scenarioResult ? (
                    <Stack spacing={1}>
                      <Typography variant="body2" sx={{ fontWeight: 600 }}>Module execution status</Typography>
                      <Stack direction="row" spacing={1} flexWrap="wrap">
                        {vm.scenarioResult.moduleResults.map((item) => (
                          <Chip key={item.moduleKind} label={`${item.moduleKind}: ${item.status}`} size="small" variant="outlined" sx={{ mb: 1 }} />
                        ))}
                      </Stack>
                    </Stack>
                  ) : null}
                </Stack>
              </DataCard>

              <CalculationTracePanel trace={vm.traceSummary} onDetailLevelChange={(detailLevel) => vm.onTraceDetailLevelChange(detailLevel)} />

              <EngineeringReportPreview
                preview={vm.reportPreview}
                diagnostics={vm.allDiagnostics}
                jsonOutput={vm.jsonOutput}
                markdownOutput={vm.markdownOutput}
                onExportJson={() => vm.exportJson(vm.workflow.state!)}
                onExportMarkdown={() => vm.exportMarkdown(vm.workflow.state!)}
              />

              <EngineeringScenarioHistoryPanel
                scenarios={vm.scenarioHistory}
                selectedScenarioId={vm.selectedScenarioId}
                onViewResult={(scenarioId) => void vm.loadScenarioResult(scenarioId)}
                onLoadArtifacts={(scenarioId) => void vm.loadScenarioArtifacts(scenarioId)}
                artifacts={vm.scenarioArtifacts}
                onViewArtifact={(scenarioId, artifactKind) => void vm.openScenarioArtifact(scenarioId, artifactKind)}
              />

              <EngineeringCalculationJobPanel
                currentJob={vm.currentJob}
                jobs={vm.jobHistory}
                events={vm.jobEvents}
                onRefreshJob={(jobId) => void vm.refreshCurrentJob(jobId)}
                onRefreshEvents={(jobId) => void vm.refreshJobEvents(jobId)}
                onCancelJob={(jobId) => void vm.cancelJob(jobId)}
                onSelectJob={(jobId) => void vm.selectJob(jobId)}
              />
            </Stack>

            <Stack spacing={2} sx={{ width: { xs: "100%", lg: 420 } }}>
              <WorkflowDiagnosticsPanel diagnostics={vm.allDiagnostics} onSelectStep={vm.setSelectedStep} />
              <EngineeringTrustOverviewPanel model={trustOverview} />
            </Stack>
          </Stack>
        </>
      ) : (
        <EmptyState title="Workflow state is unavailable" description="Select a project and building to initialize engineering workflow foundation." />
      )}
    </Stack>
  );
}

function statusLabel(status: WorkflowStepStatus): string {
  if (status === "valid") {
    return "Valid";
  }

  if (status === "warnings") {
    return "Warnings";
  }

  if (status === "errors") {
    return "Errors";
  }

  if (status === "ready") {
    return "Ready";
  }

  return "Incomplete";
}

function StatusChip({ status }: { status: WorkflowStepStatus }): JSX.Element {
  if (status === "errors") {
    return <Chip label="Errors" size="small" color="error" />;
  }

  if (status === "warnings") {
    return <Chip label="Warnings" size="small" color="warning" />;
  }

  if (status === "valid" || status === "ready") {
    return <Chip label={status === "ready" ? "Ready" : "Valid"} size="small" color="success" />;
  }

  return <Chip label="Incomplete" size="small" variant="outlined" />;
}
