import { useMemo } from "react";
import { useMutation, useQuery } from "@tanstack/react-query";
import { queryKeys } from "@/shared/api/queryKeys";
import { createEngineeringWorkflowClient } from "../api/engineeringWorkflowClient";
import type {
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
    mutationFn: ({ calculationId, detailLevel }: { calculationId: string; detailLevel: WorkflowTraceDetailLevel }) =>
      client.getTracePreview(calculationId, detailLevel),
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
    const calculationId = stateQuery.data?.calculationTraceSummary?.calculationId ?? `building-${buildingId}`;
    return traceMutation.mutateAsync({ calculationId, detailLevel });
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
    loadTracePreview,
    generateReport,
    exportReportJson,
    exportReportMarkdown,
  };
}
