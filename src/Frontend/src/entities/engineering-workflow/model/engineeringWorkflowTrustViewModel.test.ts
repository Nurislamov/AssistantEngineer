import {
  buildValidationReadinessSummary,
  getHighestInputQualitySeverity,
  getNonClaimsText,
  summarizeInputQuality,
} from "./engineeringWorkflowTrustViewModel";
import type { EngineeringInputQualityDiagnosticViewModel } from "./engineeringWorkflowTrust";

describe("engineeringWorkflowTrustViewModel", () => {
  it("summarizeInputQuality returns ready when no blocking/error diagnostics exist", () => {
    const diagnostics: EngineeringInputQualityDiagnosticViewModel[] = [
      {
        code: "IQ-ROOM-040",
        severity: "warning",
        category: "Ventilation",
        message: "Missing ventilation configuration.",
      },
    ];

    const summary = summarizeInputQuality(diagnostics);
    expect(summary.isCalculationReady).toBe(true);
    expect(summary.hasWarnings).toBe(true);
    expect(summary.hasBlockingIssues).toBe(false);
  });

  it("warning-only diagnostics produce ready with warnings semantics", () => {
    const diagnostics: EngineeringInputQualityDiagnosticViewModel[] = [
      {
        code: "IQ-ROOM-030",
        severity: "warning",
        category: "Envelope",
        message: "Suspicious window ratio.",
      },
    ];

    const summary = summarizeInputQuality(diagnostics);
    expect(summary.isCalculationReady).toBe(true);
    expect(summary.highestSeverity).toBe("warning");
  });

  it("error or blocking diagnostics produce not-ready semantics", () => {
    const diagnostics: EngineeringInputQualityDiagnosticViewModel[] = [
      {
        code: "IQ-ROOM-010",
        severity: "error",
        category: "Geometry",
        message: "Invalid area.",
      },
      {
        code: "IQ-BLD-011",
        severity: "blocking",
        category: "CalculationReadiness",
        message: "Building has no rooms.",
      },
    ];

    const summary = summarizeInputQuality(diagnostics);
    expect(summary.isCalculationReady).toBe(false);
    expect(summary.hasBlockingIssues).toBe(true);
  });

  it("highest severity ordering is deterministic", () => {
    const diagnostics: EngineeringInputQualityDiagnosticViewModel[] = [
      { code: "A", severity: "info", category: "Units", message: "Info" },
      { code: "B", severity: "warning", category: "Envelope", message: "Warning" },
      { code: "C", severity: "error", category: "Geometry", message: "Error" },
    ];

    expect(getHighestInputQualitySeverity(diagnostics)).toBe("error");
  });

  it("non-claims include required phrases", () => {
    const nonClaims = getNonClaimsText();
    expect(nonClaims).toContain("No ASHRAE 140 compliance claim.");
    expect(nonClaims).toContain("No exact EnergyPlus equivalence claim.");
    expect(nonClaims).toContain("No third-party tool equivalence claim.");
    expect(nonClaims).toContain("No full ISO/EN compliance claim.");
    expect(nonClaims).toContain("No certified/certification claim.");
  });

  it("buildValidationReadinessSummary appends required non-claims", () => {
    const summary = buildValidationReadinessSummary({
      manualFixturesAvailable: true,
      tolerancePolicyAvailable: true,
      assumptionsRegistryAvailable: true,
      unitsGovernanceAvailable: true,
      inputQualityAvailable: false,
      traceExplainabilityAvailable: true,
    });

    expect(summary.nonClaims.length).toBeGreaterThanOrEqual(5);
    expect(summary.nonClaims).toContain("No exact EnergyPlus equivalence claim.");
  });
});

