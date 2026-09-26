import { createFileRoute, Link, useNavigate } from "@tanstack/react-router";
import * as React from "react";
import { CommonForm } from "../components/global/forms/area/common-form";
import { TextInputBlock } from "../components/global/forms/controls/text-input-block";
import { SubmitInput } from "../components/global/forms/controls/button-input";
import { useAuth } from "../contexts/auth-context";
import { ApiError } from "../services/http-client";
import * as authApi from "../services/auth-api";

export const Route = createFileRoute("/register")({
  component: RouteComponent,
});

function RouteComponent() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = React.useState("");
  const [name, setName] = React.useState("");
  const [surname, setSurname] = React.useState("");
  const [password, setPassword] = React.useState("");
  const [passwordConfirm, setPasswordConfirm] = React.useState("");
  const [error, setError] = React.useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = React.useState(false);

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setError(null);

    if (password !== passwordConfirm) {
      setError("Hesla se neshodují.");
      return;
    }

    setIsSubmitting(true);
    try {
      await authApi.register(email, name, surname, password);
      await login(email, password);
      await navigate({ to: "/projects" });
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Registrace se nezdařila.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <CommonForm title="Registrace" onSubmit={handleSubmit}>
      <TextInputBlock
        label="E-mail"
        name="email"
        type="email"
        autofocus
        value={email}
        onChange={setEmail}
      />
      <TextInputBlock label="Jméno" name="name" type="text" value={name} onChange={setName} />
      <TextInputBlock
        label="Příjmení"
        name="surname"
        type="text"
        value={surname}
        onChange={setSurname}
      />
      <TextInputBlock
        label="Heslo"
        name="password"
        type="password"
        note="Minimálně 8 znaků."
        value={password}
        onChange={setPassword}
      />
      <TextInputBlock
        label="Heslo znovu"
        name="passwordConfirm"
        type="password"
        value={passwordConfirm}
        onChange={setPasswordConfirm}
      />
      {error && <div className="text-sm text-red-600">{error}</div>}
      <SubmitInput label={isSubmitting ? "Registruji…" : "Zaregistrovat se"} />
      <p className="text-center text-sm text-gray-600">
        Už máte účet?{" "}
        <Link to="/login" className="text-blue-600 hover:underline">
          Přihlaste se
        </Link>
      </p>
    </CommonForm>
  );
}
