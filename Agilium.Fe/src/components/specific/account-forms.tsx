import { Link } from "@tanstack/react-router";
import * as React from "react";
import { ApiError } from "../../services/http-client";
import { accountErrorText } from "../../services/account-errors";
import * as authApi from "../../services/auth-api";

function Field({
  label,
  type,
  value,
  onChange,
  autoComplete,
}: {
  label: string;
  type: "password" | "email";
  value: string;
  onChange: (value: string) => void;
  autoComplete?: string;
}) {
  return (
    <label className="block text-sm font-medium text-gray-700">
      {label}
      <input
        type={type}
        value={value}
        autoComplete={autoComplete}
        onChange={(e) => onChange(e.target.value)}
        className="mt-1 w-full rounded-md border border-gray-300 px-3 py-2 font-normal"
      />
    </label>
  );
}

const primaryButton =
  "w-full rounded-md bg-violet-600 px-4 py-2 text-white hover:bg-violet-700 disabled:opacity-50";

/** Shown after a successful registration: the account exists but must be confirmed by e-mail. */
export function CheckYourEmail({ email }: { email: string }) {
  const [state, setState] = React.useState<"idle" | "sending" | "sent" | "error">("idle");

  async function resend() {
    setState("sending");
    try {
      await authApi.resendVerification(email);
      setState("sent");
    } catch {
      setState("error");
    }
  }

  return (
    <div>
      <p className="text-sm text-gray-700">
        Poslali jsme vám e-mail na <strong>{email}</strong>. Klikněte na odkaz v něm (platí 24 hodin) a registrace je
        hotová.
      </p>
      <button type="button" onClick={resend} disabled={state === "sending"} className={`${primaryButton} mt-4`}>
        Poslat znovu
      </button>
      {state === "sent" && (
        <p role="status" className="mt-2 text-sm text-emerald-700">
          Pokud účet čeká na ověření, e-mail je na cestě (další lze poslat po minutě).
        </p>
      )}
      {state === "error" && (
        <p role="alert" className="mt-2 text-sm text-red-600">
          E-mail se nepodařilo odeslat, zkuste to za chvíli.
        </p>
      )}
      <p className="mt-4 text-center text-sm text-gray-600">
        <Link to="/login" className="text-blue-600 hover:underline">
          Přejít na přihlášení
        </Link>
      </p>
    </div>
  );
}

/** Processes the token from the verification link exactly once (the token is single use). */
export function VerifyEmailResult({ token, pendingJoin }: { token: string | undefined; pendingJoin: boolean }) {
  const [state, setState] = React.useState<"working" | "ok" | "error">(token ? "working" : "error");
  const [message, setMessage] = React.useState("Odkaz je neplatný.");
  const started = React.useRef(false);

  React.useEffect(() => {
    if (!token || started.current) return;
    started.current = true;
    authApi
      .verifyEmail(token)
      .then(() => setState("ok"))
      .catch((error) => {
        setMessage(accountErrorText(error));
        setState("error");
      });
  }, [token]);

  if (state === "working") return <p className="text-gray-500">Ověřuji e-mail…</p>;
  if (state === "ok")
    return (
      <div>
        <p role="status" className="text-emerald-700">
          E-mail byl ověřen. Můžete se přihlásit.
        </p>
        <Link to="/login" className="mt-4 inline-block text-blue-600 hover:underline">
          {pendingJoin ? "Přihlásit se a pokračovat k pozvánce" : "Přejít na přihlášení"}
        </Link>
      </div>
    );
  return (
    <div>
      <p role="alert" className="text-red-600">
        {message}
      </p>
      <Link to="/login" className="mt-4 inline-block text-blue-600 hover:underline">
        Přejít na přihlášení
      </Link>
    </div>
  );
}

