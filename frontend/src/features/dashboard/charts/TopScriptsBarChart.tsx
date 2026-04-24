import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts";
import type { TopScriptRow } from "../api";

interface Props {
  rows: TopScriptRow[];
}

export function TopScriptsBarChart({ rows }: Props) {
  if (rows.length === 0) {
    return <div className="flex h-64 items-center justify-center text-sm text-muted-foreground">No runs recorded yet.</div>;
  }

  return (
    <div className="h-64" data-testid="top-scripts-bar-chart">
      <ResponsiveContainer width="100%" height="100%">
        <BarChart data={rows} layout="vertical" margin={{ top: 8, right: 16, left: 0, bottom: 0 }}>
          <CartesianGrid stroke="hsl(var(--border))" strokeDasharray="3 3" horizontal={false} />
          <XAxis
            type="number"
            allowDecimals={false}
            stroke="hsl(var(--muted-foreground))"
            fontSize={11}
          />
          <YAxis
            type="category"
            dataKey="name"
            width={140}
            stroke="hsl(var(--muted-foreground))"
            fontSize={11}
          />
          <Tooltip
            contentStyle={{
              background: "hsl(var(--card))",
              border: "1px solid hsl(var(--border))",
              borderRadius: 6,
              color: "hsl(var(--card-foreground))",
              fontSize: 12,
            }}
          />
          <Bar
            dataKey="executions"
            fill="hsl(var(--primary))"
            radius={[0, 4, 4, 0]}
            isAnimationActive={false}
          />
        </BarChart>
      </ResponsiveContainer>
    </div>
  );
}
