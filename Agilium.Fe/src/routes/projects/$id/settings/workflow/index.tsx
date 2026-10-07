import { createFileRoute, Link, useNavigate, useParams } from "@tanstack/react-router";
import * as React from "react";
import { WorkflowEditor } from "../../../../../components/specific/workflow-editor";
import { useAuth } from "../../../../../contexts/auth-context";

export const Route = createFileRoute("/projects/$id/settings/workflow/")({
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

  return <WorkflowSettingsPage />;
}

function WorkflowSettingsPage() {
  const { id } = useParams({ from: Route.id });

  return (
    <div className="p-6">
      <Link to="/projects/$id/settings" params={{ id }} className="text-sm text-blue-600 hover:underline">
        ← Zpět na nastavení
      </Link>
      <h1 className="mt-4 text-2xl font-semibold">Workflow (sloupce kanbanu)</h1>
      <p className="mt-1 mb-4 text-sm text-gray-500">
        Změny platí pro celý projekt včetně existujících sprintů. Při zrušení sloupce se jeho položky přesunou do
        nejbližšího levého sloupce. Musí zůstat alespoň 2 sloupce, mezi nimi stav ToDo i Done.
      </p>
      <WorkflowEditor projectId={Number(id)} />
    </div>
  );
}
