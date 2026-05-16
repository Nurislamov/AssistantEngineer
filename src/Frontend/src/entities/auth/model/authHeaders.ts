import type { AuthStateViewModel } from "./authTypes";

export function createAuthHeaders(
  _authState: AuthStateViewModel,
): Record<string, string> {
  // P5-07 foundation intentionally does not attach real API keys or tokens.
  // Future stages can map authenticated session context to secure transport headers.
  return {};
}
