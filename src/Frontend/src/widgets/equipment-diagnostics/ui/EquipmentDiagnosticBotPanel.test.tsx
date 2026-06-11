import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import {
  diagnoseEquipmentBot,
  EquipmentDiagnosticBotClientError,
} from "@/entities/equipment-diagnostics/api/equipmentDiagnosticBotClient";
import type { EquipmentDiagnosticBotResponse } from "@/entities/equipment-diagnostics/types";
import { EquipmentDiagnosticBotPanel } from "./EquipmentDiagnosticBotPanel";

vi.mock("@/entities/equipment-diagnostics/api/equipmentDiagnosticBotClient", () => {
  class MockClientError extends Error {
    constructor(message: string) {
      super(message);
      this.name = "EquipmentDiagnosticBotClientError";
    }
  }

  return {
    diagnoseEquipmentBot: vi.fn(),
    EquipmentDiagnosticBotClientError: MockClientError,
  };
});

const diagnoseMock = vi.mocked(diagnoseEquipmentBot);

describe("EquipmentDiagnosticBotPanel", () => {
  beforeEach(() => diagnoseMock.mockReset());

  it("renders required form fields and submits the existing endpoint request", async () => {
    diagnoseMock.mockResolvedValue(answer());
    render(<EquipmentDiagnosticBotPanel />);

    expect(screen.getByLabelText(/Manufacturer/)).toHaveValue("Gree");
    expect(screen.getByLabelText(/Displayed code/)).toBeInTheDocument();
    expect(screen.getByLabelText("Series")).toBeInTheDocument();
    expect(screen.getByLabelText("Model code")).toBeInTheDocument();

    fireEvent.change(screen.getByLabelText(/Displayed code/), { target: { value: "H5" } });
    fireEvent.change(screen.getByLabelText("Series"), { target: { value: "GMV" } });
    fireEvent.click(screen.getByRole("button", { name: "Diagnose" }));

    await waitFor(() => expect(diagnoseMock).toHaveBeenCalledWith(
      expect.objectContaining({ manufacturer: "Gree", code: "H5", series: "GMV" }),
      expect.objectContaining({ signal: expect.any(AbortSignal) }),
    ));
  });

  it("renders answer verification, provenance, safety, and next steps", async () => {
    diagnoseMock.mockResolvedValue(answer());
    renderAndSubmit();

    expect(await screen.findByText("Gree GMV H5 diagnostic guidance")).toBeInTheDocument();
    expect(screen.getByText("Verification required.")).toBeInTheDocument();
    expect(screen.getByText("Seeded knowledge.")).toBeInTheDocument();
    expect(screen.getByText("Qualified technician review required.")).toBeInTheDocument();
    expect(screen.getByText("Verify equipment identity.")).toBeInTheDocument();
  });

  it("renders clarification options and fills visible context without hidden resubmit", async () => {
    diagnoseMock.mockResolvedValue({
      ...answer(),
      status: 1,
      title: "Equipment context required",
      answerCard: null,
      sourceCard: null,
      clarificationQuestion: {
        prompt: "Which equipment context shows this code?",
        options: [{
          label: "Gree GMV (Outdoor)",
          manufacturer: "Gree",
          series: "GMV",
          category: 0,
          equipmentSide: 1,
          displayContext: 1,
          code: "E1",
          explanation: "Runtime context.",
          followUpPrompt: "Confirm context.",
        }],
      },
    });
    renderAndSubmit("E1");

    fireEvent.click(await screen.findByRole("button", { name: "Gree GMV (Outdoor)" }));

    expect(screen.getByLabelText("Series")).toHaveValue("GMV");
    expect(diagnoseMock).toHaveBeenCalledTimes(1);
    expect(screen.queryByText("Which equipment context shows this code?")).not.toBeInTheDocument();
  });

  it.each([
    [3, "Reference-only code pattern", "Verify display context."],
    [2, "Runtime diagnostic case not found", "Verify manufacturer and service manual."],
    [4, "Unsupported input", "Confirm displayed code."],
    [5, "Outside diagnostic boundary", "Escalate for qualified review."],
  ])("renders non-answer status %s without a final fault card", async (status, title, nextStep) => {
    diagnoseMock.mockResolvedValue({
      ...answer(),
      status,
      title,
      answerCard: null,
      sourceCard: null,
      operatorNextSteps: [nextStep],
    });
    renderAndSubmit();

    expect(await screen.findByText(title)).toBeInTheDocument();
    expect(screen.queryByText("Preliminary runtime guidance.")).not.toBeInTheDocument();
    expect(screen.getByText(nextStep)).toBeInTheDocument();
  });

  it("renders controlled validation and network errors", async () => {
    diagnoseMock.mockRejectedValueOnce(new EquipmentDiagnosticBotClientError("Manufacturer is required."));
    const { unmount } = renderAndSubmit();
    expect(await screen.findByText("Manufacturer is required.")).toBeInTheDocument();
    unmount();

    diagnoseMock.mockRejectedValueOnce(new EquipmentDiagnosticBotClientError("The diagnostic service is unavailable."));
    renderAndSubmit();
    expect(await screen.findByText("The diagnostic service is unavailable.")).toBeInTheDocument();
  });

  it.each([
    ["H5", 0, "Gree GMV H5 diagnostic guidance", "Verify equipment identity."],
    ["C5", 0, "Gree Indoor C5 diagnostic guidance", "Verify equipment identity."],
    ["A0", 3, "Reference-only code pattern", "Verify display context."],
    ["n6", 3, "Reference-only code pattern", "Verify display context."],
    ["ZZ99", 2, "Runtime diagnostic case not found", "Verify manufacturer and service manual."],
  ])("renders field scenario %s with deterministic UI state", async (code, status, title, nextStep) => {
    diagnoseMock.mockResolvedValue({
      ...answer(),
      status,
      title,
      answerCard: status === 0 ? answer().answerCard : null,
      sourceCard: status === 0 ? answer().sourceCard : null,
      operatorNextSteps: [nextStep],
    });
    renderAndSubmit(code);

    expect(await screen.findByText(title)).toBeInTheDocument();
    expect(screen.getByText(nextStep)).toBeInTheDocument();
    expect(screen.getByText("Qualified technician review required.")).toBeInTheDocument();
    if (status !== 0)
      expect(screen.queryByText("Preliminary runtime guidance.")).not.toBeInTheDocument();
  });

  it("disables submit while loading to prevent duplicate requests", async () => {
    let resolveRequest!: (response: EquipmentDiagnosticBotResponse) => void;
    diagnoseMock.mockReturnValue(new Promise((resolve) => {
      resolveRequest = resolve;
    }));
    renderAndSubmit();

    expect(await screen.findByRole("button", { name: /Checking runtime catalog/ })).toBeDisabled();
    resolveRequest(answer());
    await waitFor(() => expect(screen.getByRole("button", { name: "Diagnose" })).toBeEnabled());
  });
});

