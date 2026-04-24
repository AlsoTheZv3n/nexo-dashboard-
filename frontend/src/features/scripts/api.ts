import { api } from "@/lib/api";

export interface Script {
  id: string;
  name: string;
  description: string;
  filePath: string;
  metaJson: string;
  updatedAt: string;
}

export interface ScriptMeta {
  parameters: {
    name: string;
    type: "int" | "string" | "bool";
    default?: string | number | boolean;
    required?: boolean;
  }[];
}

export async function fetchScripts(): Promise<Script[]> {
  const r = await api.get<Script[]>("/v1/scripts");
  return r.data;
}

export async function fetchScript(id: string): Promise<Script> {
  const r = await api.get<Script>(`/v1/scripts/${id}`);
  return r.data;
}

export function parseMeta(metaJson: string): ScriptMeta {
  try {
    const parsed = JSON.parse(metaJson) as ScriptMeta;
    return { parameters: parsed.parameters ?? [] };
  } catch {
    return { parameters: [] };
  }
}
