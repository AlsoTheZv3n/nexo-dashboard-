import { expect, test } from "@playwright/test";
import { loginAs } from "./helpers/login";

test.describe("Scripts", () => {
  test.beforeEach(async ({ page }) => {
    await loginAs(page);
  });

  test("scripts table shows the seeded scripts", async ({ page }) => {
    await page.goto("/scripts");
    await expect(page.getByRole("link", { name: "Get-SystemHealth" })).toBeVisible();
    await expect(page.getByRole("link", { name: "Get-DiskUsage" })).toBeVisible();
  });

  test("global filter narrows the table", async ({ page }) => {
    await page.goto("/scripts");
    // The TanStack global filter is substring-against-every-column. Picking
    // "Network" because it only matches Test-NetworkConnectivity in the seeded
    // dataset; "Disk" would falsely match Get-SystemHealth's German description
    // ("CPU, RAM, Disk").
    await page.getByPlaceholder("Filter…").fill("Network");
    await expect(page.getByRole("link", { name: "Test-NetworkConnectivity" })).toBeVisible();
    await expect(page.getByRole("link", { name: "Get-SystemHealth" })).not.toBeVisible();
    await expect(page.getByRole("link", { name: "Get-DiskUsage" })).not.toBeVisible();
  });

  test("opening a script navigates to its detail page", async ({ page }) => {
    await page.goto("/scripts");
    await page.getByRole("link", { name: "Get-SystemHealth" }).click();
    await expect(page).toHaveURL(/\/scripts\/.+/);
    await expect(page.getByRole("heading", { name: "Get-SystemHealth" })).toBeVisible();
    await expect(page.getByText("Parameters")).toBeVisible();
  });

  test("Run dialog opens with parameter inputs (admin)", async ({ page }) => {
    await page.goto("/scripts");
    await page.getByRole("button", { name: "Run Get-SystemHealth" }).click();
    await expect(page.getByRole("dialog")).toBeVisible();
    await expect(page.getByLabel("MinFreeGB")).toBeVisible();
  });
});
