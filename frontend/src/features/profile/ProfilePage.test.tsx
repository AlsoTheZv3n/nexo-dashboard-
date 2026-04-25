import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import { describe, expect, it } from "vitest";
import { server } from "@/mocks/server";
import { renderWithProviders } from "@/test/testUtils";
import { ProfilePage } from "./ProfilePage";

function setSignedInAdmin() {
  localStorage.setItem(
    "nexo.tokens",
    JSON.stringify({ accessToken: "mock", refreshToken: "mock", accessExpiresAt: new Date().toISOString() }),
  );
  localStorage.setItem("nexo.user", JSON.stringify({ id: "u-1", username: "admin", role: "Admin" }));
}

describe("ProfilePage", () => {
  it("shows the signed-in user's account info", () => {
    setSignedInAdmin();
    renderWithProviders(<ProfilePage />);
    expect(screen.getByTestId("profile-username")).toHaveTextContent("admin");
    expect(screen.getByTestId("profile-role")).toHaveTextContent("Admin");
  });

  it("rejects when new and confirm don't match", async () => {
    setSignedInAdmin();
    const user = userEvent.setup();
    renderWithProviders(<ProfilePage />);

    await user.type(screen.getByLabelText(/current password/i), "old");
    await user.type(screen.getByLabelText(/^new password$/i), "newsecret1");
    await user.type(screen.getByLabelText(/confirm/i), "different");
    await user.click(screen.getByRole("button", { name: /change password/i }));

    expect(screen.getByRole("alert")).toHaveTextContent(/does not match/i);
  });

  it("posts to /profile/change-password on success", async () => {
    setSignedInAdmin();
    let received: unknown = null;
    server.use(
      http.post("/api/v1/profile/change-password", async ({ request }) => {
        received = await request.json();
        return new HttpResponse(null, { status: 204 });
      }),
    );
    const user = userEvent.setup();
    renderWithProviders(<ProfilePage />);

    await user.type(screen.getByLabelText(/current password/i), "old");
    await user.type(screen.getByLabelText(/^new password$/i), "newsecret1");
    await user.type(screen.getByLabelText(/confirm/i), "newsecret1");
    await user.click(screen.getByRole("button", { name: /change password/i }));

    await waitFor(() => {
      expect(received).toEqual({ currentPassword: "old", newPassword: "newsecret1" });
    });
  });
});
