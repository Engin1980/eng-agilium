import { apiRequest } from "./http-client";

export type LoggedInUser = {
  email: string;
  name: string;
  surname: string;
};

/** Decodes the non-verified claims of a JWT for display purposes only (the server verifies the signature). */
export function decodeUserFromAccessToken(accessToken: string): LoggedInUser {
  const payload = JSON.parse(atob(accessToken.split(".")[1]));
  return { email: payload.email, name: payload.name, surname: payload.surname };
}

export function login(email: string, password: string): Promise<string> {
  return apiRequest<string>("/auth/login/admin", {
    method: "POST",
    skipAuth: true,
    body: { email, password, rememberMe: true, turnstileToken: "" },
  });
}

export function register(
  email: string,
  name: string,
  surname: string,
  password: string,
): Promise<{ id: number }> {
  return apiRequest<{ id: number }>("/auth/users", {
    method: "POST",
    skipAuth: true,
    body: { email, name, surname, password },
  });
}

export function refresh(): Promise<string> {
  return apiRequest<string>("/auth/refresh", { method: "POST", skipAuth: true });
}

export function logout(): Promise<void> {
  return apiRequest<void>("/auth/logout", {
    method: "POST",
    skipAuth: true,
    body: { refreshToken: "" },
  });
}
