import { useMemo } from "react";
import type {
  ProjectWorkflowState,
  WorkflowCalculationTraceSummary,
  WorkflowDiagnostic,
} from "@/entities/engineering-workflow/types";
import type {
  EngineeringAssumptionsSummaryViewModel,
  EngineeringInputQualityDiagnosticViewModel,
  EngineeringInputQualitySeverity,
  EngineeringTraceSummaryViewModel,
  EngineeringTrustOverviewViewModel,
} from "@/entities/engineering-workflow/model/engineeringWorkflowTrust";
import {
  buildValidationReadinessSummary,
  summarizeInputQuality,
} from "@/entities/engineering-workflow/model/engineeringWorkflowTrustViewModel";

interface UseEngineeringWorkflowTrustOverviewArgs {
  workflowState: ProjectWorkflowState | undefined;
  diagnostics: WorkflowDiagnostic[];
  traceSummary: WorkflowCalculationTraceSummary | undefined;
}

function mapDiagnosticSeverity(severity: WorkflowDiagnostic["severity"]): EngineeringInputQualitySeverity {
  if (severity === "error") {
    return "error";
  }

  if (severity === "warning") {
    return "warning";
  }

  return "info";
}

function buildInputQualityDiagnostics(diagnostics: WorkflowDiagnostic[]): EngineeringInputQualityDiagnosticViewModel[] {
  return diagnostics.map((item) => ({
    code: item.code,
    severity: mapDiagnosticSeverity(item.severity),
    category: item.sourceModule ?? item.sourceStep,
    message: item.message,
    field: item.targetField,
    recommendation: item.suggestedCorrection,
  }));
}

function buildTraceSummary(trace: WorkflowCalculationTraceSummary | undefined): EngineeringTraceSummaryViewModel {
  if (!trace) {
    return {
      available: false,
      sectionCount: 0,
      assumptionCount: 0,
      excludedEffectCount: 0,
      diagnosticReferenceCount: 0,
    };
  }

  const diagnosticReferenceCount = trace.steps.reduce((sum, step) => sum + step.diagnosticsCount, 0);
  const available = Boolean(trace.traceId || trace.steps.length > 0 || trace.modules.length > 0);

  return {
    available,
    traceId: trace.traceId,
    sectionCount: trace.steps.length,
    assumptionCount: trace.assumptions.length,
    excludedEffectCount: 0,
    diagnosticReferenceCount,
  };
}

function buildAssumptionsSummary(
  workflowState: ProjectWorkflowState | undefined,
): EngineeringAssumptionsSummaryViewModel {
  const assumptions = workflowState?.assumptions ?? [];
  const normalized = assumptions.map((item) => item.toLowerCase());
  const activeDefaultCount = normalized.filter((item) => item.includes("default") || item.includes("fallback")).length;
  const validationOnlyCount = normalized.filter((item) => item.includes("validationonly") || item.includes("manual fixture")).length;
  const unknownNeedsAuditCount = normalized.filter((item) => item.includes("unknownneedsaudit") || item.includes("needs audit")).length;

  const registryLinked = workflowState?.metadata.assumptionsRegistryLinked === "true";
  return {
    available: registryLinked && assumptions.length > 0,
    totalCount: assumptions.length,
    activeDefaultCount,
    validationOnlyCount,
    unknownNeedsAuditCount,
  };
}

export function useEngineeringWorkflowTrustOverview({
  workflowState,
  diagnostics,
  traceSummary,
}: UseEngineeringWorkflowTrustOverviewArgs): EngineeringTrustOverviewViewModel {
  return useMemo(() => {
    const inputQualityDiagnostics = buildInputQualityDiagnostics(diagnostics);
    const inputQuality = summarizeInputQuality(inputQualityDiagnostics);
    const trace = buildTraceSummary(traceSummary);
    const assumptions = buildAssumptionsSummary(workflowState);
    const validationReadiness = buildValidationReadinessSummary({
      manualFixturesAvailable: true,
      tolerancePolicyAvailable: true,
      assumptionsRegistryAvailable: true,
      unitsGovernanceAvailable: true,
      inputQualityAvailable: inputQualityDiagnostics.length > 0,
      traceExplainabilityAvailable: true,
    });

    return {
      inputQuality,
      traceSummary: trace,
      assumptionsSummary: assumptions,
      validationReadiness,
    };
  }, [diagnostics, traceSummary, workflowState]);
}
