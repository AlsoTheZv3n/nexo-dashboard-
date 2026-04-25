import { defineConfig, devices } from "@playwright/test";

/**
 * E2E config — runs against a fully-booted stack (API + Postgres) on
 * http://localhost:8080 (the SPA + API behind the prod-style nginx) by default.
 *
 * Local one-shot:
 *   docker compose -f docker-compose.dev.yml up -d
 *   pnpm test:e2e
 *
 * Vite dev-server alternative (without docker):
 *   PLAYWRIGHT_BASE_URL=http://localhost:5173 pnpm test:e2e
 *   (but you also need the API on :5000 — see vite.config proxy)
 */
const baseURL = process.env.PLAYWRIGHT_BASE_URL ?? "http://localhost:8080";

export default defineConfig({
  testDir: "./e2e",
  fullyParallel: false,           // serial: most flows mutate shared DB state
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: 1,
  reporter: process.env.CI ? [["github"], ["html", { open: "never" }]] : "list",
  use: {
    baseURL,
    actionTimeout: 10_000,
    navigationTimeout: 30_000,
    trace: "on-first-retry",
    screenshot: "only-on-failure",
    video: process.env.CI ? "retain-on-failure" : "off",
  },
  expect: {
    timeout: 10_000,
  },
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
  ],
});
