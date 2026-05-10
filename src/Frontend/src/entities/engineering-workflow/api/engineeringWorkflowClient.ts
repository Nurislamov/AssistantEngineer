import { apiRoutes } from "@/shared/api/apiRoutes";
import { apiRequest } from "@/shared/api/httpClient";
import { formatNumber } from "@/shared/lib/format";
import type {
  EngineeringWorkflowApiStateResponse,
  EngineeringWorkflowCalculationPreparationResult,
  EngineeringWorkflowCalculationRequest,
  EngineeringWorkflowReportExportResponse,
  EngineeringWorkflowReportRequest,
  EngineeringWorkflowReportResponse,
  EngineeringWorkflowReportResult,
  EngineeringWorkflowTracePreviewResponse,
  EngineeringWorkflowValidationResponse,
  ProjectWorkflowState,
  WorkflowCalculationTraceSummary,
  WorkflowDiagnostic,
  WorkflowStepCompletion,
  WorkflowStepStatus,
  WorkflowTraceDetailLevel,
} from "../types";

function buildErrorDiagnostic(message: string): WorkflowDiagnostic {
  return {
    severity: "error",
    code: "WORKFLOW_API_ERROR",
    message,
    sourceStep: "Validation",
    suggestedCorrection: "Check backend API availability and retry workflow request.",
  };
}

export interface EngineeringWorkflowClient {
  readonly mode: "api" | "dev";
  getWorkflowState(projectId: number, buildingId: number): Promise<ProjectWorkflowState>;
  validateWorkflow(state: ProjectWorkflowState): Promise<WorkflowDiagnostic[]>;
  buildCalculationRequest(state: ProjectWorkflowState): EngineeringWorkflowCalculationRequest;
  prepareCalculation(
    request: EngineeringWorkflowCalculationRequest,
  ): Promise<EngineeringWorkflowCalculationPreparationResult>;
  getTracePreview(
    state: ProjectWorkflowState,
    detailLevel: WorkflowTraceDetailLevel,
  ): Promise<WorkflowCalculationTraceSummary>;
  generateReport(request: EngineeringWorkflowReportRequest): Promise<EngineeringWorkflowReportResult>;
  exportReportJson(request: EngineeringWorkflowReportRequest): Promise<string>;
  exportReportMarkdown(request: EngineeringWorkflowReportRequest): Promise<string>;
}

function deduplicateDiagnostics(diagnostics: WorkflowDiagnostic[]): WorkflowDiagnostic[] {
  const seen = new Set<string>();
  return diagnostics
    .slice()
    .sort((left, right) => {
      const severityDelta = severityRank(right.severity) - severityRank(left.severity);
      if (severityDelta !== 0) {
        return severityDelta;
      }

      const stepDelta = left.sourceStep.localeCompare(right.sourceStep, "en-US");
      if (stepDelta !== 0) {
        return stepDelta;
      }

      const codeDelta = left.code.localeCompare(right.code, "en-US");
      if (codeDelta !== 0) {
        return codeDelta;
      }

      return left.message.localeCompare(right.message, "en-US");
    })
    .filter((item) => {
      const key = `${item.sourceStep}|${item.code}|${item.message}|${item.targetField ?? ""}`;
      if (seen.has(key)) {
        return false;
      }

      seen.add(key);
      return true;
    });
}

function severityRank(severity: WorkflowDiagnostic["severity"]): number {
  if (severity === "error") {
    return 4;
  }

  if (severity === "warning") {
    return 3;
  }

  if (severity === "assumption") {
    return 2;
  }

  return 1;
}

