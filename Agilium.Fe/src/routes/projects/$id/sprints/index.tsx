import { createFileRoute, Link, useNavigate, useParams } from "@tanstack/react-router";
import * as React from "react";
import { ApiError } from "../../../../services/http-client";
import {
  SPRINT_STATE_LABELS,
  SprintState,
  type SprintListItemDto,
} from "../../../../services/sprints-api";
import {
  useCreateSprint,
  useDeleteSprint,
  useSprints,
  useUpdateSprint,
} from "../../../../services/sprints-queries";
import { useAuth } from "../../../../contexts/auth-context";

export const Route = createFileRoute("/projects/$id/sprints/")({
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

  return <SprintsPage />;
}

const toDateInput = (value: string | null) => (value ? value.slice(0, 10) : "");
const toApiDate = (value: string) => (value ? `${value}T00:00:00` : null);

function formatRange(sprint: SprintListItemDto) {
  if (!sprint.startDateTime && !sprint.endDateTime) return "bez termínu";
  return `${toDateInput(sprint.startDateTime) || "…"} – ${toDateInput(sprint.endDateTime) || "…"}`;
}

function errorText(error: unknown) {
  return error instanceof ApiError ? error.message : "Operace se nezdařila.";
}

function SprintsPage() {
  const { id } = useParams({ from: Route.id });
  const projectId = Number(id);
  const { data, isLoading, error } = useSprints(projectId);
  const updateSprint = useUpdateSprint(projectId);
  const deleteSprint = useDeleteSprint(projectId);

  return (
    <div className="p-6">
      <Link to="/projects/$id" params={{ id }} className="text-sm text-blue-600 hover:underline">
        ← Zpět na projekt
      </Link>
      <h1 className="mt-4 text-2xl font-semibold">Sprinty</h1>

      <CreateSprintForm projectId={projectId} />

      {(updateSprint.error || deleteSprint.error) && (
        <p role="alert" className="mt-3 text-sm text-red-600">
          {errorText(updateSprint.error ?? deleteSprint.error)}
        </p>
      )}

      {isLoading && <p className="mt-4 text-gray-500">Načítání…</p>}
      {error && <p className="mt-4 text-red-600">Nepodařilo se načíst sprinty.</p>}
      {data && data.sprints.length === 0 && <p className="mt-4 text-gray-500">Projekt zatím nemá žádný sprint.</p>}

      <ul className="mt-4 space-y-2">
        {data?.sprints.map((sprint) => (
          <li key={sprint.id} className="flex flex-wrap items-center gap-3 rounded border border-gray-200 p-3">
            <Link
              to="/projects/$id/sprints/$sprintId"
              params={{ id, sprintId: String(sprint.id) }}
              className="font-medium text-blue-700 hover:underline"
            >
              {sprint.title}
            </Link>
            <span className="text-sm text-gray-500">{formatRange(sprint)}</span>
            <span className="text-sm text-gray-500">
              hotovo {sprint.doneCount} / {sprint.itemCount}
            </span>

            <span className="mr-auto" />
            <select
              aria-label={`Stav sprintu ${sprint.title}`}
              value={sprint.state}
              disabled={updateSprint.isPending}
              onChange={(e) =>
                updateSprint.mutate({
                  id: sprint.id,
                  title: sprint.title,
                  startDateTime: sprint.startDateTime,
                  endDateTime: sprint.endDateTime,
                  state: Number(e.target.value) as SprintState,
                })
              }
              className="rounded border border-gray-300 px-2 py-1 text-sm"
            >
              {Object.values(SprintState).map((state) => (
                <option key={state} value={state}>
                  {SPRINT_STATE_LABELS[state]}
                </option>
              ))}
            </select>
            <button
              type="button"
              disabled={deleteSprint.isPending}
              onClick={() => {
                if (window.confirm(`Smazat sprint „${sprint.title}“?`)) deleteSprint.mutate(sprint.id);
              }}
              className="text-sm text-red-600 hover:underline"
            >
              Smazat
            </button>
          </li>
        ))}
      </ul>
    </div>
  );
}

function CreateSprintForm({ projectId }: { projectId: number }) {
  const create = useCreateSprint(projectId);
  const [title, setTitle] = React.useState("");
  const [start, setStart] = React.useState("");
  const [end, setEnd] = React.useState("");

  function submit(e: React.FormEvent) {
    e.preventDefault();
    create.mutate(
      { title: title.trim(), startDateTime: toApiDate(start), endDateTime: toApiDate(end) },
      {
        onSuccess: () => {
          setTitle("");
          setStart("");
          setEnd("");
        },
      },
    );
  }

  return (
    <form onSubmit={submit} className="mt-4 flex flex-wrap items-end gap-3 rounded border border-gray-200 p-3">
      <label className="flex flex-col text-sm">
        Název
        <input
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          required
          maxLength={256}
          className="rounded border border-gray-300 px-2 py-1"
        />
      </label>
      <label className="flex flex-col text-sm">
        Začátek
        <input
          type="date"
          value={start}
          onChange={(e) => setStart(e.target.value)}
          className="rounded border border-gray-300 px-2 py-1"
        />
      </label>
      <label className="flex flex-col text-sm">
        Konec
        <input
          type="date"
          value={end}
          min={start || undefined}
          onChange={(e) => setEnd(e.target.value)}
          className="rounded border border-gray-300 px-2 py-1"
        />
      </label>
      <button
        type="submit"
        disabled={create.isPending || !title.trim()}
        className="rounded bg-blue-600 px-4 py-1.5 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-40"
      >
        Vytvořit sprint
      </button>
      {create.error && (
        <p role="alert" className="w-full text-sm text-red-600">
          {errorText(create.error)}
        </p>
      )}
    </form>
  );
}
