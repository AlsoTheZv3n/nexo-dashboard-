import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render as rtlRender, type RenderOptions } from "@testing-library/react";
import type { ReactElement, ReactNode } from "react";
import { MemoryRouter } from "react-router-dom";

export function createTestQueryClient() {
  return new QueryClient({
    defaultOptions: { queries: { retry: false, staleTime: 0 }, mutations: { retry: false } },
  });
}

interface ProvidersProps {
  children: ReactNode;
  initialEntries?: string[];
  queryClient?: QueryClient;
}

export function TestProviders({ children, initialEntries = ["/"], queryClient }: ProvidersProps) {
  const qc = queryClient ?? createTestQueryClient();
  return (
    <QueryClientProvider client={qc}>
      <MemoryRouter initialEntries={initialEntries}>{children}</MemoryRouter>
    </QueryClientProvider>
  );
}

export function renderWithProviders(
  ui: ReactElement,
  options: RenderOptions & { initialEntries?: string[] } = {},
) {
  const { initialEntries, ...rest } = options;
  return rtlRender(ui, {
    wrapper: ({ children }) => <TestProviders initialEntries={initialEntries}>{children}</TestProviders>,
    ...rest,
  });
}
