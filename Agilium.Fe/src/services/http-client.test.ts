import { http, HttpResponse } from "msw";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { server } from "../test/server";
import { ApiError, apiRequest, configureHttpClient, onSessionRefreshed } from "./http-client";

const API = "http://localhost:5049/api/v1";

describe("http-client", () => {
  let accessToken: string | null;
  const onUnauthorized = vi.fn();
  const onRefreshed = vi.fn((token: string) => {
    accessToken = token;
  });

  beforeEach(() => {
    accessToken = "old-token";
    onUnauthorized.mockClear();
    onRefreshed.mockClear();
    configureHttpClient(() => accessToken, onUnauthorized);
    onSessionRefreshed(onRefreshed);
  });

  it("sends the access token as a Bearer header", async () => {
    let authorization: string | null = null;
    server.use(
      http.get(`${API}/ping`, ({ request }) => {
        authorization = request.headers.get("Authorization");
        return HttpResponse.json({ ok: true });
      }),
    );

    await apiRequest("/ping");

    expect(authorization).toBe("Bearer old-token");
  });

  it("does not send the token when skipAuth is set", async () => {
    let authorization: string | null = "unset";
    server.use(
      http.post(`${API}/auth/login/admin`, ({ request }) => {
        authorization = request.headers.get("Authorization");
        return HttpResponse.json("jwt");
      }),
    );

    await apiRequest("/auth/login/admin", { method: "POST", skipAuth: true, body: {} });

    expect(authorization).toBeNull();
  });

  it("silently refreshes after a 401 and retries the request exactly once", async () => {
    let calls = 0;
    server.use(
      http.get(`${API}/data`, ({ request }) => {
        calls++;
        return request.headers.get("Authorization") === "Bearer new-token"
          ? HttpResponse.json({ value: 1 })
          : new HttpResponse(null, { status: 401 });
      }),
      http.post(`${API}/auth/refresh`, () => HttpResponse.json("new-token")),
    );

    const result = await apiRequest<{ value: number }>("/data");

    expect(result).toEqual({ value: 1 });
    expect(calls).toBe(2);
    expect(onRefreshed).toHaveBeenCalledWith("new-token");
    expect(onUnauthorized).not.toHaveBeenCalled();
  });

  it("logs the caller out when the refresh fails", async () => {
    server.use(
      http.get(`${API}/data`, () => new HttpResponse(null, { status: 401 })),
      http.post(`${API}/auth/refresh`, () => new HttpResponse(null, { status: 401 })),
    );

    await expect(apiRequest("/data")).rejects.toBeInstanceOf(ApiError);

    expect(onUnauthorized).toHaveBeenCalledTimes(1);
  });

  it("converts problem-details into an ApiError", async () => {
    server.use(
      http.post(`${API}/projects`, () =>
        HttpResponse.json(
          { status: 409, detail: "Entity already exists", errorKey: "ENTITY_ALREADY_EXISTS" },
          { status: 409 },
        ),
      ),
    );

    const error = await apiRequest("/projects", { method: "POST", body: {} }).catch((e: unknown) => e);

    expect(error).toBeInstanceOf(ApiError);
    expect(error).toMatchObject({ status: 409, errorKey: "ENTITY_ALREADY_EXISTS", message: "Entity already exists" });
  });

  it("returns undefined for 204 responses", async () => {
    server.use(http.delete(`${API}/things/1`, () => new HttpResponse(null, { status: 204 })));

    await expect(apiRequest("/things/1", { method: "DELETE" })).resolves.toBeUndefined();
  });
});
