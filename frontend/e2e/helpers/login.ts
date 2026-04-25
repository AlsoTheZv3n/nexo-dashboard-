import type { Page } from "@playwright/test";

/**
 * Drives the login page so the rest of the flow can assume an authenticated session.
 * Faster than re-authenticating for every test once we adopt storageState; for now
 * we keep it explicit and test-local.
 */
export async function loginAs(page: Page, username = "admin", password = "admin") {
  await page.goto("/login");
  await page.getByLabel(/username/i).fill(username);
  await page.getByLabel(/password/i).fill(password);
  await page.getByRole("button", { name: /sign in/i }).click();
  await page.waitForURL("/");
}
