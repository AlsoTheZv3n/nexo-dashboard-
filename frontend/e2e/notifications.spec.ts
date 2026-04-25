import { expect, test } from "@playwright/test";
import { loginAs } from "./helpers/login";

test.describe("Notification bell", () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page);
  });

  test("renders in the header (no badge when nothing is firing)", async ({ page }) => {
    await expect(page.getByTestId("notification-bell")).toBeVisible();
    // The seeded demo data does NOT pre-create any incidents (the AlertEvaluator
    // hasn't ticked yet on a fresh DB), so the badge should be hidden.
    await expect(page.getByTestId("notification-badge")).toHaveCount(0);
  });

  test("opens a dropdown with All-clear when there are no notifications", async ({ page }) => {
    await page.getByTestId("notification-bell").click();
    const dropdown = page.getByTestId("notification-dropdown");
    await expect(dropdown).toBeVisible();
    await expect(dropdown).toContainText(/all clear/i);
  });

  test("Escape closes the dropdown", async ({ page }) => {
    await page.getByTestId("notification-bell").click();
    await expect(page.getByTestId("notification-dropdown")).toBeVisible();
    await page.keyboard.press("Escape");
    await expect(page.getByTestId("notification-dropdown")).toHaveCount(0);
  });
});
