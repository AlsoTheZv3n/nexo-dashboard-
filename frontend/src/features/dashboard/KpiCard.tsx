import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";

interface KpiCardProps {
  label: string;
  value: string | number | null | undefined;
  hint?: string;
  loading?: boolean;
}

export function KpiCard({ label, value, hint, loading }: KpiCardProps) {
  return (
    <Card>
      <CardHeader>
        <CardDescription>{label}</CardDescription>
        <CardTitle className="text-3xl" data-testid={`kpi-${label}`}>
          {loading ? <Skeleton className="h-8 w-24" /> : (value ?? "—")}
        </CardTitle>
      </CardHeader>
      {hint && <CardContent className="text-xs text-muted-foreground">{hint}</CardContent>}
    </Card>
  );
}
