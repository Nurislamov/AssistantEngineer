export type EngineeringInputQualitySeverity = "info" | "warning" | "error" | "blocking";

export interface EngineeringInputQualityDiagnosticViewModel {
  code: string;
  severity: EngineeringInputQualitySeverity;
  category: string;
  message: string;
  field?: string;
  unit?: string;
  recommendation?: string;
}

export interface EngineeringInputQualitySummaryViewModel {
  diagnosticCount: number;
  highestSeverity: EngineeringInputQualitySeverity | "none";
  hasBlockingIssues: boolean;
  hasWarnings: boolean;
  isCalculationReady: boolean;
  diagnostics: EngineeringInputQualityDiagnosticViewModel[];
}

export interface EngineeringTraceSummaryViewModel {
  available: boolean;
  traceId?: string;
  sectionCount: number;
  assumptionCount: number;
  excludedEffectCount: number;
  diagnosticReferenceCount: number;
}

export interface EngineeringAssumptionsSummaryViewModel {
  available: boolean;
  totalCount: number;
  activeDefaultCount: number;
  validationOnlyCount: number;
  unknownNeedsAuditCount: number;
}

export interface EngineeringValidationReadinessViewModel {
  manualFixturesAvailable: boolean;
  tolerancePolicyAvailable: boolean;
  assumptionsRegistryAvailable: boolean;
  unitsGovernanceAvailable: boolean;
  inputQualityAvailable: boolean;
  traceExplainabilityAvailable: boolean;
  nonClaims: string[];
}

export interface EngineeringTrustOverviewViewModel {
  inputQuality: EngineeringInputQualitySummaryViewModel;
  traceSummary: EngineeringTraceSummaryViewModel;
  assumptionsSummary: EngineeringAssumptionsSummaryViewModel;
  validationReadiness: EngineeringValidationReadinessViewModel;
}