function mapStateResponse(
  response: EngineeringWorkflowApiStateResponse,
  mode: "api" | "dev",
  extraDiagnostics: WorkflowDiagnostic[] = [],
): ProjectWorkflowState {
  const diagnostics = deduplicateDiagnostics([...response.diagnostics, ...extraDiagnostics]);
  const completions: WorkflowStepCompletion[] = (response.steps ?? []).map((item) => ({
    step: item.kind,
    status: item.status,
  }));

  return {
    projectId: response.projectId,
    projectName: response.projectName,
    buildingId: response.buildingId ?? undefined,
    availableModules: response.availableModules,
    assumptions: response.assumptions,
    links: response.links,
    metadata: response.metadata,
    buildingMetadata: response.buildingMetadata,
    zones: response.zones,
    boundaries: response.boundaries,
    weatherSolarSettings: response.weatherSolarSettings,
    ventilationSettings: response.ventilationSettings,
    groundSettings: response.groundSettings,
    domesticHotWaterSettings: response.domesticHotWaterSettings,
    systemEnergySettings: response.systemEnergySettings,
    validationDiagnostics: diagnostics,
    calculationTraceSummary: response.calculationTraceSummary ?? undefined,
    reportSummary: response.reportSummary ?? undefined,
    currentStep: response.currentStep,
    completionByStep: completions,
    workflowMode: mode,
    workflowModeLabel:
      mode === "api"
        ? "API workflow mode (real backend endpoints)"
        : "Internal dev adapter mode (non-production behavior is explicit)",
  };
}

async function fetchState(projectId: number, buildingId: number): Promise<EngineeringWorkflowApiStateResponse> {
  return apiRequest<EngineeringWorkflowApiStateResponse>(apiRoutes.engineeringWorkflow.state(projectId), {
    query: { buildingId },
  });
}

