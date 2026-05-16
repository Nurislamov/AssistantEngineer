import { render, screen } from "@testing-library/react";
import { EngineeringInputQualitySummaryPanel } from "./EngineeringInputQualitySummaryPanel";
import type { EngineeringInputQualitySummaryViewModel } from "@/entities/engineering-workflow/model/engineeringWorkflowTrust";

function createSummary(overrides: Partial<EngineeringInputQualitySummaryViewModel> = {}): EngineeringInputQualitySummaryViewModel {
  return {
    diagnosticCount: 1,
    highestSeverity: "warning",
    hasBlockingIssues: false,
    hasWarnings: true,
    isCalculationReady: true,
    diagnostics: [
      {
        code: "IQ-ROOM-040",
        severity: "warning",
        category: "Ventilation",
        message: "Missing ventilation config.",
        recommendation: "Add ventilation parameters.",
      },
    ],
    ...overrides,
  };
}

describe("EngineeringInputQualitySummaryPanel", () => {
  it("renders Ready with warnings status", () => {
    render(<EngineeringInputQualitySummaryPanel summary={createSummary()} />);
    expect(screen.getByText("Ready with warnings")).toBeInTheDocument();
  });

  it("renders Not ready for blocking issue", () => {
    render(
      <EngineeringInputQualitySummaryPanel
        summary={createSummary({
          hasBlockingIssues: true,
          isCalculationReady: false,
          highestSeverity: "blocking",
          diagnostics: [
            {
              code: "IQ-BLD-011",
              severity: "blocking",
              category: "CalculationReadiness",
              message: "Building has no rooms.",
            },
          ],
        })}
      />,
    );

    expect(screen.getByText("Not ready")).toBeInTheDocument();
  });

  it("renders diagnostic code, category, message and recommendation", () => {
    render(<EngineeringInputQualitySummaryPanel summary={createSummary()} />);
    expect(screen.getByText("IQ-ROOM-040")).toBeInTheDocument();
    expect(screen.getByText("Ventilation")).toBeInTheDocument();
    expect(screen.getByText("Missing ventilation config.")).toBeInTheDocument();
    expect(screen.getByText(/Add ventilation parameters/i)).toBeInTheDocument();
  });
});
