import * as React from "react";
import {
  configureHttpClient,
  onSessionRefreshed,
} from "../services/http-client";
import * as authApi from "../services/auth-api";
import type { LoggedInUser } from "../services/auth-api";

type AuthContextValue = {
  user: LoggedInUser | null;
  /** True while the initial silent-refresh (session restore) is in progress. */
  isRestoringSession: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
};

const AuthContext = React.createContext<AuthContextValue | null>(null);

/**
 * Access token is kept in memory only (never localStorage) to limit exposure to XSS.
 * Session survives a page reload via the httpOnly refresh-token cookie (SameSite=Strict),
 * which the http client uses to silently mint a new access token on startup and on 401s.
 */
export function AuthProvider({ children }: { children: React.ReactNode }) {
  const accessTokenRef = React.useRef<string | null>(null);
  const [user, setUser] = React.useState<LoggedInUser | null>(null);
  const [isRestoringSession, setIsRestoringSession] = React.useState(true);

  const applyToken = React.useCallback((token: string) => {
    accessTokenRef.current = token;
    setUser(authApi.decodeUserFromAccessToken(token));
  }, []);

  const clearSession = React.useCallback(() => {
    accessTokenRef.current = null;
    setUser(null);
  }, []);

  React.useEffect(() => {
    configureHttpClient(() => accessTokenRef.current, clearSession);
    onSessionRefreshed(applyToken);
  }, [applyToken, clearSession]);

  React.useEffect(() => {
    authApi
      .refresh()
      .then(applyToken)
      .catch(() => clearSession())
      .finally(() => setIsRestoringSession(false));
  }, [applyToken, clearSession]);

  const login = React.useCallback(
    async (email: string, password: string) => {
      const token = await authApi.login(email, password);
      applyToken(token);
    },
    [applyToken],
  );

  const logout = React.useCallback(async () => {
    try {
      await authApi.logout();
    } finally {
      clearSession();
    }
  }, [clearSession]);

  return (
    <AuthContext.Provider value={{ user, isRestoringSession, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}

export function useAuth(): AuthContextValue {
  const ctx = React.useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within an AuthProvider");
  return ctx;
}
