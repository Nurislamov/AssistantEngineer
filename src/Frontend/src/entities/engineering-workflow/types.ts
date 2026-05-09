export type WorkflowStepKind =
  | "Project"
  | "Building"
  | "Zones"
  | "Envelope"
  | "WeatherSolar"
  | "Ventilation"
  | "Ground"
  | "DomesticHotWater"
  | "SystemEnergy"
  | "Validation"
  | "CalculationTrace"
  | "Reports"
  | "Review";

export type WorkflowDiagnosticSeverity = "info" | "assumption" | "warning" | "error";

export type WorkflowStepStatus = "incomplete" | "valid" | "warnings" | "errors" | "ready";

export interface WorkflowDiagnostic {
  severity: WorkflowDiagnosticSeverity;
  code: string;
  message: string;
  sourceStep: WorkflowStepKind;
  sourceModule?: string;
  suggestedCorrection?: string;
  targetField?: string;
}

export interface WorkflowBuildingMetadata {
  projectName?: string;
  buildingName?: string;
  locationText?: string;
  floorAreaM2?: number;
  volumeM3?: number;
  numberOfZones?: number;
  notes?: string;
}

export interface WorkflowZoneSummary {
  id: number | string;
  name: string;
  zoneKind: string;
  floorAreaM2?: number;
  airVolumeM3?: number;
  status: WorkflowStepStatus;
}

export interface WorkflowBoundarySummary {
  id: number | string;
  zoneOrRoomName: string;
  exposureKind: string;
  areaM2?: number;
  uValue?: number;
  adjacentZoneReference?: string;
  indicator: "ground" | "exterior" | "adiabatic" | "adjacent";
  validationStatus: WorkflowStepStatus;
}

export interface WorkflowWeatherSolarSummary {
  weatherSourceStatus: string;
  locationTimezoneSummary: string;
  solarChainReadinessSummary: string;
}

export interface WorkflowVentilationSummary {
  openingCount: number;
  controlModeSummary: string;
  airflowSummary: string;
  warnings: string[];
}

export interface WorkflowGroundSummary {
  groundBoundaryCount: number;
  groundProfileMode: string;
  summaryStatus: WorkflowStepStatus;
}

export interface WorkflowDomesticHotWaterSummary {
  demandBasis: string;
  usefulDemandSummary: string;
  lossesSummary: string;
  ownershipPolicy: string;
}

export interface WorkflowSystemEnergySummary {
  usesSummary: string;
  carriersSummary: string;
  finalPrimaryCarbonSummary: string;
}

export interface WorkflowTraceStepSummary {
  stepId: string;
  moduleKind: string;
  stepName: string;
  sequence: number;
  assumptions: string[];
  warnings: string[];
  diagnosticsCount: number;
}

export type WorkflowTraceDetailLevel = "Summary" | "Standard" | "Detailed";

export interface WorkflowCalculationTraceSummary {
  traceId?: string;
  calculationId?: string;
  detailLevel: WorkflowTraceDetailLevel;
  modules: string[];
  assumptions: string[];
  warnings: string[];
  steps: WorkflowTraceStepSummary[];
}

export type WorkflowReportFormat = "Json" | "Markdown";

export type WorkflowReportKind =
  | "CalculationSummary"
  | "AnnualEnergy"
  | "HeatingCoolingLoad"
  | "DomesticHotWater"
  | "SystemEnergy"
  | "Validation"
  | "FullEngineeringCore"
  | "Generic";

export interface WorkflowReportPreview {
  reportKind: WorkflowReportKind;
  title: string;
  sections: string[];
  warningsCount: number;
  diagnosticsCount: number;
  exportFormatsAvailable: WorkflowReportFormat[];
  generatedTimestamp?: string;
  limitations: string[];
}

export interface WorkflowStepCompletion {
  step: WorkflowStepKind;
  status: WorkflowStepStatus;
}

export interface ProjectWorkflowState {
  projectId?: number;
  projectName?: string;
  buildingId?: number;
  buildingMetadata: WorkflowBuildingMetadata;
  zones: WorkflowZoneSummary[];
  boundaries: WorkflowBoundarySummary[];
  weatherSolarSettings: WorkflowWeatherSolarSummary;
  ventilationSettings: WorkflowVentilationSummary;
  groundSettings: WorkflowGroundSummary;
  domesticHotWaterSettings: WorkflowDomesticHotWaterSummary;
  systemEnergySettings: WorkflowSystemEnergySummary;
  validationDiagnostics: WorkflowDiagnostic[];
  calculationTraceSummary?: WorkflowCalculationTraceSummary;
  reportSummary?: WorkflowReportPreview;
  currentStep: WorkflowStepKind;
  completionByStep: WorkflowStepCompletion[];
  workflowMode: "api" | "dev";
  workflowModeLabel: string;
}

export interface EngineeringWorkflowCalculationRequest {
  projectId?: number;
  buildingId?: number;
  workflowState: ProjectWorkflowState;
}

export interface EngineeringWorkflowCalculationPreparationResult {
  requestId: string;
  status: "prepared" | "blocked";
  diagnostics: WorkflowDiagnostic[];
  metadata: Record<string, string>;
}

export interface EngineeringWorkflowReportRequest {
  reportKind: WorkflowReportKind;
  format: WorkflowReportFormat;
  includeTraceAppendix: boolean;
  includeLimitations: boolean;
  state: ProjectWorkflowState;
}

export interface EngineeringWorkflowReportResult {
  preview: WorkflowReportPreview;
  diagnostics: WorkflowDiagnostic[];
  json?: string;
  markdown?: string;
}