function renderAndSubmit(code = "H5"): ReturnType<typeof render> {
  const result = render(<EquipmentDiagnosticBotPanel />);
  fireEvent.change(screen.getByLabelText(/Displayed code/), { target: { value: code } });
  fireEvent.click(screen.getByRole("button", { name: "Diagnose" }));
  return result;
}

function answer(): EquipmentDiagnosticBotResponse {
  return {
    status: 0,
    title: "Gree GMV H5 diagnostic guidance",
    message: "Preliminary diagnostic guidance.",
    normalizedManufacturer: "GREE",
    normalizedCode: "H5",
    answerCard: {
      title: "Gree GMV H5 diagnostic guidance",
      summary: "Preliminary runtime guidance.",
      verificationBanner: "Verification required.",
      likelyCauses: [],
      recommendedChecks: [],
      operatorNotes: [],
    },
    clarificationQuestion: null,
    sourceCard: {
      sourceType: "SeededEngineeringKnowledge",
      evidenceLevel: "UnverifiedSeed",
      summary: "Seeded knowledge.",
      limitations: [],
    },
    safetyCard: { boundary: "Qualified technician review required.", notes: [] },
    verificationRequired: true,
    confidence: 1,
    isManualVerified: false,
    isSeedKnowledge: true,
    operatorNextSteps: ["Verify equipment identity."],
    warnings: [],
  };
}
