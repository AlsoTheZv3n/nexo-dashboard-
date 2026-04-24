import axios, { AxiosError, type InternalAxiosRequestConfig } from "axios";

const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "/api";

export const api = axios.create({
  baseURL: BASE_URL,
  timeout: 15000,
});

export interface AuthTokens {
  accessToken: string;
  refreshToken: string;
  accessExpiresAt: string;
}

const STORAGE_KEY = "nexo.tokens";

export const tokenStore = {
  get(): AuthTokens | null {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      return raw ? (JSON.parse(raw) as AuthTokens) : null;
    } catch {
      return null;
    }
  },
  set(tokens: AuthTokens) {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(tokens));
  },
  clear() {
    localStorage.removeItem(STORAGE_KEY);
  },
};

api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const tokens = tokenStore.get();
  if (tokens?.accessToken) {
    config.headers.Authorization = `Bearer ${tokens.accessToken}`;
  }
  return config;
});

let refreshPromise: Promise<AuthTokens> | null = null;

api.interceptors.response.use(
  (r) => r,
  async (error: AxiosError) => {
    const original = error.config as InternalAxiosRequestConfig & { _retry?: boolean };
    if (error.response?.status !== 401 || original?._retry) {
      return Promise.reject(error);
    }
    const tokens = tokenStore.get();
    if (!tokens?.refreshToken) {
      tokenStore.clear();
      return Promise.reject(error);
    }
    original._retry = true;
    refreshPromise ??= axios
      .post<AuthTokens>(`${BASE_URL}/v1/auth/refresh`, { refreshToken: tokens.refreshToken })
      .then((r) => {
        tokenStore.set(r.data);
        return r.data;
      })
      .catch((e) => {
        tokenStore.clear();
        throw e;
      })
      .finally(() => {
        refreshPromise = null;
      });

    try {
      const fresh = await refreshPromise;
      original.headers.Authorization = `Bearer ${fresh.accessToken}`;
      return api(original);
    } catch (e) {
      return Promise.reject(e);
    }
  },
);
