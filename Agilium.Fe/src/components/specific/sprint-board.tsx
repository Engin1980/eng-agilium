import * as React from "react";
import { Link } from "@tanstack/react-router";
import { ApiError } from "../../services/http-client";
import {
  SprintState,
  WORKFLOW_STATUS_LABELS,
  WorkflowStateType,
  type BoardCardDto,
  type BoardColumnDto,
  type BoardDto,
  type BoardParentDto,
} from "../../services/sprints-api";
import {
  useAssignSprintItem,
  useBacklog,
  useUnassignSprintItem,
  useUpdateSprintItemState,
} from "../../services/sprints-queries";

const STATUS_CLASSES: Record<WorkflowStateType, string> = {
  [WorkflowStateType.ToDo]: "bg-gray-100 text-gray-700",
  [WorkflowStateType.Active]: "bg-blue-100 text-blue-700",
  [WorkflowStateType.Done]: "bg-emerald-100 text-emerald-700",
};

const ITEM_TYPE_LABELS: Record<number, string> = { 1: "Task", 2: "Bug" };

function errorMessage(error: unknown): string | null {
  if (!error) return null;
  return error instanceof ApiError ? error.message : "Operace se nezdařila.";
}

/** Kanban board of one sprint: columns = project workflow states, cards = tasks / bugs of the sprint. */
export function SprintBoard({ projectId, board }: { projectId: number; board: BoardDto }) {
  const sprintId = board.sprint.id;
  const readOnly = board.sprint.state === SprintState.Completed;
  const move = useUpdateSprintItemState(sprintId);
  const unassign = useUnassignSprintItem(projectId);
  const [dragOverColumn, setDragOverColumn] = React.useState<number | null>(null);

  function moveCard(itemId: number, column: BoardColumnDto) {
    const current = board.columns.find((c) => c.cards.some((card) => card.itemId === itemId));
    if (current && current.id !== column.id) move.mutate({ itemId, workflowStateId: column.id });
  }

  const error = errorMessage(move.error) ?? errorMessage(unassign.error);

  return (
    <div className="space-y-6">
      {error && (
        <p role="alert" className="text-sm text-red-600">
          {error}
        </p>
      )}

      <div className="flex gap-4 overflow-x-auto pb-2">
        {board.columns.map((column) => (
          <section
            key={column.id}
            aria-label={column.title}
            onDragOver={(e) => {
              if (readOnly) return;
              e.preventDefault();
              setDragOverColumn(column.id);
            }}
            onDragLeave={() => setDragOverColumn((id) => (id === column.id ? null : id))}
            onDrop={(e) => {
              e.preventDefault();
              setDragOverColumn(null);
              const itemId = Number(e.dataTransfer.getData("text/plain"));
              if (!readOnly && Number.isInteger(itemId)) moveCard(itemId, column);
            }}
            className={`min-h-48 w-72 shrink-0 rounded-lg border p-3 ${
              dragOverColumn === column.id ? "border-blue-400 bg-blue-50" : "border-gray-200 bg-gray-50"
            }`}
          >
            <h2 className="mb-3 flex items-center justify-between font-semibold text-gray-800">
              <span>{column.title}</span>
              <span className="rounded-full bg-white px-2 text-xs text-gray-500">{column.cards.length}</span>
            </h2>
            <ul className="space-y-2">
              {column.cards.map((card) => (
                <KanbanCard
                  key={card.itemId}
                  projectId={projectId}
                  card={card}
                  columns={board.columns}
                  currentColumnId={column.id}
                  readOnly={readOnly}
                  onMove={(target) => moveCard(card.itemId, target)}
                  onUnassign={() => unassign.mutate({ sprintId, itemId: card.itemId })}
                />
              ))}
            </ul>
          </section>
        ))}
      </div>

      <FeatureOverview features={board.features} />

      {!readOnly && <BacklogPanel projectId={projectId} sprintId={sprintId} />}
    </div>
  );
}

