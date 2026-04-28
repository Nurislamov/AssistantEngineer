export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
}

export interface ApiErrorPayload {
  message: string;
  status: number;
  details?: ProblemDetails | string;
}
