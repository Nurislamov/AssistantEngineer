import { apiRequest } from "@/shared/api/httpClient";

export type WorkflowRequestOptions = Parameters<typeof apiRequest>[1];

export function workflowApiRequest<TResponse>(
  path: string,
  options?: WorkflowRequestOptions,
): Promise<TResponse> {
  return apiRequest<TResponse>(path, options);
}

export function mapTransportError(error: unknown, fallback: string): string {
  return error instanceof Error ? error.message : fallback;
}
