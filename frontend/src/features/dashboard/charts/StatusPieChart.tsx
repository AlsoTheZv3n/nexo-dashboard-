import { Cell, Legend, Pie, PieChart, ResponsiveContainer, Tooltip } from "recharts";
import type { StatusBreakdownRow } from "../api";

const STATUS_COLORS: Record<string, string> = {
  Pending: "hsl(215 16% 47%)",
  Running: "hsl(217 91% 60%)",
  Completed: "hsl(142 72% 45%)",
  Failed: "hsl(0 72% 51%)",
  Cancelled: "hsl(38 92% 50%)",
};

interface Props {
  rows: StatusBreakdownRow[];
}

export function StatusPieChart({ rows }: Props) {
  const data = rows.filter((r) => r.count > 0);
  if (data.length === 0) {
    return <div className="flex h-64 items-center justify-center text-sm text-muted-foreground">No executions in range.</div>;
  }

  return (
    <div className="h-64" data-testid="status-pie-chart">
      <ResponsiveContainer width="100%" height="100%">
        <PieChart>
          <Pie data={data} dataKey="count" nameKey="status" innerRadius={50} outerRadius={85} paddingAngle={2}>
            {data.map((row) => (
              <Cell key={row.status} fill={STATUS_COLORS[row.status] ?? "hsl(215 16% 47%)"} />
            ))}
          </Pie>
          <Tooltip
            contentStyle={{
              background: "hsl(var(--card))",
              border: "1px solid hsl(var(--border))",
              borderRadius: 6,
              color: "hsl(var(--card-foreground))",
              fontSize: 12,
            }}
          />
          <Legend verticalAlign="bottom" wrapperStyle={{ fontSize: 12 }} />
        </PieChart>
      </ResponsiveContainer>
    </div>
  );
}
