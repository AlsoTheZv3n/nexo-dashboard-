import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from "react";
import { api, tokenStore, type AuthTokens } from "@/lib/api";

export interface AuthUser {
  id: string;
  username: string;
  role: "Admin" | "Operator" | "Viewer";
}

interface AuthContextValue {
  user: AuthUser | null;
  isAuthenticated: boolean;
  login: (username: string, password: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

interface LoginResponse extends AuthTokens {
  user: AuthUser;
}

const USER_KEY = "nexo.user";

function loadUser(): AuthUser | null {
  try {
    const raw = localStorage.getItem(USER_KEY);
    return raw ? (JSON.parse(raw) as AuthUser) : null;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(() => (tokenStore.get() ? loadUser() : null));

  const login = useCallback(async (username: string, password: string) => {
    const response = await api.post<LoginResponse>("/v1/auth/login", { username, password });
    tokenStore.set({
      accessToken: response.data.accessToken,
      refreshToken: response.data.refreshToken,
      accessExpiresAt: response.data.accessExpiresAt,
    });
    localStorage.setItem(USER_KEY, JSON.stringify(response.data.user));
    setUser(response.data.user);
  }, []);

  const logout = useCallback(() => {
    tokenStore.clear();
    localStorage.removeItem(USER_KEY);
    setUser(null);
  }, []);

  useEffect(() => {
    if (!tokenStore.get()) setUser(null);
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({ user, isAuthenticated: !!user, login, logout }),
    [user, login, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used inside <AuthProvider>");
  return ctx;
}
