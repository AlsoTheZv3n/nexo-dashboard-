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
];
