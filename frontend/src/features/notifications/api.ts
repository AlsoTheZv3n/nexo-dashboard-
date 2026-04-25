import { api } from "@/lib/api";

export interface Notification {
  id: string;
  kind: string;
  title: string;
  body: string;
  severity: "critical" | "info";
  triggeredAt: string;
  linkPath: string | null;
}

export interface NotificationsResponse {
  items: Notification[];
  unreadCount: number;
}

export async function fetchNotifications(): Promise<NotificationsResponse> {
  const r = await api.get<NotificationsResponse>("/v1/notifications");
  return r.data;
}
