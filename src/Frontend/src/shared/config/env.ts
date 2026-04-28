interface RuntimeConfig {
  apiBaseUrl: string;
  apiVersion: string;
  defaultProjectId: number;
}

function readString(value: string | undefined, fallback: string): string {
  const normalized = value?.trim();
  return normalized && normalized.length > 0 ? normalized : fallback;
}

function readNumber(value: string | undefined, fallback: number): number {
  const parsed = Number(value);
  return Number.isFinite(parsed) && parsed > 0 ? parsed : fallback;
}

export const appConfig: RuntimeConfig = {
  apiBaseUrl: readString(import.meta.env.VITE_API_BASE_URL, "http://localhost:5000"),
  apiVersion: readString(import.meta.env.VITE_API_VERSION, "1"),
  defaultProjectId: readNumber(import.meta.env.VITE_DEFAULT_PROJECT_ID, 1),
};
