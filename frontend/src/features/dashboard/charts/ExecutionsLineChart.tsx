import { Area, AreaChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from "recharts";
import type { TimeseriesPoint } from "../api";

interface Props {
  points: TimeseriesPoint[];
  bucket: string;
}

export function ExecutionsLineChart({ points, bucket }: Props) {
  const data = points.map((p) => ({
    ts: new Date(p.bucketStart).getTime(),
    value: p.value,
  }));

  const formatTick = (ms: number) => {
    const d = new Date(ms);
    if (bucket === "minute") return d.toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
    if (bucket === "hour") return d.toLocaleString([], { month: "short", day: "numeric", hour: "2-digit" });
    return d.toLocaleDateString([], { month: "short", day: "numeric" });
  };

  return (
    <div className="h-72" data-testid="executions-line-chart">
      <ResponsiveContainer width="100%" height="100%">
        <AreaChart data={data} margin={{ top: 10, right: 10, left: 0, bottom: 0 }}>
          <defs>
            <linearGradient id="fillPrimary" x1="0" y1="0" x2="0" y2="1">
              <stop offset="5%" stopColor="hsl(var(--primary))" stopOpacity={0.35} />
              <stop offset="95%" stopColor="hsl(var(--primary))" stopOpacity={0.02} />
            </linearGradient>
          </defs>
          <CartesianGrid stroke="hsl(var(--border))" strokeDasharray="3 3" vertical={false} />
          <XAxis
            dataKey="ts"
            type="number"
            domain={["dataMin", "dataMax"]}
            tickFormatter={formatTick}
            stroke="hsl(var(--muted-foreground))"
            fontSize={11}
            tickMargin={8}
          />
          <YAxis
            stroke="hsl(var(--muted-foreground))"
            fontSize={11}
            allowDecimals={false}
            width={36}
          />
          <Tooltip
            contentStyle={{
              background: "hsl(var(--card))",
              border: "1px solid hsl(var(--border))",
              borderRadius: 6,
              color: "hsl(var(--card-foreground))",
              fontSize: 12,
            }}
            labelFormatter={(ms) => new Date(Number(ms)).toLocaleString()}
          />
          <Area
            type="monotone"
            dataKey="value"
            stroke="hsl(var(--primary))"
            fill="url(#fillPrimary)"
            strokeWidth={2}
            isAnimationActive={false}
          />
        </AreaChart>
      </ResponsiveContainer>
    </div>
  );
}
