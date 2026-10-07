import * as React from "react";
import { useBlocker } from "@tanstack/react-router";
import { ConfirmDialog } from "../global/dialogs/ConfirmDialog";
import { ApiError } from "../../services/http-client";
import { WORKFLOW_STATUS_LABELS, WorkflowStateType } from "../../services/sprints-api";
import type { WorkflowStateDto, WorkflowStateInput } from "../../services/workflow-api";
import { useUpdateWorkflow, useWorkflow } from "../../services/workflow-queries";

type DraftState = { key: string; id: number | null; title: string; type: WorkflowStateType; itemCount: number };

const MIN_STATES = 2;

const toDraft = (states: WorkflowStateDto[]): DraftState[] =>
  states.map((s) => ({ key: `s${s.id}`, id: s.id, title: s.title, type: s.type, itemCount: s.itemCount }));

const toInput = (draft: DraftState[]): WorkflowStateInput[] =>
  draft.map((s) => ({ id: s.id, title: s.title.trim(), type: s.type }));

/** Same rules as the backend: >= 2 columns, at least one ToDo and one Done, non-empty titles. */
function validateWorkflow(draft: DraftState[]): string[] {
  const errors: string[] = [];
  if (draft.length < MIN_STATES) errors.push("Workflow musí mít alespoň 2 sloupce.");
  if (!draft.some((s) => s.type === WorkflowStateType.ToDo)) errors.push("Chybí sloupec se stavem ToDo.");
  if (!draft.some((s) => s.type === WorkflowStateType.Done)) errors.push("Chybí sloupec se stavem Done.");
  if (draft.some((s) => !s.title.trim())) errors.push("Každý sloupec musí mít název.");
  return errors;
}

/** Editor of the project's workflow (kanban columns): count, titles, types and order. */
export function WorkflowEditor({ projectId }: { projectId: number }) {
  const { data, isLoading, error } = useWorkflow(projectId);

  if (isLoading) return <p className="text-gray-500">Načítání…</p>;
  if (error || !data) return <p className="text-red-600">Nepodařilo se načíst workflow.</p>;

  // Remount (= fresh draft) whenever the server-side workflow changes, e.g. after a save.
  return <LoadedEditor key={JSON.stringify(data)} projectId={projectId} states={data.states} />;
}

