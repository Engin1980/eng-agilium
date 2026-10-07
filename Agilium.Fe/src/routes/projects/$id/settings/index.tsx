import { createFileRoute, Link, useNavigate, useParams } from "@tanstack/react-router";
import * as React from "react";
import { ItemType } from "../../../../services/items-api";
import { useAuth } from "../../../../contexts/auth-context";

export const Route = createFileRoute("/projects/$id/settings/")({
  component: RouteComponent,
});

function RouteComponent() {
  const { user, isRestoringSession } = useAuth();
  const navigate = useNavigate();

  React.useEffect(() => {
    if (!isRestoringSession && !user) {
      navigate({ to: "/login" });
    }
  }, [isRestoringSession, user, navigate]);

  if (isRestoringSession || !user) {
    return <div className="p-6 text-gray-500">Načítání…</div>;
  }

  return <ProjectSettingsPage />;
}

function ProjectSettingsPage() {
  const { id } = useParams({ from: Route.id });

  return (
    <div className="p-6">
      <Link to="/projects/$id" params={{ id }} className="text-sm text-blue-600 hover:underline">
        ← Zpět na projekt
      </Link>

      <h1 className="mt-4 text-2xl font-semibold">Nastavení projektu</h1>

      <ul className="mt-4 space-y-2">
        <li>
          <Link
            to="/projects/$id/settings/templates"
            params={{ id }}
            search={{ type: ItemType.Feature }}
            className="text-blue-600 hover:underline"
          >
            Šablony položek (Feature, User Story, Task, Bug)
          </Link>
          <p className="text-sm text-gray-500">Rozložení a atributy detailu jednotlivých typů položek.</p>
        </li>
      </ul>
    </div>
  );
}
