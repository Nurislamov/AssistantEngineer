import { appConfig } from "@/shared/config/env";
import type { ApiErrorPayload, ProblemDetails } from "@/shared/types/api";

type QueryValue = string | number | boolean | null | undefined;

interface RequestOptions {
  method?: "GET" | "POST" | "PUT" | "DELETE";
  body?: unknown;
  query?: Record<string, QueryValue>;
  headers?: Record<string, string>;
  signal?: AbortSignal;
}

export class ApiError extends Error {
  public readonly status: number;
  public readonly details?: ProblemDetails | string;

  constructor(payload: ApiErrorPayload) {
    super(payload.message);
    this.name = "ApiError";
    this.status = payload.status;
    this.details = payload.details;
  }
}

function buildUrl(path: string, query?: Record<string, QueryValue>): string {
  const url = new URL(path, appConfig.apiBaseUrl);

  Object.entries(query ?? {}).forEach(([key, value]) => {
    if (value !== undefined && value !== null && value !== "") {
      url.searchParams.set(key, String(value));
    }
  });

  return url.toString();
}

async function readError(response: Response): Promise<ApiError> {
  const contentType = response.headers.get("content-type") ?? "";

  if (contentType.includes("application/json")) {
    const details = (await response.json()) as ProblemDetails;
    return new ApiError({
      status: response.status,
      message: details.title ?? details.detail ?? `API request failed with ${response.status}`,
      details,
    });
  }

  const text = await response.text();
  return new ApiError({
    status: response.status,
    message: text || `API request failed with ${response.status}`,
    details: text,
  });
}

export async function apiRequest<TResponse>(
  path: string,
  options: RequestOptions = {},
): Promise<TResponse> {
  const response = await fetch(buildUrl(path, options.query), {
    method: options.method ?? "GET",
    signal: options.signal,
    headers: {
      Accept: "application/json",
      ...(options.body === undefined ? {} : { "Content-Type": "application/json" }),
      ...(options.headers ?? {}),
    },
    body: options.body === undefined ? undefined : JSON.stringify(options.body),
  });

  if (!response.ok) {
    throw await readError(response);
  }

  if (response.status === 204) {
    return undefined as TResponse;
  }

  return (await response.json()) as TResponse;
}

export async function apiBlob(path: string, options: RequestOptions = {}): Promise<Blob> {
  const response = await fetch(buildUrl(path, options.query), {
    method: options.method ?? "GET",
    signal: options.signal,
    headers: {
      Accept: "application/octet-stream",
    },
  });

  if (!response.ok) {
    throw await readError(response);
  }

  return response.blob();
}
