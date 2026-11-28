/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_API_URL?: string;
  /** "true" switches the Turnstile widget on (then the site key is required). */
  readonly VITE_TURNSTILE_ENABLED?: string;
  /** Cloudflare Turnstile public site key. */
  readonly VITE_TURNSTILE_SITE_KEY?: string;
}
