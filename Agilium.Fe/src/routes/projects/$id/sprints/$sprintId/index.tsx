import { createFileRoute, Link, useNavigate, useParams } from "@tanstack/react-router";
import * as React from "react";
import { SprintBoard } from "../../../../../components/specific/sprint-board";
import { SPRINT_STATE_LABELS } from "../../../../../services/sprints-api";
import { useSprintBoard } from "../../../../../services/sprints-queries";
import { useAuth } from "../../../../../contexts/auth-context";

export const Route = createFileRoute("/projects/$id/sprints/$sprintId/")({
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

  return <SprintBoardPage />;
}

function SprintBoardPage() {
  const { id, sprintId } = useParams({ from: Route.id });
  const { data: board, isLoading, error } = useSprintBoard(Number(sprintId));

  return (
    <div className="p-6">
      <Link to="/projects/$id/sprints" params={{ id }} className="text-sm text-blue-600 hover:underline">
        ← Zpět na sprinty
      </Link>

      {isLoading && <p className="mt-4 text-gray-500">Načítání…</p>}
      {error && <p className="mt-4 text-red-600">Sprint se nepodařilo načíst.</p>}

      {board && (
        <>
          <header className="mt-4 mb-4">
            <h1 className="text-2xl font-semibold">{board.sprint.title}</h1>
            <span className="text-sm text-gray-500">{SPRINT_STATE_LABELS[board.sprint.state]}</span>
          </header>
          <SprintBoard projectId={Number(id)} board={board} />
        </>
      )}
    </div>
  );
}