export function ForgotPasswordForm() {
  const [email, setEmail] = React.useState("");
  const [done, setDone] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);
  const [busy, setBusy] = React.useState(false);

  async function submit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      await authApi.requestPasswordReset(email);
      setDone(true);
    } catch (err) {
      setError(accountErrorText(err));
    } finally {
      setBusy(false);
    }
  }

  if (done)
    return (
      <p role="status" className="text-sm text-gray-700">
        Pokud účet s tímto e-mailem existuje, poslali jsme na něj odkaz pro obnovení hesla.
      </p>
    );

  return (
    <form onSubmit={submit} className="space-y-4">
      <Field label="E-mail" type="email" value={email} onChange={setEmail} autoComplete="email" />
      {error && (
        <p role="alert" className="text-sm text-red-600">
          {error}
        </p>
      )}
      <button type="submit" disabled={busy || email.trim() === ""} className={primaryButton}>
        Poslat odkaz
      </button>
    </form>
  );
}

export function ResetPasswordForm({ token, onDone }: { token: string | undefined; onDone?: () => void }) {
  const [password, setPassword] = React.useState("");
  const [confirm, setConfirm] = React.useState("");
  const [error, setError] = React.useState<string | null>(null);
  const [done, setDone] = React.useState(false);
  const [busy, setBusy] = React.useState(false);

  async function submit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setError(null);
    if (password.length < 8) return setError("Heslo musí mít alespoň 8 znaků.");
    if (password !== confirm) return setError("Hesla se neshodují.");
    setBusy(true);
    try {
      await authApi.confirmPasswordReset(token ?? "", password);
      setDone(true);
      onDone?.();
    } catch (err) {
      setError(accountErrorText(err));
    } finally {
      setBusy(false);
    }
  }

  if (!token)
    return (
      <p role="alert" className="text-red-600">
        Odkaz je neplatný.
      </p>
    );
  if (done)
    return (
      <div>
        <p role="status" className="text-emerald-700">
          Heslo bylo změněno. Přihlaste se novým heslem.
        </p>
        <Link to="/login" className="mt-4 inline-block text-blue-600 hover:underline">
          Přejít na přihlášení
        </Link>
      </div>
    );

  return (
    <form onSubmit={submit} className="space-y-4">
      <Field label="Nové heslo" type="password" value={password} onChange={setPassword} autoComplete="new-password" />
      <Field label="Nové heslo znovu" type="password" value={confirm} onChange={setConfirm} autoComplete="new-password" />
      {error && (
        <p role="alert" className="text-sm text-red-600">
          {error}
        </p>
      )}
      <button type="submit" disabled={busy} className={primaryButton}>
        Nastavit heslo
      </button>
    </form>
  );
}

export function ChangePasswordForm() {
  const [current, setCurrent] = React.useState("");
  const [password, setPassword] = React.useState("");
  const [confirm, setConfirm] = React.useState("");
  const [error, setError] = React.useState<string | null>(null);
  const [done, setDone] = React.useState(false);
  const [busy, setBusy] = React.useState(false);

  async function submit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setError(null);
    setDone(false);
    if (password.length < 8) return setError("Nové heslo musí mít alespoň 8 znaků.");
    if (password !== confirm) return setError("Hesla se neshodují.");
    setBusy(true);
    try {
      await authApi.changePassword(current, password);
      setDone(true);
      setCurrent("");
      setPassword("");
      setConfirm("");
    } catch (err) {
      setError(err instanceof ApiError && err.status === 400 ? "Současné heslo není správné." : accountErrorText(err));
    } finally {
      setBusy(false);
    }
  }

  return (
    <form onSubmit={submit} className="max-w-sm space-y-4">
      <Field label="Současné heslo" type="password" value={current} onChange={setCurrent} autoComplete="current-password" />
      <Field label="Nové heslo" type="password" value={password} onChange={setPassword} autoComplete="new-password" />
      <Field label="Nové heslo znovu" type="password" value={confirm} onChange={setConfirm} autoComplete="new-password" />
      {error && (
        <p role="alert" className="text-sm text-red-600">
          {error}
        </p>
      )}
      {done && (
        <p role="status" className="text-sm text-emerald-700">
          Heslo bylo změněno. Na ostatních zařízeních budete odhlášeni.
        </p>
      )}
      <button type="submit" disabled={busy} className={primaryButton}>
        Změnit heslo
      </button>
    </form>
  );
}
