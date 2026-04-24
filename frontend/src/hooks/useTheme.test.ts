import { act, renderHook } from "@testing-library/react";
import { beforeEach, describe, expect, it } from "vitest";
import { useTheme } from "./useTheme";

describe("useTheme", () => {
  beforeEach(() => {
    localStorage.clear();
    document.documentElement.classList.remove("dark");
  });

  it("defaults to light when nothing is stored and media query is false", () => {
    const { result } = renderHook(() => useTheme());
    expect(result.current.theme).toBe("light");
    expect(document.documentElement.classList.contains("dark")).toBe(false);
  });

  it("reads a stored preference on mount", () => {
    localStorage.setItem("nexo.theme", "dark");
    const { result } = renderHook(() => useTheme());
    expect(result.current.theme).toBe("dark");
    expect(document.documentElement.classList.contains("dark")).toBe(true);
  });

  it("toggle writes to localStorage and flips the dark class", () => {
    const { result } = renderHook(() => useTheme());
    act(() => result.current.toggle());
    expect(result.current.theme).toBe("dark");
    expect(localStorage.getItem("nexo.theme")).toBe("dark");
    expect(document.documentElement.classList.contains("dark")).toBe(true);

    act(() => result.current.toggle());
    expect(result.current.theme).toBe("light");
    expect(localStorage.getItem("nexo.theme")).toBe("light");
  });
});
