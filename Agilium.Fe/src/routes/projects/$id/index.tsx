import { createFileRoute, Link, useNavigate, useParams } from "@tanstack/react-router";
import * as React from "react";
import { useProject } from "../../../services/projects-queries";
import { ItemTree } from "../../../components/specific/item-tree";
import { useAuth } from "../../../contexts/auth-context";

export const Route = createFileRoute("/projects/$id/")({
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

  return <ProjectDetail />;
}

function ProjectDetail() {
  const { id } = useParams({ from: Route.id });
  const projectId = Number(id);
  const { data: project, isLoading } = useProject(projectId);

  return (
    <div className="p-6">
      <Link to="/projects" className="text-sm text-blue-600 hover:underline">
        ← Zpět na projekty
      </Link>

      {isLoading && <p className="mt-4 text-gray-500">Načítání…</p>}
      {!isLoading && !project && (
        <p className="mt-4 text-gray-500">Projekt nenalezen.</p>
      )}

      {project && (
        <>
          <header className="mt-4">
            <h1 className="text-2xl font-semibold">{project.title}</h1>
            <span
              className={
                project.status === 1
                  ? "text-sm text-emerald-600"
                  : "text-sm text-gray-500"
              }
            >
              {project.status === 1 ? "Active" : "Inactive"}
            </span>
          </header>
          <main className="mt-4 space-y-2">
            <p className="text-gray-700">{project.description}</p>
            <p className="text-sm text-gray-500">{project.memberCount} členů</p>
            <Link
              to="/projects/$id/settings"
              params={{ id }}
              className="inline-block text-sm text-blue-600 hover:underline"
            >
              Nastavení projektu →
            </Link>
          </main>

          <div className="mt-6">
            <ItemTree projectId={projectId} />
          </div>
        </>
      )}
    </div>
  );
}
