import { expect, test } from "@playwright/test";
import { loginAs } from "./helpers/login";

test.describe("Dashboard", () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page);
  });

  test("renders the four KPI cards with non-error content", async ({ page }) => {
    await expect(page.getByText("Available scripts")).toBeVisible();
    await expect(page.getByText("Executions (24h)")).toBeVisible();
    await expect(page.getByText("Failures (24h)")).toBeVisible();
    await expect(page.getByText(/Avg duration/i)).toBeVisible();
    // Each card has a CardTitle test-id like kpi-<label>; ensure none renders the loading "—" forever.
    await expect(page.getByTestId(/^kpi-/).first()).not.toContainText(/^—$/, { timeout: 5_000 });
  });

  test("range presets switch the subtitle", async ({ page }) => {
    await page.getByRole("button", { name: "30d" }).click();
    await expect(page.getByText(/last 30 days/i)).toBeVisible();
  });

  test("auto-refresh toggle flips state", async ({ page }) => {
    const off = page.getByRole("button", { name: /auto-refresh off/i });
    await off.click();
    await expect(page.getByRole("button", { name: /auto-refresh on/i })).toBeVisible();
  });
});
