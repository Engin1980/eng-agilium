import * as React from "react";

/** Explicit switch (`VITE_TURNSTILE_ENABLED=true`); the site key alone does not turn the verification on. */
export const turnstileEnabled: boolean = import.meta.env.VITE_TURNSTILE_ENABLED === "true";
export const TURNSTILE_SITE_KEY: string = import.meta.env.VITE_TURNSTILE_SITE_KEY ?? "";

const SCRIPT_URL = "https://challenges.cloudflare.com/turnstile/v0/api.js?render=explicit";

type TurnstileApi = {
  render: (
    container: HTMLElement,
    options: {
      sitekey: string;
      callback: (token: string) => void;
      "expired-callback": () => void;
      "error-callback": () => void;
    },
  ) => string;
  reset: (widgetId?: string) => void;
  remove: (widgetId?: string) => void;
};

declare global {
  interface Window {
    turnstile?: TurnstileApi;
  }
}

let scriptPromise: Promise<TurnstileApi> | null = null;

function loadTurnstile(): Promise<TurnstileApi> {
  if (window.turnstile) return Promise.resolve(window.turnstile);
  scriptPromise ??= new Promise<TurnstileApi>((resolve, reject) => {
    const script = document.createElement("script");
    script.src = SCRIPT_URL;
    script.async = true;
    script.onload = () =>
      window.turnstile ? resolve(window.turnstile) : reject(new Error("Turnstile is not available."));
    script.onerror = () => {
      scriptPromise = null;
      reject(new Error("Turnstile script failed to load."));
    };
    document.head.appendChild(script);
  });
  return scriptPromise;
}

export type TurnstileControl = {
  /** Token of the solved challenge ("" when disabled or not solved yet). */
  token: string;
  /** Render this inside the form (null when the verification is disabled). */
  widget: React.ReactNode;
  /** Tokens are single-use: call after every submit attempt to get a new challenge. */
  reset: () => void;
};

/** Optional Cloudflare Turnstile challenge; does nothing unless `VITE_TURNSTILE_ENABLED=true`. */
export function useTurnstile(): TurnstileControl {
  if (turnstileEnabled && TURNSTILE_SITE_KEY.trim() === "")
    throw new Error("VITE_TURNSTILE_ENABLED is true but VITE_TURNSTILE_SITE_KEY is not set.");
  const [token, setToken] = React.useState("");
  const containerRef = React.useRef<HTMLDivElement>(null);
  const widgetIdRef = React.useRef<string | undefined>(undefined);

  React.useEffect(() => {
    if (!turnstileEnabled) return;
    let cancelled = false;
    loadTurnstile()
      .then((api) => {
        if (cancelled || !containerRef.current) return;
        widgetIdRef.current = api.render(containerRef.current, {
          sitekey: TURNSTILE_SITE_KEY,
          callback: setToken,
          "expired-callback": () => setToken(""),
          "error-callback": () => setToken(""),
        });
      })
      .catch(() => setToken(""));
    return () => {
      cancelled = true;
      if (widgetIdRef.current !== undefined) window.turnstile?.remove(widgetIdRef.current);
      widgetIdRef.current = undefined;
    };
  }, []);

  const reset = React.useCallback(() => {
    if (!turnstileEnabled) return;
    setToken("");
    if (widgetIdRef.current !== undefined) window.turnstile?.reset(widgetIdRef.current);
  }, []);

  const widget = turnstileEnabled ? <div ref={containerRef} className="flex justify-center" /> : null;
  return { token, widget, reset };
}
