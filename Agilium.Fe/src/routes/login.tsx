import { createFileRoute, useNavigate } from "@tanstack/react-router";
import * as React from "react";
import { CommonForm } from "../components/global/forms/area/common-form";
import { TextInputBlock } from "../components/global/forms/controls/text-input-block";
import { SubmitInput } from "../components/global/forms/controls/button-input";
import { useAuth } from "../contexts/auth-context";
import { ApiError } from "../services/http-client";

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

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setError(null);
    setIsSubmitting(true);
    try {
      await login(email, password);
      await navigate({ to: "/projects" });
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Přihlášení se nezdařilo.");
    } finally {
      setIsSubmitting(false);
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
      {error && <div className="text-sm text-red-600">{error}</div>}
      <SubmitInput label={isSubmitting ? "Přihlašuji…" : "Přihlásit se"} />
    </CommonForm>
  );
}
