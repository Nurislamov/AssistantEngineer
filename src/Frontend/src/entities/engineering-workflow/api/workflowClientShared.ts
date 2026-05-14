import type {
  EngineeringWorkflowApiStateResponse,
  ProjectWorkflowState,
  WorkflowDiagnostic,
  WorkflowStepCompletion,
} from "../types";

export function buildErrorDiagnostic(message: string): WorkflowDiagnostic {
  return {
    severity: "error",
    code: "WORKFLOW_API_ERROR",
    message,
    sourceStep: "Validation",
    suggestedCorrection: "Check backend API availability and retry workflow request.",
  };
}

export function buildIdempotencyKey(seed: string): string {
  const normalized = seed.trim().replace(/\s+/g, "-").toLowerCase();
  return `wf-${normalized.slice(0, 120)}`;
}

export function mapErrorMessage(error: unknown, fallback: string): string {
  return error instanceof Error ? error.message : fallback;
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

export function deduplicateDiagnostics(diagnostics: WorkflowDiagnostic[]): WorkflowDiagnostic[] {
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

export function mapStateResponse(
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

export function buildWorkflowStateFallback(
  projectId: number,
  buildingId: number,
  message: string,
): ProjectWorkflowState {
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
