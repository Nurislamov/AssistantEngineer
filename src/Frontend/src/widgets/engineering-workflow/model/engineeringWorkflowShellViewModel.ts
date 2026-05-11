import type { WorkflowDiagnostic, WorkflowStepKind, WorkflowStepStatus } from "@/entities/engineering-workflow/types";

export const orderedWorkflowSteps: WorkflowStepKind[] = [
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

export function stepLabel(step: WorkflowStepKind): string {
  if (step === "DomesticHotWater") {
    return "Domestic hot water";
  }

  if (step === "WeatherSolar") {
    return "Weather / Solar";
  }

  if (step === "SystemEnergy") {
    return "System energy";
  }

  if (step === "CalculationTrace") {
    return "Calculation trace";
  }

  return step;
}

export function statusLabel(status: WorkflowStepStatus): string {
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

export function deduplicateDiagnostics(diagnostics: WorkflowDiagnostic[]): WorkflowDiagnostic[] {
  const keys = new Set<string>();
  return diagnostics.filter((item) => {
    const key = `${item.sourceStep}|${item.code}|${item.message}`;
    if (keys.has(key)) {
      return false;
    }

    keys.add(key);
    return true;
  });
}
