import { buildingsApi } from "@/entities/building/api/buildingsApi";
import type { BuildingValidationIssueApiResponse } from "@/entities/building/types";
import { calculationsApi } from "@/entities/calculation/api/calculationsApi";
import { floorsApi } from "@/entities/floor/api/floorsApi";
import { roomsApi, thermalZonesApi } from "@/entities/room/api/roomsApi";
import type { RoomDto, WallBoundaryTypeDto } from "@/entities/room/types";
import { formatNumber } from "@/shared/lib/format";
import type {
  EngineeringWorkflowCalculationPreparationResult,
  EngineeringWorkflowCalculationRequest,
  EngineeringWorkflowReportRequest,
  EngineeringWorkflowReportResult,
  ProjectWorkflowState,
  WorkflowBoundarySummary,
  WorkflowCalculationTraceSummary,
  WorkflowDiagnostic,
  WorkflowDiagnosticSeverity,
  WorkflowReportPreview,
  WorkflowStepCompletion,
  WorkflowStepKind,
  WorkflowStepStatus,
  WorkflowTraceDetailLevel,
} from "../types";

export interface EngineeringWorkflowClient {
  readonly mode: "api" | "dev";
  getWorkflowState(projectId: number, buildingId: number): Promise<ProjectWorkflowState>;
  validateWorkflow(state: ProjectWorkflowState): Promise<WorkflowDiagnostic[]>;
  buildCalculationRequest(state: ProjectWorkflowState): EngineeringWorkflowCalculationRequest;
  prepareCalculation(
    request: EngineeringWorkflowCalculationRequest,
  ): Promise<EngineeringWorkflowCalculationPreparationResult>;
  getTracePreview(
    calculationId: string,
    detailLevel: WorkflowTraceDetailLevel,
  ): Promise<WorkflowCalculationTraceSummary>;
  generateReport(request: EngineeringWorkflowReportRequest): Promise<EngineeringWorkflowReportResult>;
  exportReportJson(request: EngineeringWorkflowReportRequest): Promise<string>;
  exportReportMarkdown(request: EngineeringWorkflowReportRequest): Promise<string>;
}

const workflowSteps: WorkflowStepKind[] = [
  "Project",
  "Building",
  "Zones",
  "Envelope",
  "WeatherSolar",
  "Ventilation",
  "Ground",
  "DomesticHotWater",
  "SystemEnergy",
  "Validation",
  "CalculationTrace",
  "Reports",
  "Review",
];

function normalizeSeverity(value: string): WorkflowDiagnosticSeverity {
  const lower = value.trim().toLowerCase();
  if (lower.includes("error")) {
    return "error";
  }

  if (lower.includes("warn")) {
    return "warning";
  }

  if (lower.includes("assumption")) {
    return "assumption";
  }

  return "info";
}

function mapValidationIssue(
  issue: BuildingValidationIssueApiResponse,
  sourceStep: WorkflowStepKind,
): WorkflowDiagnostic {
  return {
    severity: normalizeSeverity(issue.severity),
    code: issue.code,
    message: issue.message,
    sourceStep,
    sourceModule: issue.entityType ?? undefined,
    targetField: issue.entityId ? `${issue.entityType ?? "entity"}:${issue.entityId}` : undefined,
  };
}

function deduplicateDiagnostics(diagnostics: WorkflowDiagnostic[]): WorkflowDiagnostic[] {
  const seen = new Set<string>();
  return diagnostics
    .slice()
    .sort((left, right) => {
      const severityOrder = severityRank(right.severity) - severityRank(left.severity);
      if (severityOrder !== 0) {
        return severityOrder;
      }

      const source = left.sourceStep.localeCompare(right.sourceStep, "en-US");
      if (source !== 0) {
        return source;
      }

      const code = left.code.localeCompare(right.code, "en-US");
      if (code !== 0) {
        return code;
      }

      return left.message.localeCompare(right.message, "en-US");
    })
    .filter((diagnostic) => {
      const key = `${diagnostic.sourceStep}|${diagnostic.code}|${diagnostic.message}`;
      if (seen.has(key)) {
        return false;
      }

      seen.add(key);
      return true;
    });
}

