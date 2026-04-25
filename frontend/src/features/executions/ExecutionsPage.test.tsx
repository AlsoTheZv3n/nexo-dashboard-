import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import { describe, expect, it } from "vitest";
import { server } from "@/mocks/server";
import { renderWithProviders } from "@/test/testUtils";
import { ExecutionsPage } from "./ExecutionsPage";

function pageOf(items: unknown[], page: number, total: number) {
  return HttpResponse.json({ items, page, pageSize: 25, total });
}

const e = (id: string) => ({
  id,
  scriptId: "s-1",
  status: "Completed",
  stdout: "ok",
  stderr: null,
  exitCode: 0,
  createdAt: new Date().toISOString(),
  startedAt: new Date().toISOString(),
  completedAt: new Date().toISOString(),
});

describe("ExecutionsPage pagination", () => {
  it("shows page summary based on the response total", async () => {
    server.use(http.get("/api/v1/executions", () => pageOf([e("a"), e("b")], 1, 60)));
    renderWithProviders(<ExecutionsPage />);
    await waitFor(() => {
      expect(screen.getByTestId("page-summary")).toHaveTextContent("Page 1 of 3");
    });
    expect(screen.getByTestId("page-summary")).toHaveTextContent("60 total");
  });

  it("Next button advances to page 2 with the new query param", async () => {
    let lastPage: string | null = null;
    server.use(
      http.get("/api/v1/executions", ({ request }) => {
        lastPage = new URL(request.url).searchParams.get("page");
        return pageOf([e("any")], Number(lastPage ?? 1), 60);
      }),
    );
    const user = userEvent.setup();
    renderWithProviders(<ExecutionsPage />);
    await waitFor(() => expect(screen.getByTestId("page-summary")).toHaveTextContent("Page 1 of 3"));
    await user.click(screen.getByRole("button", { name: /next page/i }));
    await waitFor(() => expect(lastPage).toBe("2"));
    await waitFor(() => expect(screen.getByTestId("page-summary")).toHaveTextContent("Page 2 of 3"));
  });

  it("Prev is disabled on page 1", async () => {
    server.use(http.get("/api/v1/executions", () => pageOf([], 1, 0)));
    renderWithProviders(<ExecutionsPage />);
    expect(await screen.findByText("No executions yet.")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /previous page/i })).toBeDisabled();
  });
});
