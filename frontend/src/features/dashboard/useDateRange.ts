import { useMemo, useState } from "react";

export type RangeKey = "1h" | "24h" | "7d" | "30d";

const PRESETS: Record<RangeKey, { label: string; hours: number; bucket: "minute" | "hour" | "day" }> = {
  "1h": { label: "Last hour", hours: 1, bucket: "minute" },
  "24h": { label: "Last 24 hours", hours: 24, bucket: "hour" },
  "7d": { label: "Last 7 days", hours: 24 * 7, bucket: "hour" },
  "30d": { label: "Last 30 days", hours: 24 * 30, bucket: "day" },
};

export function useDateRange(initial: RangeKey = "7d") {
  const [key, setKey] = useState<RangeKey>(initial);

  const range = useMemo(() => {
    const preset = PRESETS[key];
    const to = new Date();
    const from = new Date(to.getTime() - preset.hours * 60 * 60 * 1000);
    return {
      key,
      label: preset.label,
      bucket: preset.bucket,
      from: from.toISOString(),
      to: to.toISOString(),
    };
  }, [key]);

  return { ...range, setKey, presets: Object.keys(PRESETS) as RangeKey[], presetLabel: (k: RangeKey) => PRESETS[k].label };
}