function severityRank(severity: WorkflowDiagnosticSeverity): number {
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

function toBoundaryIndicator(boundaryType: WallBoundaryTypeDto): WorkflowBoundarySummary["indicator"] {
  if (boundaryType === "Ground") {
    return "ground";
  }

  if (boundaryType === "External") {
    return "exterior";
  }

  if (boundaryType === "Adiabatic") {
    return "adiabatic";
  }

  return "adjacent";
}

function formatBoundaryKind(boundaryType: WallBoundaryTypeDto): string {
  switch (boundaryType) {
    case "AdjacentConditioned":
      return "Adjacent conditioned";
    case "AdjacentUnconditioned":
      return "Adjacent unconditioned";
    default:
      return boundaryType;
  }
}

function computeStepCompletions(state: Omit<ProjectWorkflowState, "completionByStep">): WorkflowStepCompletion[] {
  const diagnosticsByStep = new Map<WorkflowStepKind, WorkflowDiagnostic[]>();
  for (const diagnostic of state.validationDiagnostics) {
    const bucket = diagnosticsByStep.get(diagnostic.sourceStep) ?? [];
    bucket.push(diagnostic);
    diagnosticsByStep.set(diagnostic.sourceStep, bucket);
  }

  return workflowSteps.map((step) => {
    const stepDiagnostics = diagnosticsByStep.get(step) ?? [];
    const hasError = stepDiagnostics.some((item) => item.severity === "error");
    const hasWarning = stepDiagnostics.some((item) => item.severity === "warning");

    let status: WorkflowStepStatus = "ready";

    if (step === "Project" && !state.projectId) {
      status = "incomplete";
    } else if (step === "Building" && !state.buildingId) {
      status = "incomplete";
    } else if (step === "Zones" && state.zones.length === 0) {
      status = "incomplete";
    } else if (step === "Envelope" && state.boundaries.length === 0) {
      status = "incomplete";
    } else if (step === "Ground" && state.groundSettings.groundBoundaryCount === 0) {
      status = "incomplete";
    } else if (step === "CalculationTrace" && !state.calculationTraceSummary) {
      status = "incomplete";
    } else if (step === "Reports" && !state.reportSummary) {
      status = "incomplete";
    }

    if (hasError) {
      status = "errors";
    } else if (hasWarning && status !== "incomplete") {
      status = "warnings";
    } else if (status === "ready") {
      status = "valid";
    }

    return {
      step,
      status,
    };
  });
}

function buildBasicTraceSummary(
  buildingId: number,
  detailLevel: WorkflowTraceDetailLevel,
  diagnostics: WorkflowDiagnostic[],
): WorkflowCalculationTraceSummary {
  const modules = [
    "Weather",
    "Solar",
    "ThermalTopology",
    "Ventilation",
    "Ground",
    "DomesticHotWater",
    "SystemEnergy",
    "Validation",
    "Reporting",
  ];

  const warnings = diagnostics
    .filter((item) => item.severity === "warning")
    .map((item) => `${item.code}: ${item.message}`)
    .slice(0, 6);

  const assumptions = [
    "Trace summary is generated from available workflow data and backend diagnostics.",
    "Detailed module-level trace export endpoint is not wired yet in this frontend foundation.",
  ];

  return {
    traceId: `workflow-trace-${buildingId}`,
    calculationId: `building-${buildingId}`,
    detailLevel,
    modules,
    assumptions,
    warnings,
    steps: [
      {
        stepId: "weather-solar",
        moduleKind: "Weather",
        stepName: "Weather and solar readiness",
        sequence: 1,
        assumptions: ["Weather source is inferred from engineering core status endpoint."],
        warnings,
        diagnosticsCount: diagnostics.length,
      },
      {
        stepId: "thermal-topology",
        moduleKind: "ThermalTopology",
        stepName: "Zones and envelope summary",
        sequence: 2,
        assumptions: ["Topology summary is based on zones and boundaries available in workspace."],
        warnings: [],
        diagnosticsCount: diagnostics.filter((item) => item.sourceStep === "Zones" || item.sourceStep === "Envelope").length,
      },
      {
        stepId: "report-ready",
        moduleKind: "Reporting",
        stepName: "Report preview preparation",
        sequence: 3,
        assumptions: ["Report preview uses current frontend workflow summary data."],
        warnings: [],
        diagnosticsCount: diagnostics.filter((item) => item.sourceStep === "Reports").length,
      },
    ],
  };
}

function buildPreview(
  request: EngineeringWorkflowReportRequest,
  diagnostics: WorkflowDiagnostic[],
): WorkflowReportPreview {
  const sectionTitles = [
    "Executive summary",
    "Input summary",
    "Assumptions",
    "Warnings",
    "Validation diagnostics",
    "Weather and solar",
    "Thermal zones",
    "Envelope and boundaries",
    "Natural ventilation",
    "Ground boundaries",
    "Domestic hot water",
    "System energy",
    "Limitations",
  ];

  if (request.includeTraceAppendix) {
    sectionTitles.push("Calculation trace appendix");
  }

  return {
    reportKind: request.reportKind,
    title: `${request.reportKind} workflow preview`,
    sections: sectionTitles,
    warningsCount: diagnostics.filter((item) => item.severity === "warning").length,
    diagnosticsCount: diagnostics.length,
    exportFormatsAvailable: ["Json", "Markdown"],
    generatedTimestamp: new Date().toISOString(),
    limitations: [
      "Frontend workflow is foundation-level and may use internal dev adapters for unavailable endpoints.",
      "Report preview summarizes current internal engineering calculations only.",
      "Report preview is not a legal compliance certificate.",
      "Report preview is not external validation evidence.",
      "Report preview does not prove full standard compliance.",
    ],
  };
}

function buildReportJson(request: EngineeringWorkflowReportRequest, diagnostics: WorkflowDiagnostic[]): string {
  const preview = buildPreview(request, diagnostics);

  const payload = {
    schemaVersion: "workflow-report-v1",
    adapterMode: request.state.workflowMode,
    reportKind: request.reportKind,
    format: request.format,
    includeTraceAppendix: request.includeTraceAppendix,
    includeLimitations: request.includeLimitations,
    metadata: {
      projectId: request.state.projectId ?? null,
      buildingId: request.state.buildingId ?? null,
      generatedAt: preview.generatedTimestamp,
    },
    buildingMetadata: request.state.buildingMetadata,
    sections: preview.sections,
    warnings: diagnostics.filter((item) => item.severity === "warning"),
    diagnostics,
    assumptions: request.state.calculationTraceSummary?.assumptions ?? [],
    traceSummary: request.state.calculationTraceSummary ?? null,
    limitations: preview.limitations,
  };

  return JSON.stringify(payload, null, 2);
}

function buildReportMarkdown(request: EngineeringWorkflowReportRequest, diagnostics: WorkflowDiagnostic[]): string {
  const preview = buildPreview(request, diagnostics);
  const state = request.state;

  const lines: string[] = [
    `# ${preview.title}`,
    "",
    `- Report kind: ${request.reportKind}`,
    `- Format: ${request.format}`,
    `- Workflow mode: ${state.workflowModeLabel}`,
    `- Project: ${state.buildingMetadata.projectName ?? "n/a"}`,
    `- Building: ${state.buildingMetadata.buildingName ?? "n/a"}`,
    "",
    "## Sections",
    "",
    ...preview.sections.map((section) => `- ${section}`),
    "",
    "## Validation diagnostics",
    "",
  ];

  if (diagnostics.length === 0) {
    lines.push("No diagnostics.");
  } else {
    diagnostics.forEach((diagnostic) => {
      lines.push(
        `- [${diagnostic.severity.toUpperCase()}] ${diagnostic.code} (${diagnostic.sourceStep}): ${diagnostic.message}`,
      );
    });
  }

  lines.push("", "## Limitations", "");
  preview.limitations.forEach((item) => lines.push(`- ${item}`));

  if (request.includeTraceAppendix) {
    lines.push("", "## Calculation trace appendix", "");

    if (!state.calculationTraceSummary) {
      lines.push("Trace summary was not provided by backend endpoint for this run.");
    } else {
      lines.push(`- Trace id: ${state.calculationTraceSummary.traceId ?? "n/a"}`);
      lines.push(`- Detail level: ${state.calculationTraceSummary.detailLevel}`);
      lines.push(`- Modules: ${state.calculationTraceSummary.modules.join(", ")}`);
      lines.push("");
      lines.push("| Sequence | Module | Step | Diagnostics |", "|---|---|---|---|");
      state.calculationTraceSummary.steps.forEach((step) => {
        lines.push(`| ${step.sequence} | ${step.moduleKind} | ${step.stepName} | ${step.diagnosticsCount} |`);
      });
    }
  }

  return `${lines.join("\n")}\n`;
}

async function getStateFromApi(projectId: number, buildingId: number): Promise<ProjectWorkflowState> {
  const diagnostics: WorkflowDiagnostic[] = [];

  const [building, floors, thermalZones, coreStatus, readiness, validation] = await Promise.all([
    buildingsApi.getById(buildingId),
    floorsApi.getByBuildingId(buildingId),
    thermalZonesApi.getByBuilding(buildingId),
    calculationsApi.getEngineeringCoreV1Status(),
    buildingsApi.getReadiness(buildingId),
    buildingsApi.getValidation(buildingId),
  ]);

  const rooms = await roomsApi.getByBuilding(buildingId, floors);

  const roomDetails = await Promise.all(
    rooms.map(async (room) => {
      const [walls, windows, ventilation, groundContact] = await Promise.all([
        roomsApi.getWalls(room.id),
        roomsApi.getWindows(room.id),
        roomsApi.getVentilation(room.id),
        roomsApi.getGroundContact(room.id),
      ]);

      return {
        room,
        walls,
        windows,
        ventilation,
        groundContact,
      };
    }),
  );

  const boundaries: WorkflowBoundarySummary[] = roomDetails
    .flatMap(({ room, walls }) =>
      walls.map((wall) => ({
        id: wall.id,
        zoneOrRoomName: room.name,
        exposureKind: formatBoundaryKind(wall.boundaryType),
        areaM2: wall.areaM2,
        uValue: wall.uValue,
        adjacentZoneReference: wall.adjacentRoomId ? `Room ${wall.adjacentRoomId}` : undefined,
        indicator: toBoundaryIndicator(wall.boundaryType),
        validationStatus: (wall.boundaryType === "AdjacentUnconditioned"
          ? "warnings"
          : "valid") as WorkflowStepStatus,
      })),
    )
    .sort((left, right) => `${left.zoneOrRoomName}-${left.id}`.localeCompare(`${right.zoneOrRoomName}-${right.id}`, "en-US"));

  const floorAreaM2 = rooms.reduce((sum, room) => sum + room.area, 0);
  const volumeM3 = rooms.reduce((sum, room) => sum + (room.volume ?? room.area * (room.height ?? 3)), 0);

  const ventilationConfiguredCount = roomDetails.filter((item) => item.ventilation !== null).length;
  const openingsCount = roomDetails.reduce((sum, item) => sum + item.windows.length, 0);
  const groundBoundaryCount = roomDetails.filter((item) => item.groundContact !== null).length;

  diagnostics.push(...readiness.issues.map((issue) => mapValidationIssue(issue, "Building")));
  diagnostics.push(...validation.issues.map((issue) => mapValidationIssue(issue, "Validation")));

  if (groundBoundaryCount === 0) {
    diagnostics.push({
      severity: "warning",
      code: "WORKFLOW_GROUND_NOT_SET",
      message: "Ground boundary data is not configured for any room.",
      sourceStep: "Ground",
      suggestedCorrection: "Open Ground step and configure room ground contact for at least one room.",
      targetField: "groundSettings.groundBoundaryCount",
    });
  }

  diagnostics.push({
    severity: "assumption",
    code: "WORKFLOW_DHW_DEV_SUMMARY",
    message: "DHW summary is shown as workflow placeholder until dedicated building-level endpoint is wired.",
    sourceStep: "DomesticHotWater",
    suggestedCorrection: "Wire backend DHW summary endpoint for building-level aggregation.",
  });

  diagnostics.push({
    severity: "assumption",
    code: "WORKFLOW_SYSTEM_ENERGY_DEV_SUMMARY",
    message: "System energy summary is shown as workflow placeholder until dedicated endpoint is wired.",
    sourceStep: "SystemEnergy",
    suggestedCorrection: "Wire backend system energy summary endpoint for building-level aggregation.",
  });

  diagnostics.push({
    severity: "assumption",
    code: "WORKFLOW_TRACE_ENDPOINT_PENDING",
    message: "Detailed calculation trace endpoint is not wired; workflow shows compact summary only.",
    sourceStep: "CalculationTrace",
    suggestedCorrection: "Expose trace retrieval endpoint to switch from summary to full trace.",
  });

  diagnostics.push({
    severity: "assumption",
    code: "WORKFLOW_REPORT_ENDPOINT_PENDING",
    message: "Engineering report export endpoint is not wired; workflow export uses internal foundation serializer.",
    sourceStep: "Reports",
    suggestedCorrection: "Expose report JSON/Markdown endpoint for production integration.",
  });

  const sortedDiagnostics = deduplicateDiagnostics(diagnostics);

  const traceSummary = buildBasicTraceSummary(buildingId, "Summary", sortedDiagnostics);
  const reportSummary = buildPreview(
    {
      reportKind: "FullEngineeringCore",
      format: "Json",
      includeTraceAppendix: true,
      includeLimitations: true,
      state: {} as ProjectWorkflowState,
    },
    sortedDiagnostics,
  );

  const stateWithoutCompletion: Omit<ProjectWorkflowState, "completionByStep"> = {
    projectId,
    projectName: undefined,
    buildingId,
    buildingMetadata: {
      projectName: `Project #${projectId}`,
      buildingName: building.name,
      locationText: building.climateZoneName ?? (building.climateZoneId ? `Climate zone #${building.climateZoneId}` : "Location not set"),
      floorAreaM2,
      volumeM3,
      numberOfZones: thermalZones.length,
      notes: "Internal engineering workflow foundation state.",
    },
    zones: thermalZones
      .map((zone) => ({
        id: zone.id,
        name: zone.name,
        zoneKind: zone.rooms.length > 1 ? "Multi-room" : "Single-room",
        floorAreaM2: sumZoneArea(zone.rooms.map((item) => item.id), rooms),
        airVolumeM3: sumZoneVolume(zone.rooms.map((item) => item.id), rooms),
        status: (zone.rooms.length > 0 ? "valid" : "warnings") as WorkflowStepStatus,
      }))
      .sort((left, right) => left.name.localeCompare(right.name, "en-US")),
    boundaries,
    weatherSolarSettings: {
      weatherSourceStatus: readiness.isReady ? "Ready" : "Requires fixes",
      locationTimezoneSummary: `Weather year ${readiness.weatherYear}, climate: ${building.climateZoneName ?? "n/a"}`,
      solarChainReadinessSummary: coreStatus.weather8760GatesClosed ? "Solar/weather gates closed (ClosedV1)." : "Solar/weather gate is open.",
    },
    ventilationSettings: {
      openingCount: openingsCount,
      controlModeSummary: ventilationConfiguredCount > 0 ? "Room-level natural ventilation parameters configured." : "Defaults/manual configuration pending.",
      airflowSummary: `${ventilationConfiguredCount}/${rooms.length} room(s) have ventilation parameters`,
      warnings: sortedDiagnostics
        .filter((item) => item.sourceStep === "Ventilation" && item.severity !== "info")
        .map((item) => item.message),
    },
    groundSettings: {
      groundBoundaryCount,
      groundProfileMode: groundBoundaryCount > 0 ? "Room ground-contact profile" : "Not configured",
      summaryStatus: (groundBoundaryCount > 0 ? "valid" : "incomplete") as WorkflowStepStatus,
    },
    domesticHotWaterSettings: {
      demandBasis: "Building-level DHW endpoint not wired",
      usefulDemandSummary: "Pending endpoint integration",
      lossesSummary: "Pending endpoint integration",
      ownershipPolicy: "No double-counting policy to be supplied by system energy endpoint",
    },
    systemEnergySettings: {
      usesSummary: "Pending endpoint integration",
      carriersSummary: "Pending endpoint integration",
      finalPrimaryCarbonSummary: "Pending endpoint integration",
    },
    validationDiagnostics: sortedDiagnostics,
    calculationTraceSummary: traceSummary,
    reportSummary,
    currentStep: "Project",
    workflowMode: "api",
    workflowModeLabel: "API workflow (with explicit placeholders for pending endpoints)",
  };

  const completionByStep = computeStepCompletions(stateWithoutCompletion);

  return {
    ...stateWithoutCompletion,
    completionByStep,
  };
}

function sumZoneArea(roomIds: number[], rooms: RoomDto[]): number {
  const roomSet = new Set(roomIds);
  return rooms
    .filter((room) => roomSet.has(room.id))
    .reduce((sum, room) => sum + room.area, 0);
}

function sumZoneVolume(roomIds: number[], rooms: RoomDto[]): number {
  const roomSet = new Set(roomIds);
  return rooms
    .filter((room) => roomSet.has(room.id))
    .reduce((sum, room) => sum + (room.volume ?? room.area * (room.height ?? 3)), 0);
}

const apiClient: EngineeringWorkflowClient = {
  mode: "api",

  async getWorkflowState(projectId: number, buildingId: number): Promise<ProjectWorkflowState> {
    return getStateFromApi(projectId, buildingId);
  },

  async validateWorkflow(state: ProjectWorkflowState): Promise<WorkflowDiagnostic[]> {
    return deduplicateDiagnostics(state.validationDiagnostics);
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
    const missing = request.workflowState.validationDiagnostics.filter((item) => item.severity === "error");

    return {
      requestId: `workflow-request-${request.buildingId ?? "n-a"}`,
      status: missing.length > 0 ? "blocked" : "prepared",
      diagnostics: missing,
      metadata: {
        mode: "api",
        note: "Calculation request preparation only. Full run endpoint is pending for workflow foundation.",
      },
    };
  },

  async getTracePreview(
    calculationId: string,
    detailLevel: WorkflowTraceDetailLevel,
  ): Promise<WorkflowCalculationTraceSummary> {
    return {
      traceId: `trace-${calculationId}`,
      calculationId,
      detailLevel,
      modules: ["Validation", "Reporting"],
      assumptions: ["Detailed trace endpoint is pending for frontend workflow integration."],
      warnings: ["Trace preview is limited to compact frontend summary."],
      steps: [
        {
          stepId: "trace-pending-endpoint",
          moduleKind: "Validation",
          stepName: "Trace retrieval endpoint pending",
          sequence: 1,
          assumptions: ["Use workflow summary until backend trace endpoint is exposed."],
          warnings: ["No large arrays are exported in compact preview mode."],
          diagnosticsCount: 1,
        },
      ],
    };
  },

  async generateReport(request: EngineeringWorkflowReportRequest): Promise<EngineeringWorkflowReportResult> {
    const diagnostics = deduplicateDiagnostics([
      ...request.state.validationDiagnostics,
      {
        severity: "warning",
        code: "WORKFLOW_REPORT_DEV_EXPORT",
        message: "Report output is generated by workflow foundation serializer until backend endpoint is wired.",
        sourceStep: "Reports",
        suggestedCorrection: "Wire reporting endpoint and switch client mode to API-only export.",
      },
    ]);

    const json = buildReportJson(request, diagnostics);
    const markdown = buildReportMarkdown(request, diagnostics);

    return {
      preview: buildPreview(request, diagnostics),
      diagnostics,
      json,
      markdown,
    };
  },

  async exportReportJson(request: EngineeringWorkflowReportRequest): Promise<string> {
    return buildReportJson(request, deduplicateDiagnostics(request.state.validationDiagnostics));
  },

  async exportReportMarkdown(request: EngineeringWorkflowReportRequest): Promise<string> {
    return buildReportMarkdown(request, deduplicateDiagnostics(request.state.validationDiagnostics));
  },
};

const devClient: EngineeringWorkflowClient = {
  ...apiClient,
  mode: "dev",

  async getWorkflowState(projectId: number, buildingId: number): Promise<ProjectWorkflowState> {
    const state = await apiClient.getWorkflowState(projectId, buildingId);

    const devDiagnostics = deduplicateDiagnostics([
      ...state.validationDiagnostics,
      {
        severity: "assumption",
        code: "WORKFLOW_DEV_ADAPTER_ACTIVE",
        message: "Frontend workflow is running in internal dev adapter mode for report/trace exports.",
        sourceStep: "Reports",
        suggestedCorrection: "Switch VITE_ENGINEERING_WORKFLOW_MODE to api after endpoint wiring.",
      },
    ]);

    const updatedWithoutCompletion: Omit<ProjectWorkflowState, "completionByStep"> = {
      ...state,
      workflowMode: "dev",
      workflowModeLabel: "Internal dev workflow adapter (explicit non-production mode)",
      validationDiagnostics: devDiagnostics,
      currentStep: "Project",
    };

    return {
      ...updatedWithoutCompletion,
      completionByStep: computeStepCompletions(updatedWithoutCompletion),
    };
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
