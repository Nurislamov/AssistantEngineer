import { deduplicateDiagnostics, statusLabel, stepLabel } from "./engineeringWorkflowShellViewModel";

describe("engineeringWorkflowShellViewModel", () => {
  it("deduplicates diagnostics by step/code/message deterministically", () => {
    const diagnostics = [
      { severity: "warning", code: "DUP", message: "Duplicate", sourceStep: "Validation" },
      { severity: "warning", code: "DUP", message: "Duplicate", sourceStep: "Validation" },
      { severity: "error", code: "ERR", message: "Error", sourceStep: "Building" },
    ] as const;

    const result = deduplicateDiagnostics([...diagnostics]);

    expect(result).toHaveLength(2);
    expect(result[0].code).toBe("DUP");
    expect(result[1].code).toBe("ERR");
  });

  it("maps workflow labels safely", () => {
    expect(stepLabel("DomesticHotWater")).toBe("Domestic hot water");
    expect(stepLabel("SystemEnergy")).toBe("System energy");
    expect(statusLabel("incomplete")).toBe("Incomplete");
    expect(statusLabel("ready")).toBe("Ready");
  });
});
