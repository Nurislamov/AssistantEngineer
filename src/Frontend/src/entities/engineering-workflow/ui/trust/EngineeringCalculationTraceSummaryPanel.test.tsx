import { render, screen } from "@testing-library/react";
import { EngineeringCalculationTraceSummaryPanel } from "./EngineeringCalculationTraceSummaryPanel";

describe("EngineeringCalculationTraceSummaryPanel", () => {
  it("renders unavailable state", () => {
    render(
      <EngineeringCalculationTraceSummaryPanel
        summary={{
          available: false,
          sectionCount: 0,
          assumptionCount: 0,
          excludedEffectCount: 0,
          diagnosticReferenceCount: 0,
        }}
      />,
    );

    expect(screen.getByText("Trace is unavailable")).toBeInTheDocument();
    expect(screen.getByText(/has not been generated/i)).toBeInTheDocument();
  });

  it("renders summary counts when available", () => {
    render(
      <EngineeringCalculationTraceSummaryPanel
        summary={{
          available: true,
          traceId: "trace-001",
          sectionCount: 7,
          assumptionCount: 3,
          excludedEffectCount: 2,
          diagnosticReferenceCount: 4,
        }}
      />,
    );

    expect(screen.getByText(/Trace ID: trace-001/i)).toBeInTheDocument();
    expect(screen.getByText(/Sections: 7/i)).toBeInTheDocument();
    expect(screen.getByText(/Assumptions: 3/i)).toBeInTheDocument();
    expect(screen.getByText(/Excluded effects: 2/i)).toBeInTheDocument();
    expect(screen.getByText(/Diagnostic refs: 4/i)).toBeInTheDocument();
  });
});
