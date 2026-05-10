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
  availableModules: string[];
  assumptions: string[];
  links: string[];
  metadata: Record<string, string>;
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

export type EngineeringCalculationScenarioKind =
  | "HeatingCoolingOnly"
  | "DomesticHotWaterOnly"
  | "SystemEnergyOnly"
  | "FullEngineeringCore"
  | "ValidationOnly"
  | "ReportOnly"
  | "TraceOnly";

export type EngineeringCalculationExecutionMode =
  | "ValidateOnly"
  | "PrepareOnly"
  | "ExecuteAvailableModules"
  | "ExecuteFullRequired"
  | "DryRun";

export type EngineeringCalculationExecutionStatus =
  | "NotStarted"
  | "Prepared"
  | "PartiallyExecuted"
  | "Completed"
  | "CompletedWithWarnings"
  | "FailedValidation"
  | "FailedExecution"
  | "NotSupported";

export type EngineeringCalculationArtifactKind =
  | "TraceJson"
  | "ReportJson"
  | "ReportMarkdown"
  | "ValidationDiagnostics"
  | "ScenarioResultJson";

export type EngineeringCalculationModuleExecutionStatus =
  | "NotStarted"
  | "Executed"
  | "Skipped"
  | "Failed"
  | "NotSupported";

export interface EngineeringCalculationModuleValue {
  key: string;
  label: string;
  value: unknown;
  unit?: string;
}

export interface EngineeringCalculationModuleExecutionResult {
  moduleKind: string;
  status: EngineeringCalculationModuleExecutionStatus;
  summaryValues: EngineeringCalculationModuleValue[];
  diagnostics: WorkflowDiagnostic[];
  assumptions: string[];
  warnings: string[];
  durationMilliseconds?: number;
  sourceServiceName: string;
}

export interface EngineeringCalculationModuleTiming {
  moduleKind: string;
  durationMilliseconds: number;
}

export interface EngineeringCalculationModuleSummaries {
  topologySummary?: string;
  ventilationSummary?: string;
  groundSummary?: string;
  heatingCoolingSummary?: string;
  domesticHotWaterSummary?: string;
  systemEnergySummary?: string;
}

export interface EngineeringCalculationScenarioRequest {
  scenarioId: string;
  projectId?: number;
  buildingId?: number;
  scenarioKind: EngineeringCalculationScenarioKind;
  executionMode: EngineeringCalculationExecutionMode;
  state: ProjectWorkflowState;
  requestedModules?: string[];
  detailLevel?: WorkflowTraceDetailLevel;
  includeTrace: boolean;
  includeReport: boolean;
  reportFormats?: WorkflowReportFormat[];
  deterministicTimestampUtc?: string;
  diagnosticsMode?: string;
}

export interface EngineeringCalculationScenarioResult {
  scenarioId: string;
  status: EngineeringCalculationExecutionStatus;
  executed: boolean;
  executedModules: string[];
  skippedModules: string[];
  unavailableModules: string[];
  validationDiagnostics: WorkflowDiagnostic[];
  assumptions: string[];
  warnings: string[];
  moduleSummaries: EngineeringCalculationModuleSummaries;
  moduleResults: EngineeringCalculationModuleExecutionResult[];
  timings: EngineeringCalculationModuleTiming[];
  calculationTraceSummary?: WorkflowCalculationTraceSummary | null;
  reportPreview?: WorkflowReportPreview | null;
  reportJson?: string | null;
  reportMarkdown?: string | null;
  metadata: Record<string, string>;
}

export interface EngineeringCalculationScenarioRecord {
  scenarioId: string;
  projectId: number;
  buildingId?: number | null;
  scenarioKind: EngineeringCalculationScenarioKind;
  executionMode: EngineeringCalculationExecutionMode;
  status: EngineeringCalculationExecutionStatus;
  requestJson: string;
  resultSummaryJson?: string | null;
  createdAtUtc: string;
  startedAtUtc?: string | null;
  completedAtUtc?: string | null;
  durationMilliseconds?: number | null;
  diagnosticsJson?: string | null;
}

export interface EngineeringCalculationArtifactRecord {
  artifactId: string;
  scenarioId: string;
  artifactKind: EngineeringCalculationArtifactKind;
  contentType: string;
  content: string;
  createdAtUtc: string;
  sizeBytes?: number | null;
  checksumSha256?: string | null;
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

export interface EngineeringWorkflowApiStateResponse {
  projectId: number;
  projectName: string;
  buildingId?: number | null;
  currentStep: WorkflowStepKind;
  steps: Array<{ kind: WorkflowStepKind; status: WorkflowStepStatus; isComplete: boolean }>;
  availableModules: string[];
  buildingMetadata: WorkflowBuildingMetadata;
  zones: WorkflowZoneSummary[];
  boundaries: WorkflowBoundarySummary[];
  weatherSolarSettings: WorkflowWeatherSolarSummary;
  ventilationSettings: WorkflowVentilationSummary;
  groundSettings: WorkflowGroundSummary;
  domesticHotWaterSettings: WorkflowDomesticHotWaterSummary;
  systemEnergySettings: WorkflowSystemEnergySummary;
  diagnostics: WorkflowDiagnostic[];
  assumptions: string[];
  links: string[];
  calculationTraceSummary?: WorkflowCalculationTraceSummary | null;
  reportSummary?: WorkflowReportPreview | null;
  metadata: Record<string, string>;
}

export interface EngineeringWorkflowValidationResponse {
  isValid: boolean;
  diagnostics: WorkflowDiagnostic[];
  steps: Array<{ kind: WorkflowStepKind; status: WorkflowStepStatus; isComplete: boolean }>;
}

export type EngineeringCalculationScenarioResponse = EngineeringCalculationScenarioResult;

export interface EngineeringWorkflowTracePreviewResponse {
  traceDocument: unknown;
  traceSummary: WorkflowCalculationTraceSummary;
  diagnostics: WorkflowDiagnostic[];
}

export interface EngineeringWorkflowReportResponse {
  reportDocument: unknown;
  preview: WorkflowReportPreview;
  diagnostics: WorkflowDiagnostic[];
}

export interface EngineeringWorkflowReportExportResponse {
  format: "Json" | "Markdown";
  content: string;
  schemaVersion: string;
  reportId: string;
  diagnostics: WorkflowDiagnostic[];
}
