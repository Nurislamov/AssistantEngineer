import { apiRoutes } from "@/shared/api/apiRoutes";
import type { PagedResponse } from "@/shared/api/pagedResponse";
import type {
  EngineeringCalculationJobEvent,
  EngineeringCalculationJobRequest,
  EngineeringCalculationJobResult,
} from "../types";
import { buildErrorDiagnostic, buildIdempotencyKey } from "./workflowClientShared";
import { mapTransportError, workflowApiRequest } from "./shared/workflowTransport";

export interface WorkflowJobApi {
  createCalculationJob(request: EngineeringCalculationJobRequest): Promise<EngineeringCalculationJobResult>;
  getCalculationJob(jobId: string): Promise<EngineeringCalculationJobResult | null>;
  listProjectJobs(projectId: number): Promise<EngineeringCalculationJobResult[]>;
  getCalculationJobEvents(jobId: string): Promise<EngineeringCalculationJobEvent[]>;
  cancelCalculationJob(jobId: string): Promise<EngineeringCalculationJobResult | null>;
}

export function createWorkflowJobApi(): WorkflowJobApi {
  return {
    async createCalculationJob(
      request: EngineeringCalculationJobRequest,
    ): Promise<EngineeringCalculationJobResult> {
      try {
        const idempotencyKey = buildIdempotencyKey(
          `job-${request.projectId}-${request.jobId ?? request.scenarioId ?? request.scenarioRequest.scenarioId}`,
        );

        return await workflowApiRequest<EngineeringCalculationJobResult>(
          apiRoutes.engineeringWorkflow.jobs(),
          {
            method: "POST",
            body: request,
            headers: { "Idempotency-Key": idempotencyKey },
          },
        );
      } catch (error) {
        const message = mapTransportError(error, "Create calculation job request failed.");
        return {
          jobId: request.jobId ?? `job-${request.scenarioId ?? request.scenarioRequest.scenarioId}`,
          projectId: request.projectId,
          scenarioId: request.scenarioId ?? request.scenarioRequest.scenarioId,
          status: "FailedExecution",
          progressPercent: 100,
          currentStep: "Failed",
          queuedAtUtc: new Date().toISOString(),
          completedAtUtc: new Date().toISOString(),
          diagnostics: [buildErrorDiagnostic(message)],
          assumptions: ["Job fallback response was returned because API request failed."],
          warnings: ["Job request failed."],
          persistedArtifactReferences: [],
          historyEvents: [],
          metadata: { mode: "api", error: "job-request-failed" },
        };
      }
    },

    async getCalculationJob(jobId: string): Promise<EngineeringCalculationJobResult | null> {
      try {
        return await workflowApiRequest<EngineeringCalculationJobResult>(
          apiRoutes.engineeringWorkflow.jobById(jobId),
        );
      } catch {
        return null;
      }
    },

    async listProjectJobs(projectId: number): Promise<EngineeringCalculationJobResult[]> {
      try {
        const response = await workflowApiRequest<PagedResponse<EngineeringCalculationJobResult>>(
          apiRoutes.engineeringWorkflow.projectJobs(projectId),
          {
            query: { page: 1, pageSize: 50, sortBy: "queuedAtUtc", sortDescending: true },
          },
        );

        return response.items;
      } catch {
        return [];
      }
    },

    async getCalculationJobEvents(jobId: string): Promise<EngineeringCalculationJobEvent[]> {
      try {
        return await workflowApiRequest<EngineeringCalculationJobEvent[]>(
          apiRoutes.engineeringWorkflow.jobEvents(jobId),
        );
      } catch {
        return [];
      }
    },

    async cancelCalculationJob(jobId: string): Promise<EngineeringCalculationJobResult | null> {
      try {
        return await workflowApiRequest<EngineeringCalculationJobResult>(
          apiRoutes.engineeringWorkflow.cancelJob(jobId),
          {
            method: "POST",
          },
        );
      } catch {
        return null;
      }
    },
  };
}
