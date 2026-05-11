import { defineConfig, devices } from "@playwright/test";

const port = 5173;
const host = "127.0.0.1";
const baseUrl = `http://${host}:${port}`;

export default defineConfig({
  testDir: "./tests/e2e",
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: process.env.CI ? [["dot"], ["html", { open: "never" }]] : "list",
  use: {
    baseURL: baseUrl,
    trace: "on-first-retry",
    headless: true,
  },
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
  ],
  webServer: {
    command: `npm run dev -- --host ${host} --port ${port}`,
    url: baseUrl,
    reuseExistingServer: !process.env.CI,
    timeout: 120000,
  },
});
