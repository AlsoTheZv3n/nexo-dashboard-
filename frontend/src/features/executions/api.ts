import { api } from "@/lib/api";

export type ExecutionStatus = "Pending" | "Running" | "Completed" | "Failed" | "Cancelled";

export interface Execution {
  id: string;
  scriptId: string;
  status: ExecutionStatus;
  stdout: string | null;
  stderr: string | null;
  exitCode: number | null;
  createdAt: string;
  startedAt: string | null;
  completedAt: string | null;
}

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

export async function fetchExecutions(params: { page?: number; pageSize?: number } = {}): Promise<PagedResponse<Execution>> {
  const r = await api.get<PagedResponse<Execution>>("/v1/executions", { params });
  return r.data;
}

export async function fetchExecution(id: string): Promise<Execution> {
  const r = await api.get<Execution>(`/v1/executions/${id}`);
  return r.data;
}

export async function createExecution(scriptId: string, parameters: Record<string, unknown>): Promise<Execution> {
  const r = await api.post<Execution>("/v1/executions", { scriptId, parameters });
  return r.data;
}

export async function cancelExecution(id: string): Promise<Execution> {
  const r = await api.post<Execution>(`/v1/executions/${id}/cancel`);
  return r.data;
}
