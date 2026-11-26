import { createFileRoute, Link, useNavigate, useParams } from "@tanstack/react-router";
import * as React from "react";
import { useAuth } from "../../../../../contexts/auth-context";
import { RolesEditor } from "../../../../../components/specific/roles-editor";
import { useProjectPermissions } from "../../../../../services/projects-queries";

export const Route = createFileRoute("/projects/$id/settings/roles/")({
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

  return <Page />;
}

function Page() {
  const { id } = useParams({ from: Route.id });
  const projectId = Number(id);
  const { canViewMembers } = useProjectPermissions(projectId);

  return (
    <div className="p-6">
      <Link to="/projects/$id/settings" params={{ id }} className="text-sm text-blue-600 hover:underline">
        ← Zpět na nastavení
      </Link>
      <h1 className="mt-4 mb-4 text-2xl font-semibold">Role</h1>
      {canViewMembers ? <RolesEditor projectId={projectId} /> : <p className="text-gray-500">Nemáte oprávnění tuto stránku zobrazit.</p>}
    </div>
  );
}
