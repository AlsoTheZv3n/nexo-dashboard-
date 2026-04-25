import { http, HttpResponse } from "msw";

const tokens = {
  accessToken: "mock-access",
  refreshToken: "mock-refresh",
  accessExpiresAt: new Date(Date.now() + 15 * 60_000).toISOString(),
  user: { id: "u-1", username: "admin", role: "Admin" as const },
};

export const handlers = [
  http.post("/api/v1/auth/login", async ({ request }) => {
    const body = (await request.json()) as { username: string; password: string };
    if (body.username === "admin" && body.password === "admin") {
      return HttpResponse.json(tokens);
    }
    return HttpResponse.json({ title: "Unauthorized", detail: "Invalid credentials." }, { status: 401 });
  }),

  http.get("/api/v1/scripts", () => {
    return HttpResponse.json([
      {
        id: "s-1",
        name: "Get-SystemHealth",
        description: "Checks system health",
        filePath: "Get-SystemHealth.ps1",
        metaJson: '{"parameters":[]}',
        updatedAt: new Date().toISOString(),
      },
      {
        id: "s-2",
        name: "Get-DiskUsage",
        description: "Disk usage per mount",
        filePath: "Get-DiskUsage.ps1",
        metaJson: '{"parameters":[]}',
        updatedAt: new Date().toISOString(),
      },
    ]);
  }),

  http.get("/api/v1/executions", () => {
    return HttpResponse.json({
      items: [
        {
          id: "e-1",
          scriptId: "s-1",
          status: "Completed",
          stdout: "ok",
          stderr: null,
          exitCode: 0,
          createdAt: new Date().toISOString(),
          startedAt: new Date().toISOString(),
          completedAt: new Date().toISOString(),
        },
      ],
      page: 1,
      pageSize: 25,
      total: 1,
    });
  }),

  http.get("/api/v1/metrics/summary", () =>
    HttpResponse.json({
      scriptCount: 7,
      executionsLast24h: 42,
      failuresLast24h: 3,
      averageDurationSeconds: 1.8,
    }),
  ),

  http.get("/api/v1/metrics/timeseries", ({ request }) => {
    const url = new URL(request.url);
    return HttpResponse.json({
      key: url.searchParams.get("key") ?? "executions.completed",
      bucket: url.searchParams.get("bucket") ?? "hour",
      aggregation: url.searchParams.get("aggregation") ?? "count",
      from: url.searchParams.get("from") ?? new Date().toISOString(),
      to: url.searchParams.get("to") ?? new Date().toISOString(),
      points: [
        { bucketStart: new Date(Date.now() - 2 * 3600_000).toISOString(), value: 5, samples: 5 },
        { bucketStart: new Date(Date.now() - 1 * 3600_000).toISOString(), value: 9, samples: 9 },
        { bucketStart: new Date(Date.now()).toISOString(), value: 14, samples: 14 },
      ],
    });
  }),

  http.get("/api/v1/metrics/status-breakdown", () =>
    HttpResponse.json([
      { status: "Completed", count: 30 },
      { status: "Failed", count: 3 },
      { status: "Cancelled", count: 1 },
    ]),
  ),

  http.get("/api/v1/metrics/top-scripts", () =>
    HttpResponse.json([
      { scriptId: "s-1", name: "Get-SystemHealth", executions: 18 },
      { scriptId: "s-2", name: "Get-DiskUsage", executions: 12 },
    ]),
  ),

  http.get("/api/v1/health/live", () =>
    HttpResponse.json({ status: "alive", at: new Date().toISOString() }),
  ),
  http.get("/api/v1/health/ready", () =>
    HttpResponse.json({ status: "ready", db: "ok" }),
  ),
];
