import { expect, test } from "@playwright/test";

test.describe("Authentication", () => {
  test("login → dashboard → logout", async ({ page }) => {
    await page.goto("/login");

    await expect(page.getByRole("heading", { name: /sign in/i })).toBeVisible();
    await page.getByLabel(/username/i).fill("admin");
    await page.getByLabel(/password/i).fill("admin");
    await page.getByRole("button", { name: /sign in/i }).click();

    await expect(page).toHaveURL("/");
    await expect(page.getByRole("heading", { name: /overview/i })).toBeVisible();

    await page.getByRole("button", { name: /logout/i }).click();
    await expect(page).toHaveURL(/\/login/);
  });

  test("rejects wrong credentials with a visible error", async ({ page }) => {
    await page.goto("/login");
    await page.getByLabel(/username/i).fill("admin");
    await page.getByLabel(/password/i).fill("definitely-wrong");
    await page.getByRole("button", { name: /sign in/i }).click();

    await expect(page.getByRole("alert")).toContainText(/login failed/i);
    await expect(page).toHaveURL(/\/login/);
  });

  test("unauthenticated routes redirect to /login", async ({ page }) => {
    await page.context().clearCookies();
    await page.goto("/scripts");
    await expect(page).toHaveURL(/\/login/);
  });
});
