import { apiRoutes } from "@/shared/api/apiRoutes";
import type { PagedResponse } from "@/shared/api/pagedResponse";
import type {
  EngineeringCalculationArtifactKind,
  EngineeringCalculationArtifactRecord,
  EngineeringCalculationScenarioRequest,
  EngineeringCalculationScenarioRecord,
  EngineeringCalculationScenarioResponse,
} from "../types";
import { buildErrorDiagnostic, buildIdempotencyKey } from "./workflowClientShared";
import { mapTransportError, workflowApiRequest } from "./shared/workflowTransport";

export interface WorkflowScenarioApi {
  runCalculation(request: EngineeringCalculationScenarioRequest): Promise<EngineeringCalculationScenarioResponse>;
  getScenarioResult(scenarioId: string): Promise<EngineeringCalculationScenarioRecord | null>;
  listProjectScenarios(projectId: number): Promise<EngineeringCalculationScenarioRecord[]>;
  getScenarioArtifacts(scenarioId: string): Promise<EngineeringCalculationArtifactRecord[]>;
  getScenarioArtifact(
    scenarioId: string,
    artifactKind: EngineeringCalculationArtifactKind,
  ): Promise<EngineeringCalculationArtifactRecord | null>;
}

export function createWorkflowScenarioApi(): WorkflowScenarioApi {
  return {
    async runCalculation(
      request: EngineeringCalculationScenarioRequest,
    ): Promise<EngineeringCalculationScenarioResponse> {
      try {
        const idempotencyKey = buildIdempotencyKey(
          `run-${request.projectId ?? request.state.projectId ?? "unknown"}-${request.scenarioId}`,
        );

        return await workflowApiRequest<EngineeringCalculationScenarioResponse>(
          apiRoutes.engineeringWorkflow.runCalculation(),
          {
            method: "POST",
            body: request,
            headers: { "Idempotency-Key": idempotencyKey },
          },
        );
      } catch (error) {
        const message = mapTransportError(error, "Run calculation request failed.");
        return {
          scenarioId: request.scenarioId,
          status: "FailedExecution",
          executed: false,
          executedModules: [],
          skippedModules: [],
          unavailableModules: ["Runner"],
          validationDiagnostics: [buildErrorDiagnostic(message)],
          assumptions: ["Scenario runner fallback was used because API request failed."],
          warnings: ["Scenario runner request failed."],
          moduleSummaries: {},
          moduleResults: [],
          timings: [],
          calculationTraceSummary: undefined,
          reportPreview: undefined,
          reportJson: null,
          reportMarkdown: null,
          metadata: { mode: "api", error: "run-calculation-request-failed" },
        };
      }
    },

    async getScenarioResult(scenarioId: string): Promise<EngineeringCalculationScenarioRecord | null> {
      try {
        return await workflowApiRequest<EngineeringCalculationScenarioRecord>(
          apiRoutes.engineeringWorkflow.scenarioById(scenarioId),
        );
      } catch {
        return null;
      }
    },

    async listProjectScenarios(projectId: number): Promise<EngineeringCalculationScenarioRecord[]> {
      try {
        const response = await workflowApiRequest<PagedResponse<EngineeringCalculationScenarioRecord>>(
          apiRoutes.engineeringWorkflow.projectScenarios(projectId),
          {
            query: { page: 1, pageSize: 50, sortBy: "createdAtUtc", sortDescending: true },
          },
        );

        return response.items;
      } catch {
        return [];
      }
    },

    async getScenarioArtifacts(scenarioId: string): Promise<EngineeringCalculationArtifactRecord[]> {
      try {
        return await workflowApiRequest<EngineeringCalculationArtifactRecord[]>(
          apiRoutes.engineeringWorkflow.scenarioArtifacts(scenarioId),
        );
      } catch {
        return [];
      }
    },

    async getScenarioArtifact(
      scenarioId: string,
      artifactKind: EngineeringCalculationArtifactKind,
    ): Promise<EngineeringCalculationArtifactRecord | null> {
      try {
        return await workflowApiRequest<EngineeringCalculationArtifactRecord>(
          apiRoutes.engineeringWorkflow.scenarioArtifactByKind(scenarioId, artifactKind),
        );
      } catch {
        return null;
      }
    },
  };
}
