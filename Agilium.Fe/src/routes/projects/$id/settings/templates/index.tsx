import { createFileRoute, Link, useNavigate, useParams } from "@tanstack/react-router";
import * as React from "react";
import { ItemType } from "../../../../../services/items-api";
import { TemplateEditor } from "../../../../../components/specific/template-editor/template-editor";
import { useAuth } from "../../../../../contexts/auth-context";

const TYPE_TABS: { type: ItemType; label: string }[] = [
  { type: ItemType.Feature, label: "Feature" },
  { type: ItemType.UserStory, label: "User Story" },
  { type: ItemType.Task, label: "Task" },
  { type: ItemType.Bug, label: "Bug" },
];

export const Route = createFileRoute("/projects/$id/settings/templates/")({
  validateSearch: (search: Record<string, unknown>): { type: ItemType } => {
    const type = Number(search.type);
    return { type: TYPE_TABS.some((t) => t.type === type) ? (type as ItemType) : ItemType.Feature };
  },
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

  return <ProjectTemplatesPage />;
}

function ProjectTemplatesPage() {
  const { id } = useParams({ from: Route.id });
  const { type: itemType } = Route.useSearch();
  const projectId = Number(id);

  return (
    <div className="p-6">
      <Link to="/projects/$id/settings" params={{ id }} className="text-sm text-blue-600 hover:underline">
        ← Zpět na nastavení projektu
      </Link>

      <h1 className="mt-4 text-2xl font-semibold">Šablony projektu</h1>
      <p className="mt-1 text-sm text-gray-500">
        Úpravy se týkají jen tohoto projektu. Šablona se skládá z tabulek, sloupců (šířka, očekává se součet 12),
        sekcí a atributů.
      </p>

      <nav className="mt-4 flex gap-2 border-b border-gray-200">
        {TYPE_TABS.map((tab) => (
          <Link
            key={tab.type}
            to="/projects/$id/settings/templates"
            params={{ id }}
            search={{ type: tab.type }}
            className={`px-3 py-2 text-sm font-medium ${
              itemType === tab.type
                ? "border-b-2 border-blue-600 text-blue-600"
                : "text-gray-500 hover:text-gray-700"
            }`}
          >
            {tab.label}
          </Link>
        ))}
      </nav>

      <div className="mt-4">
        {/* Keyed by type so switching tabs starts a fresh editor (unsaved changes are guarded by a blocker). */}
        <TemplateEditor key={itemType} projectId={projectId} itemType={itemType} />
      </div>
    </div>
  );
}
