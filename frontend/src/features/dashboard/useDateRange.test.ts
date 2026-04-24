import { act, renderHook } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { useDateRange } from "./useDateRange";

describe("useDateRange", () => {
  it("defaults to 7 days with hour buckets", () => {
    const { result } = renderHook(() => useDateRange());
    expect(result.current.key).toBe("7d");
    expect(result.current.bucket).toBe("hour");
    const hours = (Date.parse(result.current.to) - Date.parse(result.current.from)) / 3600_000;
    expect(Math.round(hours)).toBe(24 * 7);
  });

  it("switches to 30d with day buckets", () => {
    const { result } = renderHook(() => useDateRange("7d"));
    act(() => result.current.setKey("30d"));
    expect(result.current.key).toBe("30d");
    expect(result.current.bucket).toBe("day");
  });

  it("produces minute buckets for the 1h preset", () => {
    const { result } = renderHook(() => useDateRange("1h"));
    expect(result.current.bucket).toBe("minute");
    const hours = (Date.parse(result.current.to) - Date.parse(result.current.from)) / 3600_000;
    expect(Math.round(hours)).toBe(1);
  });
});
