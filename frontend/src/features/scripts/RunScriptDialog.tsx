import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Loader2, Play, X } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { createExecution } from "@/features/executions/api";
import { parseMeta, type Script, type ScriptMeta } from "./api";

interface Props {
  script: Script;
  onClose: () => void;
}

interface FieldState {
  value: string;
  error?: string;
}

export function RunScriptDialog({ script, onClose }: Props) {
  const meta: ScriptMeta = parseMeta(script.metaJson);
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const overlayRef = useRef<HTMLDivElement>(null);

  const [fields, setFields] = useState<Record<string, FieldState>>(() => {
    const initial: Record<string, FieldState> = {};
    for (const p of meta.parameters) {
      const dflt = p.default === undefined || p.default === null ? "" : String(p.default);
      initial[p.name] = { value: dflt };
    }
    return initial;
  });

  useEffect(() => {
    function onKey(e: KeyboardEvent) {
      if (e.key === "Escape") onClose();
    }
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [onClose]);

  const mutation = useMutation({
    mutationFn: (values: Record<string, unknown>) => createExecution(script.id, values),
    onSuccess: (execution) => {
      toast.success(`Execution started — ${execution.id.slice(0, 8)}…`);
      queryClient.invalidateQueries({ queryKey: ["executions"] });
      onClose();
      navigate(`/executions/${execution.id}`);
    },
    onError: () => toast.error("Failed to start execution."),
  });

  function validate(): Record<string, unknown> | null {
    const payload: Record<string, unknown> = {};
    const next = { ...fields };
    let ok = true;

    for (const p of meta.parameters) {
      const raw = fields[p.name]?.value ?? "";
      if (p.required && raw.trim() === "") {
        next[p.name] = { value: raw, error: "Required" };
        ok = false;
        continue;
      }
      if (raw === "" && !p.required) {
        // Omit unset optional params entirely.
        continue;
      }
      if (p.type === "int") {
        const n = Number.parseInt(raw, 10);
        if (Number.isNaN(n)) {
          next[p.name] = { value: raw, error: "Must be an integer" };
          ok = false;
          continue;
        }
        payload[p.name] = n;
      } else if (p.type === "bool") {
        payload[p.name] = raw === "true";
      } else {
        payload[p.name] = raw;
      }
      next[p.name] = { value: raw };
    }

    setFields(next);
    return ok ? payload : null;
  }

  function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    const payload = validate();
    if (payload !== null) mutation.mutate(payload);
  }

  function setField(name: string, value: string) {
    setFields((prev) => ({ ...prev, [name]: { value } }));
  }

  return (
    <div
      ref={overlayRef}
      role="dialog"
      aria-modal="true"
      aria-labelledby="run-dialog-title"
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
      onClick={(e) => {
        if (e.target === overlayRef.current) onClose();
      }}
    >
      <div className="w-full max-w-md rounded-lg border bg-card p-6 shadow-lg">
        <div className="mb-4 flex items-start justify-between">
          <div>
            <h2 id="run-dialog-title" className="text-lg font-semibold">
              Run {script.name}
            </h2>
            <p className="text-sm text-muted-foreground">{script.description}</p>
          </div>
          <button
            type="button"
            aria-label="Close"
            onClick={onClose}
            className="rounded-md p-1 text-muted-foreground hover:bg-accent hover:text-accent-foreground"
          >
            <X className="h-4 w-4" />
          </button>
        </div>

        <form onSubmit={onSubmit} className="space-y-4" noValidate>
          {meta.parameters.length === 0 ? (
            <p className="text-sm text-muted-foreground">No parameters — this script runs as-is.</p>
          ) : (
            meta.parameters.map((p) => {
              const f = fields[p.name];
              return (
                <div key={p.name} className="space-y-1.5">
                  <label htmlFor={`param-${p.name}`} className="text-sm font-medium">
                    {p.name}
                    {p.required && <span className="text-destructive"> *</span>}
                  </label>
                  {p.type === "bool" ? (
                    <select
                      id={`param-${p.name}`}
                      value={f.value}
                      onChange={(e) => setField(p.name, e.target.value)}
                      className="h-10 w-full rounded-md border border-input bg-background px-3 text-sm"
                    >
                      <option value="">(unset)</option>
                      <option value="true">true</option>
                      <option value="false">false</option>
                    </select>
                  ) : (
                    <Input
                      id={`param-${p.name}`}
                      type={p.type === "int" ? "number" : "text"}
                      value={f.value}
                      onChange={(e) => setField(p.name, e.target.value)}
                      placeholder={p.default === undefined ? "" : String(p.default)}
                      aria-invalid={!!f.error}
                    />
                  )}
                  {f.error && <p className="text-xs text-destructive">{f.error}</p>}
                </div>
              );
            })
          )}

          <div className="flex justify-end gap-2 pt-2">
            <Button type="button" variant="outline" onClick={onClose}>
              Cancel
            </Button>
            <Button type="submit" disabled={mutation.isPending}>
              {mutation.isPending ? (
                <Loader2 className="mr-1 h-4 w-4 animate-spin" />
              ) : (
                <Play className="mr-1 h-4 w-4" />
              )}
              Run
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
