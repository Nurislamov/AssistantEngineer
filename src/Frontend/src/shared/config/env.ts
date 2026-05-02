interface RuntimeConfig {
  apiBaseUrl: string;
  apiVersion: string;
}

function readString(value: string | undefined, fallback: string): string {
  const normalized = value?.trim();
  return normalized && normalized.length > 0 ? normalized : fallback;
}

export const appConfig: RuntimeConfig = {
  apiBaseUrl: readString(import.meta.env.VITE_API_BASE_URL, "http://localhost:5194"),
  apiVersion: readString(import.meta.env.VITE_API_VERSION, "1"),
};
