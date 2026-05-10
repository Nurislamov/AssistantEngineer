import { fireEvent, render, screen } from "@testing-library/react";
import { WorkflowDiagnosticsPanel } from "./WorkflowDiagnosticsPanel";
import type { WorkflowDiagnostic } from "@/entities/engineering-workflow/types";

describe("WorkflowDiagnosticsPanel", () => {
  it("renders diagnostics with deterministic sorting and suggested correction", () => {
    const diagnostics: WorkflowDiagnostic[] = [
      {
        severity: "warning",
        code: "WARN_01",
        message: "Warning message",
        sourceStep: "Validation",
      },
      {
        severity: "error",
        code: "ERR_01",
        message: "Error message",
        sourceStep: "Building",
        suggestedCorrection: "Fix building metadata",
      },
    ];

    const onSelectStep = vi.fn();
    render(<WorkflowDiagnosticsPanel diagnostics={diagnostics} onSelectStep={onSelectStep} />);

    expect(screen.getByText("Validation diagnostics")).toBeInTheDocument();
    expect(screen.getByRole("combobox")).toBeInTheDocument();
    expect(screen.getByText("ERR_01")).toBeInTheDocument();
    expect(screen.getByText("WARN_01")).toBeInTheDocument();
    expect(screen.getByText(/Suggested correction:/i)).toBeInTheDocument();

    fireEvent.click(screen.getByText("Error message"));
    expect(onSelectStep).toHaveBeenCalledWith("Building");
  });
});
