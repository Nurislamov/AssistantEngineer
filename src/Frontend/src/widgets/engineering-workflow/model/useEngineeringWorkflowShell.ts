import { useEffect, useMemo, useState } from "react";
import { useEngineeringWorkflow } from "@/entities/engineering-workflow/model/useEngineeringWorkflow";
import type {
  EngineeringCalculationArtifactRecord,
  EngineeringCalculationJobEvent,
  EngineeringCalculationJobResult,
  EngineeringCalculationScenarioRecord,
  EngineeringCalculationScenarioResult,
  EngineeringWorkflowCalculationPreparationResult,
  EngineeringWorkflowReportRequest,
  ProjectWorkflowState,
  WorkflowDiagnostic,
  WorkflowStepKind,
  WorkflowStepStatus,
  WorkflowTraceDetailLevel,
} from "@/entities/engineering-workflow/types";
import { deduplicateDiagnostics } from "./engineeringWorkflowShellViewModel";

interface UseEngineeringWorkflowShellResult {
  workflow: ReturnType<typeof useEngineeringWorkflow>;
  selectedStep: WorkflowStepKind;
  setSelectedStep: (step: WorkflowStepKind) => void;
  traceSummary: ProjectWorkflowState["calculationTraceSummary"];
  reportPreview: ProjectWorkflowState["reportSummary"];
  reportDiagnostics: WorkflowDiagnostic[];
  jsonOutput: string;
  markdownOutput: string;
  preparation: EngineeringWorkflowCalculationPreparationResult | null;
  scenarioResult: EngineeringCalculationScenarioResult | null;
  scenarioHistory: EngineeringCalculationScenarioRecord[];
  selectedScenarioId: string | undefined;
  scenarioArtifacts: EngineeringCalculationArtifactRecord[];
  jobHistory: EngineeringCalculationJobResult[];
  currentJob: EngineeringCalculationJobResult | null;
  jobEvents: EngineeringCalculationJobEvent[];
  stepStatus: Map<WorkflowStepKind, WorkflowStepStatus>;
  allDiagnostics: WorkflowDiagnostic[];
  prepareRequest: () => Promise<void>;
  runAvailableModules: () => Promise<void>;
  generateReport: (state: ProjectWorkflowState) => Promise<void>;
  exportJson: (state: ProjectWorkflowState) => Promise<void>;
  exportMarkdown: (state: ProjectWorkflowState) => Promise<void>;
  refreshCurrentJob: (jobId: string) => Promise<void>;
  refreshJobEvents: (jobId: string) => Promise<void>;
  cancelJob: (jobId: string) => Promise<void>;
  selectJob: (jobId: string) => Promise<void>;
  loadScenarioResult: (scenarioId: string) => Promise<void>;
  loadScenarioArtifacts: (scenarioId: string) => Promise<void>;
  openScenarioArtifact: (scenarioId: string, artifactKind: EngineeringCalculationArtifactRecord["artifactKind"]) => Promise<void>;
  onTraceDetailLevelChange: (detailLevel: WorkflowTraceDetailLevel) => Promise<void>;
}

export function useEngineeringWorkflowShell(
  projectId: number,
  buildingId: number,
): UseEngineeringWorkflowShellResult {
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
  const [jobHistory, setJobHistory] = useState<EngineeringCalculationJobResult[]>([]);
  const [currentJob, setCurrentJob] = useState<EngineeringCalculationJobResult | null>(null);
  const [jobEvents, setJobEvents] = useState<EngineeringCalculationJobEvent[]>([]);

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
      const jobs = await workflow.listProjectJobs();
      setJobHistory(jobs);
    };

    void loadHistory();
  }, [workflow, workflow.state?.projectId]);

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

  const syncFromScenarioResult = (result: EngineeringCalculationScenarioResult) => {
    setScenarioResult(result);

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
  };

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
    const job = await workflow.createCalculationJob("Synchronous");
    setCurrentJob(job);
    setSelectedScenarioId(job.scenarioId);
    setJobEvents(job.historyEvents ?? []);

    const result = job.scenarioResultSummary;
    if (result) {
      syncFromScenarioResult(result);
    }

    const scenarios = await workflow.listScenarios();
    setScenarioHistory(scenarios);
    const jobs = await workflow.listProjectJobs();
    setJobHistory(jobs);
  };

  const refreshCurrentJob = async (jobId: string) => {
    const job = await workflow.getCalculationJob(jobId);
    if (!job) {
      return;
    }

    setCurrentJob(job);
    setSelectedScenarioId(job.scenarioId);
    if (job.scenarioResultSummary) {
      syncFromScenarioResult(job.scenarioResultSummary);
    }

    const jobs = await workflow.listProjectJobs();
    setJobHistory(jobs);
  };

  const refreshJobEvents = async (jobId: string) => {
    const events = await workflow.getCalculationJobEvents(jobId);
    setJobEvents(events);
  };

  const cancelJob = async (jobId: string) => {
    const job = await workflow.cancelCalculationJob(jobId);
    if (!job) {
      return;
    }

    setCurrentJob(job);
    setJobEvents(job.historyEvents ?? []);
    const jobs = await workflow.listProjectJobs();
    setJobHistory(jobs);
  };

  const selectJob = async (jobId: string) => {
    await refreshCurrentJob(jobId);
    await refreshJobEvents(jobId);
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
      syncFromScenarioResult(parsed);
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
        const parsed = JSON.parse(artifact.content) as {
          steps?: Array<{ stepId: string; moduleKind: string; stepName: string; sequence: number; assumptions: string[]; warnings: string[]; diagnostics: unknown[] }>;
          traceId?: string;
          calculationId?: string;
          assumptions?: string[];
          warnings?: string[];
        };
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

  const onTraceDetailLevelChange = async (detailLevel: WorkflowTraceDetailLevel) => {
    const next = await workflow.loadTracePreview(detailLevel);
    setTraceSummary(next);
  };

  return {
    workflow,
    selectedStep,
    setSelectedStep,
    traceSummary,
    reportPreview,
    reportDiagnostics,
    jsonOutput,
    markdownOutput,
    preparation,
    scenarioResult,
    scenarioHistory,
    selectedScenarioId,
    scenarioArtifacts,
    jobHistory,
    currentJob,
    jobEvents,
    stepStatus,
    allDiagnostics,
    prepareRequest,
    runAvailableModules,
    generateReport,
    exportJson,
    exportMarkdown,
    refreshCurrentJob,
    refreshJobEvents,
    cancelJob,
    selectJob,
    loadScenarioResult,
    loadScenarioArtifacts,
    openScenarioArtifact,
    onTraceDetailLevelChange,
  };
}
