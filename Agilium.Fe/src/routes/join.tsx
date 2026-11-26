import { createFileRoute, Link, useNavigate } from "@tanstack/react-router";
import * as React from "react";
import { useAuth } from "../contexts/auth-context";
import { ApiError } from "../services/http-client";
import { useJoinProject } from "../services/members-queries";

/** The key survives the redirect to the login/registration page. */
export const PENDING_JOIN_KEY = "agilium.pendingJoinKey";

export const Route = createFileRoute("/join")({
  validateSearch: (search: Record<string, unknown>): { key?: string } => ({
    key: typeof search.key === "string" ? search.key : undefined,
  }),
  component: RouteComponent,
});

function RouteComponent() {
  const { user, isRestoringSession } = useAuth();
  const { key: keyFromUrl } = Route.useSearch();
  const navigate = useNavigate();

  React.useEffect(() => {
    if (isRestoringSession) return;
    if (!user) {
      if (keyFromUrl) sessionStorage.setItem(PENDING_JOIN_KEY, keyFromUrl);
      navigate({ to: "/login" });
    }
  }, [isRestoringSession, user, keyFromUrl, navigate]);

  if (isRestoringSession || !user) {
    return <div className="p-6 text-gray-500">Načítání…</div>;
  }

  return <JoinForm initialKey={keyFromUrl ?? sessionStorage.getItem(PENDING_JOIN_KEY) ?? ""} />;
}

function JoinForm({ initialKey }: { initialKey: string }) {
  const navigate = useNavigate();
  const join = useJoinProject();
  const [key, setKey] = React.useState(initialKey);

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    try {
      const { projectId } = await join.mutateAsync(key.trim());
      sessionStorage.removeItem(PENDING_JOIN_KEY);
      await navigate({ to: "/projects/$id", params: { id: String(projectId) } });
    } catch {
      // shown from join.error
    }
  }

  return (
    <div className="mx-auto max-w-md p-6">
      <Link to="/projects" className="text-sm text-blue-600 hover:underline">
        ← Zpět na projekty
      </Link>
      <h1 className="mt-4 text-2xl font-semibold">Připojit se k projektu</h1>
      <form onSubmit={handleSubmit} className="mt-4 space-y-3">
        <label className="block text-sm">
          Pozvánkový klíč
          <input
            value={key}
            onChange={(e) => setKey(e.target.value)}
            className="mt-1 w-full rounded border border-gray-300 px-2 py-1 font-mono"
          />
        </label>
        {join.error && (
          <p role="alert" className="text-sm text-red-600">
            {join.error instanceof ApiError ? join.error.message : "Připojení se nezdařilo."}
          </p>
        )}
        <button
          type="submit"
          disabled={join.isPending || key.trim() === ""}
          className="rounded bg-blue-600 px-3 py-1 text-sm font-medium text-white hover:bg-blue-700"
        >
          Připojit se
        </button>
      </form>
    </div>
  );
}
