import { defineConfig, devices } from "@playwright/test";

/**
 * E2E config — runs against a fully-booted stack (API + Postgres) on
 * http://localhost:8090 (the SPA + API behind the prod-style nginx) by default.
 * The frontend container's nginx proxies /api/* to api:8080 inside the docker
 * network, so the suite never depends on the API port being host-published.
 *
 * Local one-shot:
 *   docker compose -f docker-compose.dev.yml up -d
 *   pnpm test:e2e
 *
 * Vite dev-server alternative (without docker):
 *   PLAYWRIGHT_BASE_URL=http://localhost:5173 pnpm test:e2e
 *   (also needs the API reachable — see vite.config proxy)
 *
 * Override port via env if 8090 collides:
 *   FRONTEND_HOST_PORT=9090 docker compose -f docker-compose.dev.yml up -d
 *   PLAYWRIGHT_BASE_URL=http://localhost:9090 pnpm test:e2e
 */
const baseURL = process.env.PLAYWRIGHT_BASE_URL ?? "http://localhost:8090";

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
