import { apiRoutes } from "@/shared/api/apiRoutes";
import {
  diagnoseEquipmentBot,
  EquipmentDiagnosticBotClientError,
} from "./equipmentDiagnosticBotClient";
import type { EquipmentDiagnosticBotResponse } from "../types";

const answer: EquipmentDiagnosticBotResponse = {
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

describe("equipmentDiagnosticBotClient", () => {
  afterEach(() => vi.restoreAllMocks());

  it("posts a deterministic request to the existing bot endpoint", async () => {
    const fetchSpy = vi.spyOn(globalThis, "fetch").mockResolvedValueOnce(jsonResponse(answer));

    await diagnoseEquipmentBot({ manufacturer: "Gree", code: "H5", series: "GMV" });

    const [url, init] = fetchSpy.mock.calls[0];
    expect(String(url)).toContain(apiRoutes.equipmentDiagnostics.botDiagnose());
    expect(init?.method).toBe("POST");
    expect(JSON.parse(String(init?.body))).toEqual({ manufacturer: "Gree", code: "H5", series: "GMV" });
  });

  it("returns validation problem details as a controlled message", async () => {
    vi.spyOn(globalThis, "fetch").mockResolvedValueOnce(jsonResponse({
      title: "Validation failed",
      errors: { Manufacturer: ["Manufacturer is required."] },
    }, 400));

    await expect(diagnoseEquipmentBot({ manufacturer: "", code: "H5" }))
      .rejects.toMatchObject({
        name: "EquipmentDiagnosticBotClientError",
        message: "Manufacturer is required.",
      });
  });

  it("returns a safe message for network and non-json failures", async () => {
    vi.spyOn(globalThis, "fetch").mockRejectedValueOnce(new Error("socket details"));

    await expect(diagnoseEquipmentBot({ manufacturer: "Gree", code: "H5" }))
      .rejects.toThrow("The diagnostic service is unavailable");
  });

  it.each([
    "artifacts/verification/report.json",
    "Knowledge/staging/candidate.json",
    "Knowledge/manual-codebook/gree.json",
    "disable protection",
  ])("rejects a response containing non-runtime or unsafe text: %s", async (fragment) => {
    vi.spyOn(globalThis, "fetch").mockResolvedValueOnce(jsonResponse({ ...answer, message: fragment }));

    await expect(diagnoseEquipmentBot({ manufacturer: "Gree", code: "H5" }))
      .rejects.toBeInstanceOf(EquipmentDiagnosticBotClientError);
  });
});

function jsonResponse(body: unknown, status = 200): Response {
  return new Response(JSON.stringify(body), {
    status,
    headers: { "content-type": "application/json" },
  });
}