function KanbanCard({
  projectId,
  card,
  columns,
  currentColumnId,
  readOnly,
  onMove,
  onUnassign,
}: {
  projectId: number;
  card: BoardCardDto;
  columns: BoardColumnDto[];
  currentColumnId: number;
  readOnly: boolean;
  onMove: (column: BoardColumnDto) => void;
  onUnassign: () => void;
}) {
  const context = [card.featureTitle, card.userStoryTitle].filter(Boolean).join(" › ");

  return (
    <li
      draggable={!readOnly}
      onDragStart={(e) => {
        e.dataTransfer.setData("text/plain", String(card.itemId));
        e.dataTransfer.effectAllowed = "move";
      }}
      className={`rounded border border-gray-200 bg-white p-2 text-sm shadow-sm ${readOnly ? "" : "cursor-grab"}`}
    >
      <div className="flex items-start justify-between gap-2">
        <Link
          to="/projects/$id/items/$itemId"
          params={{ id: String(projectId), itemId: String(card.itemId) }}
          className="font-medium text-blue-700 hover:underline"
          draggable={false}
        >
          {card.title}
        </Link>
        <span className="shrink-0 rounded bg-gray-100 px-1.5 text-xs text-gray-600">
          {ITEM_TYPE_LABELS[card.type] ?? "?"}
        </span>
      </div>
      {context && <p className="mt-1 text-xs text-gray-500">{context}</p>}
      {card.assigneeName && <p className="mt-1 text-xs text-gray-500">👤 {card.assigneeName}</p>}

      {!readOnly && (
        <div className="mt-2 flex items-center gap-2">
          {/* Keyboard / touch alternative to drag & drop. */}
          <select
            aria-label={`Přesunout kartu ${card.title}`}
            value={currentColumnId}
            onChange={(e) => {
              const target = columns.find((c) => c.id === Number(e.target.value));
              if (target) onMove(target);
            }}
            className="min-w-0 flex-1 rounded border border-gray-300 px-1 py-0.5 text-xs"
          >
            {columns.map((c) => (
              <option key={c.id} value={c.id}>
                {c.title}
              </option>
            ))}
          </select>
          <button type="button" onClick={onUnassign} className="text-xs text-red-600 hover:underline">
            Odebrat
          </button>
        </div>
      )}
    </li>
  );
}

function StatusBadge({ status }: { status: WorkflowStateType }) {
  return (
    <span className={`rounded px-1.5 text-xs font-medium ${STATUS_CLASSES[status]}`}>
      {WORKFLOW_STATUS_LABELS[status]}
    </span>
  );
}

/** Features / user stories that have at least one task or bug in the sprint, with their derived status. */
function FeatureOverview({ features }: { features: BoardParentDto[] }) {
  if (features.length === 0) return null;
  return (
    <section aria-label="Přehled featur a user stories ve sprintu">
      <h2 className="mb-2 font-semibold text-gray-800">Featury a user stories ve sprintu</h2>
      <ul className="space-y-2">
        {features.map((feature) => (
          <li key={feature.itemId} className="rounded border border-gray-200 p-2">
            <div className="flex items-center gap-2">
              <span className="font-medium">{feature.title}</span>
              <StatusBadge status={feature.status} />
            </div>
            <ul className="mt-1 ml-4 space-y-1">
              {feature.children.map((story) => (
                <li key={story.itemId} className="flex items-center gap-2 text-sm text-gray-700">
                  <span>{story.title}</span>
                  <StatusBadge status={story.status} />
                </li>
              ))}
            </ul>
          </li>
        ))}
      </ul>
    </section>
  );
}

/** Tasks / bugs of the project that can be put into this sprint (an item belongs to exactly one sprint). */
function BacklogPanel({ projectId, sprintId }: { projectId: number; sprintId: number }) {
  const { data, isLoading } = useBacklog(projectId);
  const assign = useAssignSprintItem(projectId);
  const candidates = (data?.items ?? []).filter((i) => i.sprintId !== sprintId);

  return (
    <section aria-label="Přidat do sprintu">
      <h2 className="mb-2 font-semibold text-gray-800">Přidat do sprintu</h2>
      {errorMessage(assign.error) && (
        <p role="alert" className="mb-2 text-sm text-red-600">
          {errorMessage(assign.error)}
        </p>
      )}
      {isLoading && <p className="text-sm text-gray-500">Načítání…</p>}
      {!isLoading && candidates.length === 0 && (
        <p className="text-sm text-gray-500">Žádné další tasky ani bugy k přidání.</p>
      )}
      <ul className="space-y-1">
        {candidates.map((item) => (
          <li key={item.id} className="flex items-center gap-2 rounded border border-gray-200 px-2 py-1 text-sm">
            <span className="rounded bg-gray-100 px-1.5 text-xs text-gray-600">
              {ITEM_TYPE_LABELS[item.type] ?? "?"}
            </span>
            <span className="font-medium">{item.title}</span>
            {item.userStoryTitle && <span className="text-xs text-gray-500">({item.userStoryTitle})</span>}
            {item.sprintTitle && (
              <span className="text-xs text-amber-600">je ve sprintu „{item.sprintTitle}“ – přidáním se přesune</span>
            )}
            <button
              type="button"
              disabled={assign.isPending}
              onClick={() => assign.mutate({ sprintId, itemId: item.id })}
              className="ml-auto rounded border border-gray-300 px-2 py-0.5 text-xs hover:bg-gray-50 disabled:opacity-40"
            >
              Přidat
            </button>
          </li>
        ))}
      </ul>
    </section>
  );
}
