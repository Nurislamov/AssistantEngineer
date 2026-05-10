import PlayArrowIcon from "@mui/icons-material/PlayArrow";
import {
  Alert,
  Box,
  Button,
  Chip,
  Divider,
  List,
  ListItemButton,
  ListItemText,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from "@mui/material";
import { useEffect, useMemo, useState } from "react";
import { useEngineeringWorkflow } from "@/entities/engineering-workflow/model/useEngineeringWorkflow";
import type {
  EngineeringCalculationArtifactRecord,
  EngineeringCalculationScenarioRecord,
  EngineeringCalculationScenarioResult,
  EngineeringWorkflowCalculationPreparationResult,
  EngineeringWorkflowReportRequest,
  ProjectWorkflowState,
  WorkflowDiagnostic,
  WorkflowStepKind,
  WorkflowStepStatus,
} from "@/entities/engineering-workflow/types";
import { summarizeBuildingMetrics } from "@/entities/engineering-workflow/api/engineeringWorkflowClient";
import { formatNumber } from "@/shared/lib/format";
import { getErrorMessage } from "@/shared/lib/getErrorMessage";
import { DataCard } from "@/shared/ui/DataCard";
import { EmptyState } from "@/shared/ui/EmptyState";
import { QueryState } from "@/shared/ui/QueryState";
import { CalculationTracePanel } from "./CalculationTracePanel";
import { EngineeringReportPreview } from "./EngineeringReportPreview";
import { EngineeringScenarioHistoryPanel } from "./EngineeringScenarioHistoryPanel";
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
  const workflow = useEngineeringWorkflow(projectId, buildingId);
  const [selectedStep, setSelectedStep] = useState<WorkflowStepKind>("Project");
  const [traceSummary, setTraceSummary] = useState(workflow.state?.calculationTraceSummary);
  const [reportPreview, setReportPreview] = useState(workflow.state?.reportSummary);
  const [reportDiagnostics, setReportDiagnostics] = useState<WorkflowDiagnostic[]>([]);
  const [jsonOutput, setJsonOutput] = useState("");
  const [markdownOutput, setMarkdownOutput] = useState("");
  const [preparation, setPreparation] = useState<EngineeringWorkflowCalculationPreparationResult | null>(null);
  const [scenarioResult, setScenarioResult] = useState<EngineeringCalculationScenarioResult | null>(null);
  const [scenarioHistory, setScenarioHistory] = useState<EngineeringCalculationScenarioRecord[]>([]);
  const [selectedScenarioId, setSelectedScenarioId] = useState<string | undefined>();
  const [scenarioArtifacts, setScenarioArtifacts] = useState<EngineeringCalculationArtifactRecord[]>([]);

  useEffect(() => {
    if (workflow.state?.currentStep) {
      setSelectedStep(workflow.state.currentStep);
    }

    if (workflow.state?.calculationTraceSummary) {
      setTraceSummary(workflow.state.calculationTraceSummary);
    }

    if (workflow.state?.reportSummary) {
      setReportPreview(workflow.state.reportSummary);
    }
  }, [workflow.state]);

  useEffect(() => {
    const loadHistory = async () => {
      const scenarios = await workflow.listScenarios();
      setScenarioHistory(scenarios);
    };

    void loadHistory();
  }, [workflow.state?.projectId]);

  const stepStatus = useMemo(() => {
    const map = new Map<WorkflowStepKind, WorkflowStepStatus>();
    for (const item of workflow.state?.completionByStep ?? []) {
      map.set(item.step, item.status);
    }

    return map;
  }, [workflow.state?.completionByStep]);

  const allDiagnostics = useMemo(
    () => deduplicateDiagnostics([...(workflow.state?.validationDiagnostics ?? []), ...reportDiagnostics]),
    [reportDiagnostics, workflow.state?.validationDiagnostics],
  );

  const generateReport = async (state: ProjectWorkflowState) => {
    const request: EngineeringWorkflowReportRequest = {
      reportKind: "FullEngineeringCore",
      format: "Json",
      includeTraceAppendix: true,
      includeLimitations: true,
      state,
    };

    const result = await workflow.generateReport(request);
    setReportPreview(result.preview);
    setReportDiagnostics(result.diagnostics);
    setJsonOutput(result.json ?? "");
    setMarkdownOutput(result.markdown ?? "");
  };

  const exportJson = async (state: ProjectWorkflowState) => {
    const request: EngineeringWorkflowReportRequest = {
      reportKind: "FullEngineeringCore",
      format: "Json",
      includeTraceAppendix: true,
      includeLimitations: true,
      state,
    };

    setJsonOutput(await workflow.exportReportJson(request));
  };

  const exportMarkdown = async (state: ProjectWorkflowState) => {
    const request: EngineeringWorkflowReportRequest = {
      reportKind: "FullEngineeringCore",
      format: "Markdown",
      includeTraceAppendix: true,
      includeLimitations: true,
      state,
    };

    setMarkdownOutput(await workflow.exportReportMarkdown(request));
  };

  const prepareRequest = async () => {
    setPreparation(await workflow.prepareCalculation());
  };

  const runAvailableModules = async () => {
    const result = await workflow.runCalculation("ExecuteAvailableModules");
    setScenarioResult(result);
    setSelectedScenarioId(result.scenarioId);

    if (result.calculationTraceSummary) {
      setTraceSummary(result.calculationTraceSummary);
    }

    if (result.reportPreview) {
      setReportPreview(result.reportPreview);
    }

    if (result.reportJson) {
      setJsonOutput(result.reportJson);
    }

    if (result.reportMarkdown) {
      setMarkdownOutput(result.reportMarkdown);
    }

    if (result.validationDiagnostics.length > 0) {
      setReportDiagnostics((previous) => deduplicateDiagnostics([...previous, ...result.validationDiagnostics]));
    }

    const scenarios = await workflow.listScenarios();
    setScenarioHistory(scenarios);
  };

  const loadScenarioResult = async (scenarioId: string) => {
    const record = await workflow.getScenarioResult(scenarioId);
    setSelectedScenarioId(scenarioId);

    if (!record) {
      return;
    }

    const artifact = await workflow.getScenarioArtifact(scenarioId, "ScenarioResultJson");
    if (!artifact?.content) {
      return;
    }

    try {
      const parsed = JSON.parse(artifact.content) as EngineeringCalculationScenarioResult;
      setScenarioResult(parsed);

      if (parsed.calculationTraceSummary) {
        setTraceSummary(parsed.calculationTraceSummary);
      }

      if (parsed.reportPreview) {
        setReportPreview(parsed.reportPreview);
      }

      if (parsed.reportJson) {
        setJsonOutput(parsed.reportJson);
      }

      if (parsed.reportMarkdown) {
        setMarkdownOutput(parsed.reportMarkdown);
      }
    } catch {
      // Keep UI deterministic and leave currently rendered results untouched.
    }
  };

  const loadScenarioArtifacts = async (scenarioId: string) => {
    const artifacts = await workflow.getScenarioArtifacts(scenarioId);
    setSelectedScenarioId(scenarioId);
    setScenarioArtifacts(artifacts);
  };

  const openScenarioArtifact = async (scenarioId: string, artifactKind: EngineeringCalculationArtifactRecord["artifactKind"]) => {
    const artifact = await workflow.getScenarioArtifact(scenarioId, artifactKind);
    if (!artifact) {
      return;
    }

    if (artifact.artifactKind === "TraceJson") {
      try {
        const parsed = JSON.parse(artifact.content) as { steps?: Array<{ stepId: string; moduleKind: string; stepName: string; sequence: number; assumptions: string[]; warnings: string[]; diagnostics: unknown[] }>; traceId?: string; calculationId?: string; assumptions?: string[]; warnings?: string[] };
        setTraceSummary({
          traceId: parsed.traceId ?? "persisted-trace",
          calculationId: parsed.calculationId,
          detailLevel: "Detailed",
          modules: (parsed.steps ?? []).map((item) => item.moduleKind),
          assumptions: parsed.assumptions ?? [],
          warnings: parsed.warnings ?? [],
          steps: (parsed.steps ?? []).map((item) => ({
            stepId: item.stepId,
            moduleKind: item.moduleKind,
            stepName: item.stepName,
            sequence: item.sequence,
            assumptions: item.assumptions ?? [],
            warnings: item.warnings ?? [],
            diagnosticsCount: Array.isArray(item.diagnostics) ? item.diagnostics.length : 0,
          })),
        });
      } catch {
        // Keep UI deterministic and leave currently rendered trace untouched.
      }
    }

    if (artifact.artifactKind === "ReportJson" || artifact.artifactKind === "ScenarioResultJson") {
      setJsonOutput(artifact.content);
    }

    if (artifact.artifactKind === "ReportMarkdown") {
      setMarkdownOutput(artifact.content);
    }
  };

  return (
    <Stack spacing={2}>
      <QueryState isLoading={workflow.isLoading} error={workflow.error} onRetry={() => void workflow.refresh()} />

      {workflow.state ? (
        <>
          <Alert severity={workflow.mode === "dev" ? "warning" : "info"}>
            {workflow.state.workflowModeLabel}. This frontend workflow aggregates existing contracts and does not run engineering physics in browser.
          </Alert>

          <Stack direction={{ xs: "column", lg: "row" }} spacing={2} alignItems="stretch">
            <DataCard>
              <Stack spacing={1}>
                <Typography variant="h6">Workflow steps</Typography>
                <List dense>
                  {orderedSteps.map((step) => {
                    const status = stepStatus.get(step) ?? "incomplete";
                    return (
                      <ListItemButton
                        key={step}
                        selected={selectedStep === step}
                        onClick={() => setSelectedStep(step)}
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
                  <Typography variant="h6">{stepLabel(selectedStep)}</Typography>
                  {renderStepContent(selectedStep, workflow.state)}
                  <Divider />
                  <Stack direction={{ xs: "column", sm: "row" }} spacing={1}>
                    <Button variant="contained" startIcon={<PlayArrowIcon />} onClick={() => void prepareRequest()}>
                      Prepare calculation request
                    </Button>
                    <Button variant="contained" color="secondary" startIcon={<PlayArrowIcon />} onClick={() => void runAvailableModules()}>
                      Run available modules
                    </Button>
                    <Button variant="outlined" onClick={() => void generateReport(workflow.state!)}>
                      Generate report preview
                    </Button>
                  </Stack>
                  {preparation ? (
                    <Alert severity={preparation.status === "prepared" ? "success" : "error"}>
                      Request status: {preparation.status}. Scenario ID: {preparation.requestId}. {preparation.metadata.note}
                    </Alert>
                  ) : null}
                  {scenarioResult ? (
                    <Alert severity={scenarioResult.status.includes("Failed") ? "error" : scenarioResult.status === "PartiallyExecuted" ? "warning" : "success"}>
                      Scenario ID: {scenarioResult.scenarioId}. Status: {scenarioResult.status}. Executed modules: {scenarioResult.executedModules.length}. Skipped modules: {scenarioResult.skippedModules.length}.
                    </Alert>
                  ) : null}
                  {scenarioResult ? (
                    <Stack spacing={1}>
                      <Typography variant="body2" sx={{ fontWeight: 600 }}>Module execution status</Typography>
                      <Stack direction="row" spacing={1} flexWrap="wrap">
                        {scenarioResult.moduleResults.map((item) => (
                          <Chip key={item.moduleKind} label={`${item.moduleKind}: ${item.status}`} size="small" variant="outlined" sx={{ mb: 1 }} />
                        ))}
                      </Stack>
                    </Stack>
                  ) : null}
                </Stack>
              </DataCard>

              <CalculationTracePanel
                trace={traceSummary}
                onDetailLevelChange={async (detailLevel) => {
                  const next = await workflow.loadTracePreview(detailLevel);
                  setTraceSummary(next);
                }}
              />

              <EngineeringReportPreview
                preview={reportPreview}
                diagnostics={allDiagnostics}
                jsonOutput={jsonOutput}
                markdownOutput={markdownOutput}
                onExportJson={() => exportJson(workflow.state!)}
                onExportMarkdown={() => exportMarkdown(workflow.state!)}
              />

              <EngineeringScenarioHistoryPanel
                scenarios={scenarioHistory}
                selectedScenarioId={selectedScenarioId}
                onViewResult={(scenarioId) => void loadScenarioResult(scenarioId)}
                onLoadArtifacts={(scenarioId) => void loadScenarioArtifacts(scenarioId)}
                artifacts={scenarioArtifacts}
                onViewArtifact={(scenarioId, artifactKind) => void openScenarioArtifact(scenarioId, artifactKind)}
              />
            </Stack>

            <Box sx={{ width: { xs: "100%", lg: 420 } }}>
              <WorkflowDiagnosticsPanel diagnostics={allDiagnostics} onSelectStep={setSelectedStep} />
            </Box>
          </Stack>
        </>
      ) : (
        <EmptyState title="Workflow state is unavailable" description="Select a project and building to initialize engineering workflow foundation." />
      )}
    </Stack>
  );
}

function renderStepContent(step: WorkflowStepKind, state: ProjectWorkflowState): JSX.Element {
  if (step === "Project" || step === "Building") {
    return (
      <Stack spacing={0.5}>
        <Typography variant="body2">Project: {state.buildingMetadata.projectName ?? "n/a"}</Typography>
        <Typography variant="body2">Building: {state.buildingMetadata.buildingName ?? "n/a"}</Typography>
        <Typography variant="body2">Location: {state.buildingMetadata.locationText ?? "n/a"}</Typography>
        <Typography variant="body2">Floor area: {formatNumber(state.buildingMetadata.floorAreaM2, 1)} m2</Typography>
        <Typography variant="body2">Volume: {formatNumber(state.buildingMetadata.volumeM3, 0)} m3</Typography>
      </Stack>
    );
  }

  if (step === "Zones") {
    return state.zones.length === 0 ? (
      <EmptyState title="No zones" description="Create zones in Building workspace before running full engineering workflow." />
    ) : (
      <TableContainer>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Zone</TableCell>
              <TableCell>Kind</TableCell>
              <TableCell>Area</TableCell>
              <TableCell>Volume</TableCell>
              <TableCell>Status</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {state.zones.map((zone) => (
              <TableRow key={String(zone.id)}>
                <TableCell>{zone.name}</TableCell>
                <TableCell>{zone.zoneKind}</TableCell>
                <TableCell>{formatNumber(zone.floorAreaM2, 1)} m2</TableCell>
                <TableCell>{formatNumber(zone.airVolumeM3, 0)} m3</TableCell>
                <TableCell><StatusChip status={zone.status} /></TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    );
  }

  if (step === "Envelope") {
    return state.boundaries.length === 0 ? (
      <EmptyState title="No boundaries" description="Add wall boundaries in building workspace envelope panel." />
    ) : (
      <TableContainer>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Room / zone</TableCell>
              <TableCell>Exposure</TableCell>
              <TableCell>Area</TableCell>
              <TableCell>U-value</TableCell>
              <TableCell>Adjacent</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {state.boundaries.slice(0, 60).map((boundary) => (
              <TableRow key={String(boundary.id)}>
                <TableCell>{boundary.zoneOrRoomName}</TableCell>
                <TableCell>{boundary.exposureKind}</TableCell>
                <TableCell>{formatNumber(boundary.areaM2, 2)} m2</TableCell>
                <TableCell>{formatNumber(boundary.uValue, 3)} W/(m2*K)</TableCell>
                <TableCell>{boundary.adjacentZoneReference ?? "-"}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>
    );
  }

  if (step === "WeatherSolar") {
    return (
      <Stack spacing={0.5}>
        <Typography variant="body2">Weather source status: {state.weatherSolarSettings.weatherSourceStatus}</Typography>
        <Typography variant="body2">Location/timezone summary: {state.weatherSolarSettings.locationTimezoneSummary}</Typography>
        <Typography variant="body2">Solar chain readiness: {state.weatherSolarSettings.solarChainReadinessSummary}</Typography>
      </Stack>
    );
  }

  if (step === "Ventilation") {
    return (
      <Stack spacing={0.5}>
        <Typography variant="body2">Openings: {state.ventilationSettings.openingCount}</Typography>
        <Typography variant="body2">Control mode: {state.ventilationSettings.controlModeSummary}</Typography>
        <Typography variant="body2">Airflow/Hve summary: {state.ventilationSettings.airflowSummary}</Typography>
      </Stack>
    );
  }

  if (step === "Ground") {
    return (
      <Stack spacing={0.5}>
        <Typography variant="body2">Ground boundary count: {state.groundSettings.groundBoundaryCount}</Typography>
        <Typography variant="body2">Ground profile mode: {state.groundSettings.groundProfileMode}</Typography>
        <Typography variant="body2">Status: {statusLabel(state.groundSettings.summaryStatus)}</Typography>
      </Stack>
    );
  }

  if (step === "DomesticHotWater") {
    return (
      <Stack spacing={0.5}>
        <Typography variant="body2">Demand basis: {state.domesticHotWaterSettings.demandBasis}</Typography>
        <Typography variant="body2">Useful demand summary: {state.domesticHotWaterSettings.usefulDemandSummary}</Typography>
        <Typography variant="body2">Losses summary: {state.domesticHotWaterSettings.lossesSummary}</Typography>
        <Typography variant="body2">Ownership policy: {state.domesticHotWaterSettings.ownershipPolicy}</Typography>
      </Stack>
    );
  }

  if (step === "SystemEnergy") {
    return (
      <Stack spacing={0.5}>
        <Typography variant="body2">Uses: {state.systemEnergySettings.usesSummary}</Typography>
        <Typography variant="body2">Carriers: {state.systemEnergySettings.carriersSummary}</Typography>
        <Typography variant="body2">Final/Primary/CO2: {state.systemEnergySettings.finalPrimaryCarbonSummary}</Typography>
      </Stack>
    );
  }

  if (step === "Validation") {
    const errors = state.validationDiagnostics.filter((item) => item.severity === "error").length;
    const warnings = state.validationDiagnostics.filter((item) => item.severity === "warning").length;
    return (
      <Stack spacing={0.5}>
        <Typography variant="body2">Diagnostics summary: {state.validationDiagnostics.length}</Typography>
        <Typography variant="body2">Errors: {errors}</Typography>
        <Typography variant="body2">Warnings: {warnings}</Typography>
      </Stack>
    );
  }

  if (step === "CalculationTrace") {
    return (
      <Stack spacing={0.5}>
        <Typography variant="body2">Trace modules: {state.calculationTraceSummary?.modules.join(", ") ?? "n/a"}</Typography>
        <Typography variant="body2">Assumptions: {state.calculationTraceSummary?.assumptions.length ?? 0}</Typography>
        <Typography variant="body2">Warnings: {state.calculationTraceSummary?.warnings.length ?? 0}</Typography>
      </Stack>
    );
  }

  if (step === "Reports") {
    return (
      <Stack spacing={0.5}>
        <Typography variant="body2">Preview title: {state.reportSummary?.title ?? "n/a"}</Typography>
        <Typography variant="body2">Sections: {state.reportSummary?.sections.length ?? 0}</Typography>
        <Typography variant="body2">Available formats: {state.reportSummary?.exportFormatsAvailable.join(", ") ?? "Json, Markdown"}</Typography>
      </Stack>
    );
  }

  return (
    <Stack spacing={0.5}>
      <Typography variant="body2">Review summary: {summarizeBuildingMetrics(state)}</Typography>
      <Typography variant="body2">Current mode: {state.workflowModeLabel}</Typography>
    </Stack>
  );
}

function stepLabel(step: WorkflowStepKind): string {
  if (step === "DomesticHotWater") {
    return "Domestic hot water";
  }

  if (step === "WeatherSolar") {
    return "Weather / Solar";
  }

  if (step === "SystemEnergy") {
    return "System energy";
  }

  if (step === "CalculationTrace") {
    return "Calculation trace";
  }

  return step;
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

function deduplicateDiagnostics(diagnostics: WorkflowDiagnostic[]): WorkflowDiagnostic[] {
  const keys = new Set<string>();
  return diagnostics.filter((item) => {
    const key = `${item.sourceStep}|${item.code}|${item.message}`;
    if (keys.has(key)) {
      return false;
    }

    keys.add(key);
    return true;
  });
}
