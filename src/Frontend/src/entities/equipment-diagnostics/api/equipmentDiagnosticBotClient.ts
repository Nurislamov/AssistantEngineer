import { apiRoutes } from "@/shared/api/apiRoutes";
import { ApiError, apiRequest } from "@/shared/api/httpClient";
import type {
  EquipmentDiagnosticBotRequest,
  EquipmentDiagnosticBotResponse,
  EquipmentDiagnosticBotValidationProblem,
} from "../types";

const forbiddenResponseFragments = [
  "artifacts/verification",
  "knowledge/staging",
  "knowledge/manual-codebook",
  "staging-candidate-preview",
  "bypass",
  "disable protection",
  "disable protections",
  "force run",
  "short protection",
  "ignore protection",
  "c:\\",
  "d:\\",
  "/src/",
  ".pdf",
];

export class EquipmentDiagnosticBotClientError extends Error {
  public readonly validationErrors?: Record<string, string[]>;

  constructor(message: string, validationErrors?: Record<string, string[]>) {
    super(message);
    this.name = "EquipmentDiagnosticBotClientError";
    this.validationErrors = validationErrors;
  }
}

export async function diagnoseEquipmentBot(
  request: EquipmentDiagnosticBotRequest,
  options: { signal?: AbortSignal } = {},
): Promise<EquipmentDiagnosticBotResponse> {
  try {
    const response = await apiRequest<EquipmentDiagnosticBotResponse>(
      apiRoutes.equipmentDiagnostics.botDiagnose(),
      {
        method: "POST",
        body: request,
        signal: options.signal,
      },
    );

    assertSafeResponse(response);
    return response;
  } catch (error) {
    if (error instanceof EquipmentDiagnosticBotClientError) {
      throw error;
    }

    if (error instanceof ApiError) {
      const problem = typeof error.details === "object"
        ? error.details as EquipmentDiagnosticBotValidationProblem
        : undefined;
      const validationMessage = Object.values(problem?.errors ?? {}).flat()[0];
      throw new EquipmentDiagnosticBotClientError(
        validationMessage ?? problem?.detail ?? "The diagnostic request could not be processed.",
        problem?.errors,
      );
    }

    if (error instanceof DOMException && error.name === "AbortError") {
      throw error;
    }

    throw new EquipmentDiagnosticBotClientError(
      "The diagnostic service is unavailable. Check the connection and try again.",
    );
  }
}

function assertSafeResponse(response: EquipmentDiagnosticBotResponse): void {
  const serialized = JSON.stringify(response).toLowerCase();
  if (forbiddenResponseFragments.some((fragment) => serialized.includes(fragment))) {
    throw new EquipmentDiagnosticBotClientError(
      "The diagnostic response could not be displayed safely.",
    );
  }
}