function LoadedEditor({ projectId, states }: { projectId: number; states: WorkflowStateDto[] }) {
  const update = useUpdateWorkflow(projectId);
  const original = React.useMemo(() => toDraft(states), [states]);
  const [draft, setDraft] = React.useState<DraftState[]>(original);
  const [confirmSave, setConfirmSave] = React.useState(false);
  const [notice, setNotice] = React.useState<string | null>(null);
  const nextKey = React.useRef(0);

  const isDirty = JSON.stringify(toInput(draft)) !== JSON.stringify(toInput(original));
  const errors = validateWorkflow(draft);

  const blocker = useBlocker({
    shouldBlockFn: () => isDirty,
    enableBeforeUnload: () => isDirty,
    withResolver: true,
  });

  const keptIds = new Set(draft.map((s) => s.id));
  const removed = original.filter((s) => !keptIds.has(s.id));
  const itemsToMove = removed.reduce((sum, s) => sum + s.itemCount, 0);

  function patch(key: string, changes: Partial<DraftState>) {
    setDraft((d) => d.map((s) => (s.key === key ? { ...s, ...changes } : s)));
  }

  function move(index: number, dir: -1 | 1) {
    setDraft((d) => {
      const target = index + dir;
      if (target < 0 || target >= d.length) return d;
      const copy = [...d];
      [copy[index], copy[target]] = [copy[target], copy[index]];
      return copy;
    });
  }

  function save() {
    setConfirmSave(false);
    setNotice(null);
    update.mutate(toInput(draft), {
      onSuccess: (result) =>
        setNotice(
          result.movedItems > 0
            ? `Workflow uloženo. Přesunuté položky: ${result.movedItems}.`
            : "Workflow uloženo.",
        ),
    });
  }

  return (
    <div className="space-y-4">
      <ol className="space-y-2">
        {draft.map((state, index) => (
          <li key={state.key} className="flex flex-wrap items-center gap-2 rounded border border-gray-200 p-2">
            <span className="w-6 text-center text-sm text-gray-500">{index + 1}.</span>
            <input
              aria-label={`Název sloupce ${index + 1}`}
              value={state.title}
              maxLength={256}
              onChange={(e) => patch(state.key, { title: e.target.value })}
              className={`min-w-0 flex-1 rounded border px-2 py-1 ${
                state.title.trim() ? "border-gray-300" : "border-red-400"
              }`}
            />
            <select
              aria-label={`Stav sloupce ${index + 1}`}
              value={state.type}
              onChange={(e) => patch(state.key, { type: Number(e.target.value) as WorkflowStateType })}
              className="rounded border border-gray-300 px-2 py-1 text-sm"
            >
              {Object.values(WorkflowStateType).map((type) => (
                <option key={type} value={type}>
                  {WORKFLOW_STATUS_LABELS[type]}
                </option>
              ))}
            </select>
            {state.id !== null && <span className="text-xs text-gray-500">položek: {state.itemCount}</span>}
            <button
              type="button"
              aria-label={`Posunout sloupec ${index + 1} doleva`}
              disabled={index === 0}
              onClick={() => move(index, -1)}
              className="rounded border border-gray-300 px-2 py-1 text-sm disabled:opacity-30"
            >
              ←
            </button>
            <button
              type="button"
              aria-label={`Posunout sloupec ${index + 1} doprava`}
              disabled={index === draft.length - 1}
              onClick={() => move(index, 1)}
              className="rounded border border-gray-300 px-2 py-1 text-sm disabled:opacity-30"
            >
              →
            </button>
            <button
              type="button"
              aria-label={`Odebrat sloupec ${index + 1}`}
              disabled={draft.length <= MIN_STATES}
              onClick={() => setDraft((d) => d.filter((s) => s.key !== state.key))}
              className="rounded border border-red-300 px-2 py-1 text-sm text-red-600 disabled:opacity-30"
            >
              ✕
            </button>
          </li>
        ))}
      </ol>

      <button
        type="button"
        onClick={() =>
          setDraft((d) => [
            ...d,
            { key: `n${nextKey.current++}`, id: null, title: "", type: WorkflowStateType.Active, itemCount: 0 },
          ])
        }
        className="rounded border border-dashed border-gray-400 px-3 py-1 text-sm text-gray-700 hover:bg-gray-50"
      >
        + Sloupec
      </button>

      {isDirty && errors.length > 0 && (
        <ul role="alert" className="list-disc pl-5 text-sm text-red-600">
          {errors.map((e) => (
            <li key={e}>{e}</li>
          ))}
        </ul>
      )}
      {update.error && (
        <p role="alert" className="text-sm text-red-600">
          {update.error instanceof ApiError ? update.error.message : "Uložení se nezdařilo."}
        </p>
      )}
      {notice && !isDirty && <p className="text-sm text-emerald-600">{notice}</p>}

      <div className="flex items-center gap-3">
        {isDirty && <span className="text-sm text-amber-600">Neuložené změny</span>}
        <span className="mr-auto" />
        <button
          type="button"
          disabled={!isDirty || update.isPending}
          onClick={() => setDraft(original)}
          className="rounded border border-gray-300 px-3 py-2 text-sm hover:bg-gray-50 disabled:opacity-40"
        >
          Zahodit změny
        </button>
        <button
          type="button"
          disabled={!isDirty || errors.length > 0 || update.isPending}
          onClick={() => (removed.length > 0 ? setConfirmSave(true) : save())}
          className="rounded bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-40"
        >
          {update.isPending ? "Ukládám…" : "Uložit"}
        </button>
      </div>

      <ConfirmDialog
        open={confirmSave}
        title="Zrušit sloupce?"
        message={`Rušíte ${removed.length} sloupců (${removed.map((s) => s.title).join(", ")}). Položky ve sprintech (${itemsToMove}) se přesunou do nejbližšího levého sloupce.`}
        confirmLabel="Uložit"
        onCancel={() => setConfirmSave(false)}
        onConfirm={save}
      />
      <ConfirmDialog
        open={blocker.status === "blocked"}
        title="Zahodit neuložené změny?"
        message="Workflow má neuložené změny. Pokud stránku opustíte, změny se ztratí."
        confirmLabel="Zahodit a odejít"
        cancelLabel="Zůstat"
        onCancel={() => blocker.reset?.()}
        onConfirm={() => blocker.proceed?.()}
      />
    </div>
  );
}
