import { apiRoutes } from "@/shared/api/apiRoutes";
import type {
  EngineeringWorkflowApiStateResponse,
  EngineeringWorkflowCalculationPreparationResult,
  EngineeringWorkflowCalculationRequest,
  EngineeringWorkflowValidationResponse,
  ProjectWorkflowState,
  WorkflowDiagnostic,
} from "../types";
import {
  buildErrorDiagnostic,
  buildWorkflowStateFallback,
  deduplicateDiagnostics,
  mapStateResponse,
} from "./workflowClientShared";
import { mapTransportError, workflowApiRequest } from "./shared/workflowTransport";

export interface WorkflowStateApi {
  getWorkflowState(projectId: number, buildingId: number): Promise<ProjectWorkflowState>;
  validateWorkflow(state: ProjectWorkflowState): Promise<WorkflowDiagnostic[]>;
  buildCalculationRequest(state: ProjectWorkflowState): EngineeringWorkflowCalculationRequest;
  prepareCalculation(
    request: EngineeringWorkflowCalculationRequest,
  ): Promise<EngineeringWorkflowCalculationPreparationResult>;
}

async function fetchState(projectId: number, buildingId: number): Promise<EngineeringWorkflowApiStateResponse> {
  return workflowApiRequest<EngineeringWorkflowApiStateResponse>(apiRoutes.engineeringWorkflow.state(projectId), {
    query: { buildingId },
  });
}

export function createWorkflowStateApi(mode: "api" | "dev"): WorkflowStateApi {
  return {
    async getWorkflowState(projectId: number, buildingId: number): Promise<ProjectWorkflowState> {
      if (mode === "dev") {
        const response = await fetchState(projectId, buildingId);

        return mapStateResponse(response, "dev", [
          {
            severity: "assumption",
            code: "WORKFLOW_DEV_ADAPTER_ACTIVE",
            message: "Frontend workflow is running in internal dev adapter mode.",
            sourceStep: "Reports",
            suggestedCorrection: "Switch VITE_ENGINEERING_WORKFLOW_MODE to api for production endpoint behavior.",
          },
        ]);
      }

      try {
        const response = await fetchState(projectId, buildingId);
        return mapStateResponse(response, "api");
      } catch (error) {
        const message = mapTransportError(error, "Engineering workflow state request failed.");
        return buildWorkflowStateFallback(projectId, buildingId, message);
      }
    },

    async validateWorkflow(state: ProjectWorkflowState): Promise<WorkflowDiagnostic[]> {
      try {
        const response = await workflowApiRequest<EngineeringWorkflowValidationResponse>(
          apiRoutes.engineeringWorkflow.validate(),
          {
            method: "POST",
            body: { state },
          },
        );

        return deduplicateDiagnostics(response.diagnostics);
      } catch (error) {
        const message = mapTransportError(error, "Workflow validation request failed.");
        return [buildErrorDiagnostic(message)];
      }
    },

    buildCalculationRequest(state: ProjectWorkflowState): EngineeringWorkflowCalculationRequest {
      return {
        projectId: state.projectId,
        buildingId: state.buildingId,
        workflowState: state,
      };
    },

    async prepareCalculation(
      request: EngineeringWorkflowCalculationRequest,
    ): Promise<EngineeringWorkflowCalculationPreparationResult> {
      try {
        return await workflowApiRequest<EngineeringWorkflowCalculationPreparationResult>(
          apiRoutes.engineeringWorkflow.prepareCalculation(),
          {
            method: "POST",
            body: {
              state: request.workflowState,
              executeCalculation: false,
            },
          },
        );
      } catch (error) {
        const message = mapTransportError(error, "Prepare calculation request failed.");
        return {
          requestId: "workflow-prepare-failed",
          status: "blocked",
          diagnostics: [buildErrorDiagnostic(message)],
          metadata: { mode: "api", error: "prepare-request-failed" },
        };
      }
    },
  };
}
