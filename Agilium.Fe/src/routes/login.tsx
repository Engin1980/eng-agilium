import { createFileRoute, Link, useNavigate } from "@tanstack/react-router";
import * as React from "react";
import { CommonForm } from "../components/global/forms/area/common-form";
import { TextInputBlock } from "../components/global/forms/controls/text-input-block";
import { SubmitInput } from "../components/global/forms/controls/button-input";
import { useAuth } from "../contexts/auth-context";
import { ApiError } from "../services/http-client";
import { accountErrorText } from "../services/account-errors";
import { resendVerification } from "../services/auth-api";
import { useTurnstile } from "../components/global/turnstile";

export const Route = createFileRoute("/login")({
  component: RouteComponent,
});

function RouteComponent() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = React.useState("");
  const [password, setPassword] = React.useState("");
  const [error, setError] = React.useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = React.useState(false);
  const [unverified, setUnverified] = React.useState(false);
  const [resent, setResent] = React.useState(false);
  const turnstile = useTurnstile();

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setError(null);
    setUnverified(false);
    setResent(false);
    setIsSubmitting(true);
    try {
      await login(email, password, turnstile.token);
      if (sessionStorage.getItem("agilium.pendingJoinKey")) {
        await navigate({ to: "/join" });
        return;
      }
      await navigate({ to: "/projects" });
    } catch (err) {
      setUnverified(err instanceof ApiError && err.errorKey === "EMAIL_NOT_VERIFIED");
      setError(err instanceof ApiError ? accountErrorText(err) : "Přihlášení se nezdařilo.");
    } finally {
      turnstile.reset();
      setIsSubmitting(false);
    }
  }

  async function handleResend() {
    try {
      await resendVerification(email);
    } finally {
      setResent(true);
    }
  }

  return (
    <CommonForm title="Přihlášení" onSubmit={handleSubmit}>
      <TextInputBlock
        label="E-mail"
        name="email"
        type="email"
        autofocus
        value={email}
        onChange={setEmail}
      />
      <TextInputBlock
        label="Heslo"
        name="password"
        type="password"
        value={password}
        onChange={setPassword}
      />
      {turnstile.widget}
      {error && <div role="alert" className="text-sm text-red-600">{error}</div>}
      {unverified && (
        <div className="text-sm">
          <button type="button" onClick={handleResend} className="text-blue-600 hover:underline">
            Poslat ověřovací e-mail znovu
          </button>
          {resent && <span role="status" className="ml-2 text-emerald-700">Pokud účet čeká na ověření, e-mail je na cestě.</span>}
        </div>
      )}
      <SubmitInput label={isSubmitting ? "Přihlašuji…" : "Přihlásit se"} />
      <p className="text-center text-sm text-gray-600">
        <Link to="/forgot-password" className="text-blue-600 hover:underline">
          Zapomenuté heslo
        </Link>
      </p>
      <p className="text-center text-sm text-gray-600">
        Nemáte účet?{" "}
        <Link to="/register" className="text-blue-600 hover:underline">
          Zaregistrujte se
        </Link>
      </p>
    </CommonForm>
  );
}
