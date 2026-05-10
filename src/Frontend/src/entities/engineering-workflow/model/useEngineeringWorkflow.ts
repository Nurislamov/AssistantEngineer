import { useMemo } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/shared/api/queryKeys";
import { createEngineeringWorkflowClient } from "../api/engineeringWorkflowClient";
import type {
  EngineeringCalculationScenarioRequest,
  EngineeringCalculationScenarioResult,
  EngineeringWorkflowCalculationPreparationResult,
  EngineeringWorkflowCalculationRequest,
  EngineeringWorkflowReportRequest,
  EngineeringWorkflowReportResult,
  ProjectWorkflowState,
  WorkflowCalculationTraceSummary,
  WorkflowDiagnostic,
  WorkflowTraceDetailLevel,
} from "../types";

interface UseEngineeringWorkflowResult {
  mode: "api" | "dev";
  state: ProjectWorkflowState | undefined;
  isLoading: boolean;
  error: unknown;
  refresh: () => Promise<unknown>;
  validate: () => Promise<WorkflowDiagnostic[]>;
  prepareCalculation: () => Promise<EngineeringWorkflowCalculationPreparationResult>;
  runCalculation: (mode?: "ExecuteAvailableModules" | "ExecuteFullRequired") => Promise<EngineeringCalculationScenarioResult>;
  loadTracePreview: (detailLevel: WorkflowTraceDetailLevel) => Promise<WorkflowCalculationTraceSummary>;
  generateReport: (request: EngineeringWorkflowReportRequest) => Promise<EngineeringWorkflowReportResult>;
  exportReportJson: (request: EngineeringWorkflowReportRequest) => Promise<string>;
  exportReportMarkdown: (request: EngineeringWorkflowReportRequest) => Promise<string>;
}

function resolveWorkflowMode(): "api" | "dev" {
  const raw = import.meta.env.VITE_ENGINEERING_WORKFLOW_MODE;
  if (typeof raw === "string" && raw.trim().toLowerCase() === "api") {
    return "api";
  }

  return "dev";
}

export function useEngineeringWorkflow(
  projectId: number,
  buildingId: number,
): UseEngineeringWorkflowResult {
  const mode = resolveWorkflowMode();
  const client = useMemo(() => createEngineeringWorkflowClient(mode), [mode]);

  const stateQuery = useQuery({
    queryKey: queryKeys.workflow.state(projectId, buildingId, mode),
    queryFn: () => client.getWorkflowState(projectId, buildingId),
    enabled: Number.isFinite(projectId) && projectId > 0 && Number.isFinite(buildingId) && buildingId > 0,
  });

  const validateMutation = useMutation({
    mutationFn: (state: ProjectWorkflowState) => client.validateWorkflow(state),
  });

  const prepareMutation = useMutation({
    mutationFn: (request: EngineeringWorkflowCalculationRequest) => client.prepareCalculation(request),
  });

  const traceMutation = useMutation({
    mutationFn: ({ state, detailLevel }: { state: ProjectWorkflowState; detailLevel: WorkflowTraceDetailLevel }) =>
      client.getTracePreview(state, detailLevel),
  });

  const runMutation = useMutation({
    mutationFn: (request: EngineeringCalculationScenarioRequest) => client.runCalculation(request),
  });

  const reportMutation = useMutation({
    mutationFn: (request: EngineeringWorkflowReportRequest) => client.generateReport(request),
  });

  const refresh = async () => stateQuery.refetch();

  const validate = async () => {
    if (!stateQuery.data) {
      return [];
    }

    return validateMutation.mutateAsync(stateQuery.data);
  };

  const prepareCalculation = async () => {
    if (!stateQuery.data) {
      const fallback: EngineeringWorkflowCalculationPreparationResult = {
        requestId: "workflow-request-missing-state",
        status: "blocked",
        diagnostics: [
          {
            severity: "error",
            code: "WORKFLOW_STATE_MISSING",
            message: "Workflow state is not loaded.",
            sourceStep: "Review",
          },
        ],
        metadata: {
          mode,
        },
      };

      return fallback;
    }

    const request = client.buildCalculationRequest(stateQuery.data);
    return prepareMutation.mutateAsync(request);
  };

  const loadTracePreview = async (detailLevel: WorkflowTraceDetailLevel) => {
    if (!stateQuery.data) {
      return {
        traceId: `workflow-trace-missing-${buildingId}`,
        calculationId: `building-${buildingId}`,
        detailLevel,
        modules: ["Validation"],
        assumptions: ["Workflow state is not loaded."],
        warnings: ["Trace preview could not be loaded because workflow state is missing."],
        steps: [],
      };
    }

    return traceMutation.mutateAsync({ state: stateQuery.data, detailLevel });
  };

  const runCalculation = async (
    mode: "ExecuteAvailableModules" | "ExecuteFullRequired" = "ExecuteAvailableModules",
  ): Promise<EngineeringCalculationScenarioResult> => {
    if (!stateQuery.data) {
      return {
        scenarioId: `workflow-run-missing-${buildingId}`,
        status: "FailedValidation",
        executed: false,
        executedModules: [],
        skippedModules: [],
        unavailableModules: ["Runner"],
        validationDiagnostics: [
          {
            severity: "error",
            code: "WORKFLOW_STATE_MISSING",
            message: "Workflow state is not loaded.",
            sourceStep: "Review",
          },
        ],
        assumptions: ["Scenario runner was not executed because workflow state is missing."],
        warnings: [],
        moduleSummaries: {},
        moduleResults: [],
        timings: [],
        metadata: { mode },
      };
    }

    const request: EngineeringCalculationScenarioRequest = {
      scenarioId: `wf-run-${stateQuery.data.projectId ?? projectId}-${stateQuery.data.buildingId ?? buildingId}`,
      projectId: stateQuery.data.projectId,
      buildingId: stateQuery.data.buildingId,
      scenarioKind: "FullEngineeringCore",
      executionMode: mode,
      state: stateQuery.data,
      requestedModules: stateQuery.data.availableModules,
      detailLevel: stateQuery.data.calculationTraceSummary?.detailLevel ?? "Standard",
      includeTrace: true,
      includeReport: true,
      reportFormats: ["Json", "Markdown"],
      diagnosticsMode: "Deterministic",
    };

    return runMutation.mutateAsync(request);
  };

  const generateReport = async (request: EngineeringWorkflowReportRequest) =>
    reportMutation.mutateAsync(request);

  const exportReportJson = async (request: EngineeringWorkflowReportRequest) =>
    client.exportReportJson(request);

  const exportReportMarkdown = async (request: EngineeringWorkflowReportRequest) =>
    client.exportReportMarkdown(request);

  return {
    mode,
    state: stateQuery.data,
    isLoading: stateQuery.isLoading,
    error: stateQuery.error,
    refresh,
    validate,
    prepareCalculation,
    runCalculation,
    loadTracePreview,
    generateReport,
    exportReportJson,
    exportReportMarkdown,
  };
}
