const API_BASE_URL: string =
  import.meta.env.VITE_API_URL ?? "http://localhost:5049/api/v1";

export class ApiError extends Error {
  status: number;
  errorKey: string;

  constructor(status: number, errorKey: string, message: string) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.errorKey = errorKey;
  }
}

type AccessTokenGetter = () => string | null;
type OnUnauthorized = () => void;

let getAccessToken: AccessTokenGetter = () => null;
let onUnauthorized: OnUnauthorized = () => {};

/** Wires the http client to the auth context, without the two importing each other directly. */
export function configureHttpClient(
  accessTokenGetter: AccessTokenGetter,
  unauthorizedHandler: OnUnauthorized,
) {
  getAccessToken = accessTokenGetter;
  onUnauthorized = unauthorizedHandler;
}

type RequestOptions = {
  method?: "GET" | "POST" | "PATCH" | "PUT" | "DELETE";
  body?: unknown;
  /** Skips attaching the Authorization header (login/refresh calls). */
  skipAuth?: boolean;
};

async function parseErrorResponse(response: Response): Promise<ApiError> {
  try {
    const problem = await response.json();
    return new ApiError(
      response.status,
      problem.errorKey ?? "UNKNOWN_ERROR",
      problem.detail ?? response.statusText,
    );
  } catch {
    return new ApiError(response.status, "UNKNOWN_ERROR", response.statusText);
  }
}

async function rawRequest(path: string, options: RequestOptions): Promise<Response> {
  const headers: Record<string, string> = {};
  if (options.body !== undefined) headers["Content-Type"] = "application/json";

  const token = options.skipAuth ? null : getAccessToken();
  if (token) headers["Authorization"] = `Bearer ${token}`;

  return fetch(`${API_BASE_URL}${path}`, {
    method: options.method ?? "GET",
    headers,
    credentials: "include",
    body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
  });
}

/**
 * Performs an authenticated API request. On a 401 (expired access token) it silently
 * refreshes the session once via the httpOnly refresh cookie and retries the request;
 * if that also fails, the caller is logged out.
 */
export async function apiRequest<T>(path: string, options: RequestOptions = {}): Promise<T> {
  let response = await rawRequest(path, options);

  if (response.status === 401 && !options.skipAuth) {
    const refreshed = await tryRefreshSession();
    if (refreshed) {
      response = await rawRequest(path, options);
    } else {
      onUnauthorized();
    }
  }

  if (!response.ok) throw await parseErrorResponse(response);

  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}

let refreshInFlight: Promise<boolean> | null = null;

function tryRefreshSession(): Promise<boolean> {
  refreshInFlight ??= (async () => {
    try {
      const token = await apiRequest<string>("/auth/refresh", {
        method: "POST",
        skipAuth: true,
      });
      refreshedTokenHandler?.(token);
      return true;
    } catch {
      return false;
    } finally {
      refreshInFlight = null;
    }
  })();
  return refreshInFlight;
}

let refreshedTokenHandler: ((token: string) => void) | null = null;

/** Lets the auth context store the freshly refreshed access token. */
export function onSessionRefreshed(handler: (token: string) => void) {
  refreshedTokenHandler = handler;
}
