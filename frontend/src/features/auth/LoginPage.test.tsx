import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { renderWithProviders } from "@/test/testUtils";
import { AuthProvider } from "./AuthContext";
import { LoginPage } from "./LoginPage";

describe("LoginPage", () => {
  it("validates empty submission", async () => {
    const user = userEvent.setup();
    renderWithProviders(
      <AuthProvider>
        <LoginPage />
      </AuthProvider>,
    );
    await user.click(screen.getByRole("button", { name: /sign in/i }));
    expect(await screen.findByText("Username is required")).toBeInTheDocument();
    expect(screen.getByText("Password is required")).toBeInTheDocument();
  });

  it("surfaces 401 errors from the API", async () => {
    const user = userEvent.setup();
    renderWithProviders(
      <AuthProvider>
        <LoginPage />
      </AuthProvider>,
    );
    await user.type(screen.getByLabelText(/username/i), "admin");
    await user.type(screen.getByLabelText(/password/i), "wrong");
    await user.click(screen.getByRole("button", { name: /sign in/i }));
    await waitFor(() => {
      expect(screen.getByRole("alert")).toHaveTextContent(/login failed/i);
    });
  });
});
