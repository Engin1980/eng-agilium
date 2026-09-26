import { createFileRoute, Link, useParams } from "@tanstack/react-router";
import { useProject } from "../../../services/projects-queries";

export const Route = createFileRoute("/projects/$id/")({
  component: RouteComponent,
});

function RouteComponent() {
  const { id } = useParams({ from: Route.id });
  const { data: project, isLoading } = useProject(Number(id));

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
          </main>
        </>
      )}
    </div>
  );
}
