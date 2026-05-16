import { render, screen } from "@testing-library/react";
import { EngineeringValidationReadinessPanel } from "./EngineeringValidationReadinessPanel";

describe("EngineeringValidationReadinessPanel", () => {
  it("renders checklist items", () => {
    render(
      <EngineeringValidationReadinessPanel
        readiness={{
          manualFixturesAvailable: true,
          tolerancePolicyAvailable: true,
          assumptionsRegistryAvailable: true,
          unitsGovernanceAvailable: true,
          inputQualityAvailable: false,
          traceExplainabilityAvailable: true,
          nonClaims: [],
        }}
      />,
    );

    expect(screen.getByText("Manual validation fixtures")).toBeInTheDocument();
    expect(screen.getByText("Validation tolerance policy")).toBeInTheDocument();
    expect(screen.getByText("Engineering assumptions registry")).toBeInTheDocument();
    expect(screen.getByText("Units governance")).toBeInTheDocument();
    expect(screen.getByText("Input quality checks")).toBeInTheDocument();
    expect(screen.getByText("Trace explainability foundation")).toBeInTheDocument();
  });

  it("renders required non-claims", () => {
    render(
      <EngineeringValidationReadinessPanel
        readiness={{
          manualFixturesAvailable: true,
          tolerancePolicyAvailable: true,
          assumptionsRegistryAvailable: true,
          unitsGovernanceAvailable: true,
          inputQualityAvailable: true,
          traceExplainabilityAvailable: true,
          nonClaims: [
            "No ASHRAE 140 compliance claim.",
            "No exact EnergyPlus equivalence claim.",
            "No third-party tool equivalence claim.",
            "No full ISO/EN compliance claim.",
            "No certified/certification claim.",
          ],
        }}
      />,
    );

    expect(screen.getByText(/No ASHRAE 140 compliance claim\./)).toBeInTheDocument();
    expect(screen.getByText(/No exact EnergyPlus equivalence claim\./)).toBeInTheDocument();
    expect(screen.getByText(/No third-party tool equivalence claim\./)).toBeInTheDocument();
    expect(screen.getByText(/No full ISO\/EN compliance claim\./)).toBeInTheDocument();
    expect(screen.getByText(/No certified\/certification claim\./)).toBeInTheDocument();
    expect(screen.getByText("This panel does not claim exact EnergyPlus equivalence.")).toBeInTheDocument();
  });
});

