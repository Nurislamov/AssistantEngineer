import type {
  EngineeringInputQualityDiagnosticViewModel,
  EngineeringInputQualitySeverity,
  EngineeringInputQualitySummaryViewModel,
  EngineeringValidationReadinessViewModel,
} from "./engineeringWorkflowTrust";

const severityRank: Record<EngineeringInputQualitySeverity, number> = {
  info: 0,
  warning: 1,
  error: 2,
  blocking: 3,
};

export function getHighestInputQualitySeverity(
  diagnostics: EngineeringInputQualityDiagnosticViewModel[],
): EngineeringInputQualitySeverity | "none" {
  if (diagnostics.length === 0) {
    return "none";
  }

  return diagnostics
    .slice()
    .sort((left, right) => severityRank[right.severity] - severityRank[left.severity])[0].severity;
}

export function summarizeInputQuality(
  diagnostics: EngineeringInputQualityDiagnosticViewModel[],
): EngineeringInputQualitySummaryViewModel {
  const highestSeverity = getHighestInputQualitySeverity(diagnostics);
  const hasBlockingIssues = diagnostics.some((item) => item.severity === "blocking");
  const hasErrors = diagnostics.some((item) => item.severity === "error");
  const hasWarnings = diagnostics.some((item) => item.severity === "warning");

  return {
    diagnosticCount: diagnostics.length,
    highestSeverity,
    hasBlockingIssues,
    hasWarnings,
    isCalculationReady: !hasBlockingIssues && !hasErrors,
    diagnostics,
  };
}

export function isCalculationReady(summary: EngineeringInputQualitySummaryViewModel): boolean {
  return summary.isCalculationReady;
}

export function getSeverityLabel(
  severity: EngineeringInputQualitySeverity | "none",
): string {
  if (severity === "blocking") {
    return "Blocking";
  }

  if (severity === "error") {
    return "Error";
  }

  if (severity === "warning") {
    return "Warning";
  }

  if (severity === "info") {
    return "Info";
  }

  return "None";
}

export function getSeverityTone(
  severity: EngineeringInputQualitySeverity | "none",
): "default" | "info" | "warning" | "error" | "success" {
  if (severity === "blocking" || severity === "error") {
    return "error";
  }

  if (severity === "warning") {
    return "warning";
  }

  if (severity === "info") {
    return "info";
  }

  return "default";
}

export function getNonClaimsText(): string[] {
  return [
    "No ASHRAE 140 compliance claim.",
    "No exact EnergyPlus equivalence claim.",
    "No third-party tool equivalence claim.",
    "No full ISO/EN compliance claim.",
    "No certified/certification claim.",
  ];
}

export function buildValidationReadinessSummary(
  flags: Omit<EngineeringValidationReadinessViewModel, "nonClaims">,
): EngineeringValidationReadinessViewModel {
  return {
    ...flags,
    nonClaims: getNonClaimsText(),
  };
}

