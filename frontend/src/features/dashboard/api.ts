import { api } from "@/lib/api";

export interface Summary {
  scriptCount: number;
  executionsLast24h: number;
  failuresLast24h: number;
  averageDurationSeconds: number;
}

export interface TimeseriesPoint {
  bucketStart: string;
  value: number;
  samples: number;
}

export interface Timeseries {
  key: string;
  bucket: string;
  aggregation: string;
  from: string;
  to: string;
  points: TimeseriesPoint[];
}

export interface StatusBreakdownRow {
  status: string;
  count: number;
}

export interface TopScriptRow {
  scriptId: string;
  name: string;
  executions: number;
}

export type Bucket = "minute" | "hour" | "day" | "week";
export type Aggregation = "avg" | "sum" | "min" | "max" | "count";

export async function fetchSummary(): Promise<Summary> {
  const r = await api.get<Summary>("/v1/metrics/summary");
  return r.data;
}

export async function fetchTimeseries(params: {
  key: string;
  from?: string;
  to?: string;
  bucket?: Bucket;
  aggregation?: Aggregation;
}): Promise<Timeseries> {
  const r = await api.get<Timeseries>("/v1/metrics/timeseries", { params });
  return r.data;
}

export async function fetchStatusBreakdown(params: { from?: string; to?: string } = {}): Promise<StatusBreakdownRow[]> {
  const r = await api.get<StatusBreakdownRow[]>("/v1/metrics/status-breakdown", { params });
  return r.data;
}

export async function fetchTopScripts(params: { from?: string; to?: string; limit?: number } = {}): Promise<TopScriptRow[]> {
  const r = await api.get<TopScriptRow[]>("/v1/metrics/top-scripts", { params });
  return r.data;
}
