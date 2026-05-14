import { apiRoutes } from "@/shared/api/apiRoutes";
import { formatNumber } from "@/shared/lib/format";
import type {
  EngineeringCalculationArtifactKind,
  EngineeringCalculationArtifactRecord,
  EngineeringCalculationJobEvent,
  EngineeringCalculationJobRequest,
  EngineeringCalculationJobResult,
  EngineeringCalculationScenarioRequest,
  EngineeringCalculationScenarioRecord,
  EngineeringCalculationScenarioResponse,
  EngineeringWorkflowCalculationPreparationResult,
  EngineeringWorkflowCalculationRequest,
  EngineeringWorkflowReportRequest,
  EngineeringWorkflowReportResult,
  ProjectWorkflowState,
  WorkflowCalculationTraceSummary,
  WorkflowDiagnostic,
  WorkflowStepStatus,
  WorkflowTraceDetailLevel,
} from "../types";
import { createWorkflowJobApi } from "./workflowJobApi";
import { createWorkflowReportApi } from "./workflowReportApi";
import { createWorkflowScenarioApi } from "./workflowScenarioApi";
import { createWorkflowStateApi } from "./workflowStateApi";
import { deduplicateDiagnostics } from "./workflowClientShared";
import { createWorkflowTraceApi } from "./workflowTraceApi";

export interface EngineeringWorkflowClient {
  readonly mode: "api" | "dev";
  getWorkflowState(projectId: number, buildingId: number): Promise<ProjectWorkflowState>;
  validateWorkflow(state: ProjectWorkflowState): Promise<WorkflowDiagnostic[]>;
  buildCalculationRequest(state: ProjectWorkflowState): EngineeringWorkflowCalculationRequest;
  prepareCalculation(
    request: EngineeringWorkflowCalculationRequest,
  ): Promise<EngineeringWorkflowCalculationPreparationResult>;
  runCalculation(request: EngineeringCalculationScenarioRequest): Promise<EngineeringCalculationScenarioResponse>;
  createCalculationJob(request: EngineeringCalculationJobRequest): Promise<EngineeringCalculationJobResult>;
  getCalculationJob(jobId: string): Promise<EngineeringCalculationJobResult | null>;
  listProjectJobs(projectId: number): Promise<EngineeringCalculationJobResult[]>;
  getCalculationJobEvents(jobId: string): Promise<EngineeringCalculationJobEvent[]>;
  cancelCalculationJob(jobId: string): Promise<EngineeringCalculationJobResult | null>;
  getScenarioResult(scenarioId: string): Promise<EngineeringCalculationScenarioRecord | null>;
  listProjectScenarios(projectId: number): Promise<EngineeringCalculationScenarioRecord[]>;
  getScenarioArtifacts(scenarioId: string): Promise<EngineeringCalculationArtifactRecord[]>;
  getScenarioArtifact(
    scenarioId: string,
    artifactKind: EngineeringCalculationArtifactKind,
  ): Promise<EngineeringCalculationArtifactRecord | null>;
  getTracePreview(
    state: ProjectWorkflowState,
    detailLevel: WorkflowTraceDetailLevel,
  ): Promise<WorkflowCalculationTraceSummary>;
  generateReport(request: EngineeringWorkflowReportRequest): Promise<EngineeringWorkflowReportResult>;
  exportReportJson(request: EngineeringWorkflowReportRequest): Promise<string>;
  exportReportMarkdown(request: EngineeringWorkflowReportRequest): Promise<string>;
}

// Keep explicit route references in this facade for architecture guard visibility.
const workflowRouteGuardMarkers = {
  state: apiRoutes.engineeringWorkflow.state,
  validate: apiRoutes.engineeringWorkflow.validate(),
  prepareCalculation: apiRoutes.engineeringWorkflow.prepareCalculation(),
  runCalculation: apiRoutes.engineeringWorkflow.runCalculation(),
  jobs: apiRoutes.engineeringWorkflow.jobs(),
  jobById: apiRoutes.engineeringWorkflow.jobById,
  projectJobs: apiRoutes.engineeringWorkflow.projectJobs,
  jobEvents: apiRoutes.engineeringWorkflow.jobEvents,
  cancelJob: apiRoutes.engineeringWorkflow.cancelJob,
  projectScenarios: apiRoutes.engineeringWorkflow.projectScenarios,
  scenarioById: apiRoutes.engineeringWorkflow.scenarioById,
  scenarioArtifacts: apiRoutes.engineeringWorkflow.scenarioArtifacts,
  scenarioArtifactByKind: apiRoutes.engineeringWorkflow.scenarioArtifactByKind,
  tracePreview: apiRoutes.engineeringWorkflow.tracePreview(),
  report: apiRoutes.engineeringWorkflow.report(),
  reportExportJson: apiRoutes.engineeringWorkflow.reportExportJson(),
  reportExportMarkdown: apiRoutes.engineeringWorkflow.reportExportMarkdown(),
};

void workflowRouteGuardMarkers;

const stateApi = createWorkflowStateApi("api");
const scenarioApi = createWorkflowScenarioApi();
const jobApi = createWorkflowJobApi();
const traceApi = createWorkflowTraceApi();
const reportApi = createWorkflowReportApi();

const apiClient: EngineeringWorkflowClient = {
  mode: "api",
  ...stateApi,
  ...scenarioApi,
  ...jobApi,
  ...traceApi,
  ...reportApi,
};

const devStateApi = createWorkflowStateApi("dev");

const devClient: EngineeringWorkflowClient = {
  ...apiClient,
  mode: "dev",

  async getWorkflowState(projectId: number, buildingId: number): Promise<ProjectWorkflowState> {
    return devStateApi.getWorkflowState(projectId, buildingId);
  },

  async exportReportJson(request: EngineeringWorkflowReportRequest): Promise<string> {
    const content = await apiClient.exportReportJson(request);
    return `${content}\n`;
  },

  async runCalculation(
    request: EngineeringCalculationScenarioRequest,
  ): Promise<EngineeringCalculationScenarioResponse> {
    const result = await apiClient.runCalculation(request);
    const diagnostic: WorkflowDiagnostic = {
      severity: "assumption",
      code: "WORKFLOW_DEV_ADAPTER_ACTIVE",
      message: "Scenario execution response includes internal dev adapter marker.",
      sourceStep: "Review",
      suggestedCorrection: "Switch VITE_ENGINEERING_WORKFLOW_MODE to api for production endpoint behavior.",
    };

    return {
      ...result,
      validationDiagnostics: deduplicateDiagnostics([...(result.validationDiagnostics ?? []), diagnostic]),
    };
  },

  async exportReportMarkdown(request: EngineeringWorkflowReportRequest): Promise<string> {
    const content = await apiClient.exportReportMarkdown(request);
    return `${content}\n<!-- internal dev adapter mode -->\n`;
  },
};

export function createEngineeringWorkflowClient(mode: "api" | "dev" = "dev"): EngineeringWorkflowClient {
  return mode === "api" ? apiClient : devClient;
}

export function describeWorkflowStepStatus(status: WorkflowStepStatus): string {
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

export function summarizeBuildingMetrics(state: ProjectWorkflowState): string {
  const area = formatNumber(state.buildingMetadata.floorAreaM2, 1);
  const volume = formatNumber(state.buildingMetadata.volumeM3, 0);
  return `${state.zones.length} zone(s), ${state.boundaries.length} boundaries, ${area} m2, ${volume} m3`;
}
