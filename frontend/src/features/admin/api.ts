import { api } from "@/lib/api";

// --- Users ---
export interface User {
  id: string;
  username: string;
  role: "Admin" | "Operator" | "Viewer";
  isActive: boolean;
  createdAt: string;
  lastLoginAt: string | null;
}

export const fetchUsers = async (): Promise<User[]> =>
  (await api.get<User[]>("/v1/users")).data;

export const createUser = async (input: { username: string; password: string; role: User["role"] }) =>
  (await api.post<User>("/v1/users", input)).data;

export const updateUser = async (id: string, input: { role?: User["role"]; isActive?: boolean }) =>
  (await api.patch<User>(`/v1/users/${id}`, input)).data;

export const resetPassword = (id: string, newPassword: string) =>
  api.post(`/v1/users/${id}/reset-password`, { newPassword });

// --- Audit ---
export interface AuditEntry {
  id: string;
  userId: string | null;
  action: string;
  targetType: string | null;
  targetId: string | null;
  detailsJson: string | null;
  ipAddress: string | null;
  timestamp: string;
}

export interface PagedResponse<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

export const fetchAudit = async (params: {
  page?: number;
  pageSize?: number;
  userId?: string;
  action?: string;
  from?: string;
  to?: string;
} = {}): Promise<PagedResponse<AuditEntry>> =>
  (await api.get<PagedResponse<AuditEntry>>("/v1/audit", { params })).data;

// --- API keys ---
export interface ApiKey {
  id: string;
  name: string;
  prefix: string;
  role: User["role"];
  isActive: boolean;
  createdAt: string;
  expiresAt: string | null;
  lastUsedAt: string | null;
}

export interface ApiKeyCreated {
  key: ApiKey;
  plaintext: string;
}

export const fetchApiKeys = async (): Promise<ApiKey[]> =>
  (await api.get<ApiKey[]>("/v1/api-keys")).data;

export const createApiKey = async (input: { name: string; role: User["role"]; expiresInDays: number | null }) =>
  (await api.post<ApiKeyCreated>("/v1/api-keys", input)).data;

export const revokeApiKey = (id: string) => api.delete(`/v1/api-keys/${id}`);

// --- Alerts ---
export type AlertOperator = "GreaterThan" | "LessThan" | "Equals";
export type AlertAggregation = "Avg" | "Sum" | "Min" | "Max" | "Count";

export interface AlertRule {
  id: string;
  name: string;
  metricKey: string;
  operator: string;
  threshold: number;
  windowMinutes: number;
  aggregation: string;
  webhookUrl: string | null;
  isActive: boolean;
  lastEvaluatedAt: string | null;
}

export interface AlertIncident {
  id: string;
  ruleId: string;
  state: "Firing" | "Acknowledged" | "Resolved";
  observedValue: number;
  triggeredAt: string;
  acknowledgedAt: string | null;
  acknowledgedByUserId: string | null;
}

export const fetchAlertRules = async (): Promise<AlertRule[]> =>
  (await api.get<AlertRule[]>("/v1/alerts/rules")).data;

export const createAlertRule = async (input: {
  name: string;
  metricKey: string;
  operator: number;
  threshold: number;
  windowMinutes: number;
  aggregation: number;
  webhookUrl: string | null;
}) => (await api.post<AlertRule>("/v1/alerts/rules", input)).data;

export const deleteAlertRule = (id: string) => api.delete(`/v1/alerts/rules/${id}`);

export const fetchAlertIncidents = async (limit = 50): Promise<AlertIncident[]> =>
  (await api.get<AlertIncident[]>("/v1/alerts/incidents", { params: { limit } })).data;

export const acknowledgeIncident = async (id: string) =>
  (await api.post<AlertIncident>(`/v1/alerts/incidents/${id}/acknowledge`)).data;

// --- Schedules ---
export interface Schedule {
  id: string;
  scriptId: string;
  name: string;
  cronExpression: string;
  parametersJson: string;
  isActive: boolean;
  lastRunAt: string | null;
  nextRunAt: string | null;
}

export const fetchSchedules = async (): Promise<Schedule[]> =>
  (await api.get<Schedule[]>("/v1/schedules")).data;

export const createSchedule = async (input: {
  scriptId: string;
  name: string;
  cronExpression: string;
  parameters?: Record<string, unknown> | null;
}) => (await api.post<Schedule>("/v1/schedules", input)).data;

export const toggleSchedule = async (id: string, isActive: boolean) =>
  (await api.patch<Schedule>(`/v1/schedules/${id}/toggle`, { isActive })).data;

export const deleteSchedule = (id: string) => api.delete(`/v1/schedules/${id}`);