const apiClient: EngineeringWorkflowClient = {
  mode: "api",

  async getWorkflowState(projectId: number, buildingId: number): Promise<ProjectWorkflowState> {
    try {
      const response = await fetchState(projectId, buildingId);
      return mapStateResponse(response, "api");
    } catch (error) {
      const message = error instanceof Error ? error.message : "Engineering workflow state request failed.";
      return {
        projectId,
        projectName: `Project #${projectId}`,
        buildingId,
        availableModules: [],
        assumptions: ["Workflow state fallback is used because API request failed."],
        links: [],
        metadata: { mode: "api", error: "state-request-failed" },
        buildingMetadata: { buildingName: "n/a", locationText: "n/a" },
        zones: [],
        boundaries: [],
        weatherSolarSettings: {
          weatherSourceStatus: "Unavailable",
          locationTimezoneSummary: "n/a",
          solarChainReadinessSummary: "n/a",
        },
        ventilationSettings: {
          openingCount: 0,
          controlModeSummary: "Unavailable",
          airflowSummary: "Unavailable",
          warnings: [],
        },
        groundSettings: {
          groundBoundaryCount: 0,
          groundProfileMode: "Unavailable",
          summaryStatus: "incomplete",
        },
        domesticHotWaterSettings: {
          demandBasis: "Unavailable",
          usefulDemandSummary: "Unavailable",
          lossesSummary: "Unavailable",
          ownershipPolicy: "Unavailable",
        },
        systemEnergySettings: {
          usesSummary: "Unavailable",
          carriersSummary: "Unavailable",
          finalPrimaryCarbonSummary: "Unavailable",
        },
        validationDiagnostics: [buildErrorDiagnostic(message)],
        currentStep: "Project",
        completionByStep: [{ step: "Project", status: "errors" }],
        workflowMode: "api",
        workflowModeLabel: "API workflow mode (real backend endpoints)",
      };
    }
  },

  async validateWorkflow(state: ProjectWorkflowState): Promise<WorkflowDiagnostic[]> {
    try {
      const response = await apiRequest<EngineeringWorkflowValidationResponse>(
        apiRoutes.engineeringWorkflow.validate(),
        {
          method: "POST",
          body: { state },
        },
      );

      return deduplicateDiagnostics(response.diagnostics);
    } catch (error) {
      return [buildErrorDiagnostic(error instanceof Error ? error.message : "Workflow validation request failed.")];
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
      return await apiRequest<EngineeringWorkflowCalculationPreparationResult>(
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
      return {
        requestId: "workflow-prepare-failed",
        status: "blocked",
        diagnostics: [buildErrorDiagnostic(error instanceof Error ? error.message : "Prepare calculation request failed.")],
        metadata: { mode: "api", error: "prepare-request-failed" },
      };
    }
  },

  async getTracePreview(
    state: ProjectWorkflowState,
    detailLevel: WorkflowTraceDetailLevel,
  ): Promise<WorkflowCalculationTraceSummary> {
    try {
      const response = await apiRequest<EngineeringWorkflowTracePreviewResponse>(
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

  async generateReport(request: EngineeringWorkflowReportRequest): Promise<EngineeringWorkflowReportResult> {
    try {
      const response = await apiRequest<EngineeringWorkflowReportResponse>(
        apiRoutes.engineeringWorkflow.report(),
        {
          method: "POST",
          body: {
            state: request.state,
            reportKind: request.reportKind,
            requestedFormat: request.format,
            detailLevel: request.state.calculationTraceSummary?.detailLevel ?? "Standard",
            includeTraceAppendix: request.includeTraceAppendix,
            includeLimitations: request.includeLimitations,
          },
        },
      );

      return {
        preview: response.preview,
        diagnostics: deduplicateDiagnostics(response.diagnostics),
        json: undefined,
        markdown: undefined,
      };
    } catch (error) {
      return {
        preview: {
          reportKind: request.reportKind,
          title: "Report generation failed",
          sections: [],
          warningsCount: 0,
          diagnosticsCount: 1,
          exportFormatsAvailable: ["Json", "Markdown"],
          generatedTimestamp: new Date().toISOString(),
          limitations: ["Report generation fallback due to API request failure."],
        },
        diagnostics: [buildErrorDiagnostic(error instanceof Error ? error.message : "Report generation request failed.")],
      };
    }
  },

  async exportReportJson(request: EngineeringWorkflowReportRequest): Promise<string> {
    try {
      const response = await apiRequest<EngineeringWorkflowReportExportResponse>(
        apiRoutes.engineeringWorkflow.reportExportJson(),
        {
          method: "POST",
          body: {
            request: {
              state: request.state,
              reportKind: request.reportKind,
              requestedFormat: "Json",
              detailLevel: request.state.calculationTraceSummary?.detailLevel ?? "Standard",
              includeTraceAppendix: request.includeTraceAppendix,
              includeLimitations: request.includeLimitations,
            },
          },
        },
      );

      return response.content;
    } catch (error) {
      return JSON.stringify(
        {
          schemaVersion: "workflow-report-export-error-v1",
          error: error instanceof Error ? error.message : "JSON export request failed.",
        },
        null,
        2,
      );
    }
  },

  async exportReportMarkdown(request: EngineeringWorkflowReportRequest): Promise<string> {
    try {
      const response = await apiRequest<EngineeringWorkflowReportExportResponse>(
        apiRoutes.engineeringWorkflow.reportExportMarkdown(),
        {
          method: "POST",
          body: {
            request: {
              state: request.state,
              reportKind: request.reportKind,
              requestedFormat: "Markdown",
              detailLevel: request.state.calculationTraceSummary?.detailLevel ?? "Standard",
              includeTraceAppendix: request.includeTraceAppendix,
              includeLimitations: request.includeLimitations,
            },
          },
        },
      );

      return response.content;
    } catch (error) {
      const message = error instanceof Error ? error.message : "Markdown export request failed.";
      return `# Report export error\n\n- ${message}\n`;
    }
  },
};

const devClient: EngineeringWorkflowClient = {
  ...apiClient,
  mode: "dev",

  async getWorkflowState(projectId: number, buildingId: number): Promise<ProjectWorkflowState> {
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
  },

  async exportReportJson(request: EngineeringWorkflowReportRequest): Promise<string> {
    const content = await apiClient.exportReportJson(request);
    return `${content}\n`;
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
