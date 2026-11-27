import { createFileRoute, Link, useNavigate } from "@tanstack/react-router";
import * as React from "react";
import { ChangePasswordForm } from "../components/specific/account-forms";
import { useAuth } from "../contexts/auth-context";

export const Route = createFileRoute("/account")({
  component: RouteComponent,
});

function RouteComponent() {
  const { user, isRestoringSession } = useAuth();
  const navigate = useNavigate();

  React.useEffect(() => {
    if (!isRestoringSession && !user) navigate({ to: "/login" });
  }, [isRestoringSession, user, navigate]);

  if (isRestoringSession || !user) return <div className="p-6 text-gray-500">Načítání…</div>;

  return (
    <div className="p-6">
      <Link to="/projects" className="text-sm text-blue-600 hover:underline">
        ← Zpět na projekty
      </Link>
      <h1 className="mt-4 text-2xl font-semibold">Můj účet</h1>
      <p className="mt-1 text-sm text-gray-600">
        {user.name} {user.surname} ({user.email})
      </p>
      <h2 className="mt-6 mb-3 text-lg font-semibold">Změna hesla</h2>
      <ChangePasswordForm />
    </div>
  );
}
