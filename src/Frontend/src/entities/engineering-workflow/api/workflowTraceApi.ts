import { apiRoutes } from "@/shared/api/apiRoutes";
import type {
  EngineeringWorkflowTracePreviewResponse,
  ProjectWorkflowState,
  WorkflowCalculationTraceSummary,
  WorkflowTraceDetailLevel,
} from "../types";
import { workflowApiRequest } from "./shared/workflowTransport";

export interface WorkflowTraceApi {
  getTracePreview(
    state: ProjectWorkflowState,
    detailLevel: WorkflowTraceDetailLevel,
  ): Promise<WorkflowCalculationTraceSummary>;
}

export function createWorkflowTraceApi(): WorkflowTraceApi {
  return {
    async getTracePreview(
      state: ProjectWorkflowState,
      detailLevel: WorkflowTraceDetailLevel,
    ): Promise<WorkflowCalculationTraceSummary> {
      try {
        const response = await workflowApiRequest<EngineeringWorkflowTracePreviewResponse>(
          apiRoutes.engineeringWorkflow.tracePreview(),
          {
            method: "POST",
            body: {
              state,
              detailLevel,
            },
          },
        );

        return response.traceSummary;
      } catch {
        return {
          traceId: "workflow-trace-preview-failed",
          calculationId: state.buildingId?.toString(),
          detailLevel,
          modules: ["Validation"],
          assumptions: ["Trace preview fallback is used because API request failed."],
          warnings: ["Trace preview request failed."],
          steps: [],
        };
      }
    },
  };
}
