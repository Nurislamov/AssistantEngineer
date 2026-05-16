import { render, screen } from "@testing-library/react";
import { EngineeringTrustOverviewPanel } from "./EngineeringTrustOverviewPanel";
import type { EngineeringTrustOverviewViewModel } from "@/entities/engineering-workflow/model/engineeringWorkflowTrust";

function createModel(): EngineeringTrustOverviewViewModel {
  return {
    inputQuality: {
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
          recommendation: "Provide explicit ACH input.",
        },
      ],
    },
    traceSummary: {
      available: false,
      sectionCount: 0,
      assumptionCount: 0,
      excludedEffectCount: 0,
      diagnosticReferenceCount: 0,
    },
    assumptionsSummary: {
      available: false,
      totalCount: 2,
      activeDefaultCount: 1,
      validationOnlyCount: 1,
      unknownNeedsAuditCount: 0,
    },
    validationReadiness: {
      manualFixturesAvailable: true,
      tolerancePolicyAvailable: true,
      assumptionsRegistryAvailable: true,
      unitsGovernanceAvailable: true,
      inputQualityAvailable: false,
      traceExplainabilityAvailable: true,
      nonClaims: ["No ASHRAE 140 compliance claim."],
    },
  };
}

describe("EngineeringTrustOverviewPanel", () => {
  it("renders combined trust panels without crashing", () => {
    render(<EngineeringTrustOverviewPanel model={createModel()} />);

    expect(screen.getByText("Engineering trust overview")).toBeInTheDocument();
    expect(screen.getByText("Input quality summary")).toBeInTheDocument();
    expect(screen.getByText("Calculation trace summary")).toBeInTheDocument();
    expect(screen.getByText("Assumptions summary")).toBeInTheDocument();
    expect(screen.getByText("Validation readiness")).toBeInTheDocument();
  });
});
