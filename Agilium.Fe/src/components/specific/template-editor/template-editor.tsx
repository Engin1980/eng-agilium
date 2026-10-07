import * as React from "react";
import { useBlocker } from "@tanstack/react-router";
import { ConfirmDialog } from "../../global/dialogs/ConfirmDialog";
import { ApiError } from "../../../services/http-client";
import type { ItemType } from "../../../services/items-api";
import type { TemplateDto, UpdateTemplateResult } from "../../../services/templates-api";
import { useProjectTemplate, useUpdateProjectTemplate } from "../../../services/templates-queries";
import { AttributeInput } from "../attribute-input";
import { TemplateLayout } from "../template-layout";
import { AddButton } from "./editor-controls";
import { TableEditor } from "./table-editor";
import {
  countRemovedSavedAttributes,
  draftFromDto,
  draftReducer,
  draftToInput,
  draftToLayout,
  validateDraft,
  type Draft,
  type Issue,
} from "./template-draft";
import { TemplateEditorContext, type MoveTarget, type TemplateEditorContextValue } from "./template-editor-context";

/**
 * Editor of one project template (one item type): loads it, lets the user edit a local draft of the
 * whole tree and saves it with a single PUT. Globally defined templates are never edited here.
 */
export function TemplateEditor({ projectId, itemType }: { projectId: number; itemType: ItemType }) {
  const { data, isLoading, error } = useProjectTemplate(projectId, itemType);
  const update = useUpdateProjectTemplate(projectId, itemType);
  const [notice, setNotice] = React.useState<string | null>(null);

  if (isLoading) return <p className="text-gray-500">Načítání šablony…</p>;
  if (error || !data) return <p className="text-red-600">Nepodařilo se načíst šablonu.</p>;

  function handleSave(draft: Draft) {
    setNotice(null);
    update.mutate(draftToInput(draft), {
      onSuccess: (result: UpdateTemplateResult) =>
        setNotice(
          result.deletedFieldValues > 0
            ? `Šablona uložena. Smazáno hodnot polí: ${result.deletedFieldValues}.`
            : "Šablona uložena.",
        ),
    });
  }

  return (
    // Remount (= fresh draft) whenever the server-side template changes, e.g. after a save.
    <LoadedEditor
      key={JSON.stringify(data)}
      data={data}
      isSaving={update.isPending}
      saveError={update.error}
      notice={notice}
      onSave={handleSave}
    />
  );
}

type PendingConfirm = { title: string; message: string; confirmLabel: string; action: () => void };

function LoadedEditor({
  data,
  isSaving,
  saveError,
  notice,
  onSave,
}: {
  data: TemplateDto;
  isSaving: boolean;
  saveError: Error | null;
  notice: string | null;
  onSave: (draft: Draft) => void;
}) {
  const original = React.useMemo(() => draftFromDto(data), [data]);
  const [draft, dispatch] = React.useReducer(draftReducer, original);
  const [mode, setMode] = React.useState<"edit" | "preview">("edit");
  const [pending, setPending] = React.useState<PendingConfirm | null>(null);

  const isDirty = React.useMemo(
    () => JSON.stringify(draftToInput(draft)) !== JSON.stringify(draftToInput(original)),
    [draft, original],
  );

  const blocker = useBlocker({
    shouldBlockFn: () => isDirty,
    enableBeforeUnload: () => isDirty,
    withResolver: true,
  });

  const validation = React.useMemo(() => validateDraft(draft), [draft]);
  const errors = React.useMemo(() => groupByCid(validation.errors), [validation]);
  const warnings = React.useMemo(() => groupByCid(validation.warnings), [validation]);

  const { columnTargets, sectionTargets } = React.useMemo(() => {
    const columns: MoveTarget[] = [];
    const sections: MoveTarget[] = [];
    draft.forEach((table, ti) =>
      table.columns.forEach((column, ci) => {
        const columnLabel = `Tabulka ${ti + 1} / Sloupec ${ci + 1}`;
        columns.push({ cid: column.cid, label: columnLabel });
        column.sections.forEach((section, si) =>
          sections.push({ cid: section.cid, label: `${columnLabel} / ${section.title || `Sekce ${si + 1}`}` }),
        );
      }),
    );
    return { columnTargets: columns, sectionTargets: sections };
  }, [draft]);

  const context = React.useMemo<TemplateEditorContextValue>(
    () => ({
      dispatch,
      errors,
      warnings,
      columnTargets,
      sectionTargets,
      confirm: (message, action) =>
        message === null
          ? action()
          : setPending({ title: "Odebrat?", message, confirmLabel: "Odebrat", action }),
    }),
    [errors, warnings, columnTargets, sectionTargets],
  );

  function requestSave() {
    const removed = countRemovedSavedAttributes(original, draft);
    if (removed === 0) {
      onSave(draft);
      return;
    }
    setPending({
      title: "Uložit změny šablony?",
      message: `Odebíráte ${removed} uložených atributů. Jejich hodnoty se smažou u všech položek tohoto typu.`,
      confirmLabel: "Uložit a smazat hodnoty",
      action: () => onSave(draft),
    });
  }

  const hasErrors = validation.errors.length > 0;

  return (
    <TemplateEditorContext.Provider value={context}>
      <div className="mb-4 flex flex-wrap items-center gap-3">
        <div className="flex overflow-hidden rounded border border-gray-300 text-sm">
          {(["edit", "preview"] as const).map((m) => (
            <button
              key={m}
              type="button"
              onClick={() => setMode(m)}
              className={`px-3 py-1 ${mode === m ? "bg-blue-600 text-white" : "bg-white text-gray-700 hover:bg-gray-50"}`}
            >
              {m === "edit" ? "Editace" : "Náhled"}
            </button>
          ))}
        </div>

        <span className="mr-auto" />
        {isDirty && <span className="text-sm text-amber-600">Neuložené změny</span>}
        <button
          type="button"
          onClick={() => dispatch({ type: "reset", draft: draftFromDto(data) })}
          disabled={!isDirty || isSaving}
          className="rounded border border-gray-300 px-3 py-2 text-sm hover:bg-gray-50 disabled:opacity-40"
        >
          Zahodit změny
        </button>
        <button
          type="button"
          onClick={requestSave}
          disabled={!isDirty || hasErrors || isSaving}
          className="rounded bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700 disabled:opacity-40"
        >
          {isSaving ? "Ukládám…" : "Uložit"}
        </button>
      </div>

      {hasErrors && isDirty && (
        <p role="alert" className="mb-3 text-sm text-red-600">
          Šablonu nelze uložit, dokud nejsou opraveny chyby označené níže.
        </p>
      )}
      {saveError && (
        <p role="alert" className="mb-3 text-sm text-red-600">
          {saveError instanceof ApiError ? saveError.message : "Uložení šablony se nezdařilo."}
        </p>
      )}
      {notice && !isDirty && <p className="mb-3 text-sm text-emerald-600">{notice}</p>}

      {mode === "edit" ? (
        <div className="space-y-4">
          {draft.map((table, i) => (
            <TableEditor key={table.cid} table={table} index={i} isFirst={i === 0} isLast={i === draft.length - 1} />
          ))}
          {draft.length === 0 && <p className="text-sm text-gray-500">Šablona nemá žádnou tabulku.</p>}
          <AddButton onClick={() => dispatch({ type: "addTable" })}>Tabulka</AddButton>
        </div>
      ) : (
        <div className="rounded border border-gray-200 p-4">
          <TemplateLayout
            tables={draftToLayout(draft)}
            getAttributeKey={(attribute) => attribute.cid}
            renderAttribute={(attribute) => (
              <AttributeInput
                title={attribute.title || "(bez titulku)"}
                type={attribute.type}
                value={null}
                onChange={() => {}}
                disabled
              />
            )}
          />
        </div>
      )}

      <ConfirmDialog
        open={pending !== null}
        title={pending?.title ?? ""}
        message={pending?.message ?? ""}
        confirmLabel={pending?.confirmLabel}
        onCancel={() => setPending(null)}
        onConfirm={() => {
          pending?.action();
          setPending(null);
        }}
      />

      <ConfirmDialog
        open={blocker.status === "blocked"}
        title="Zahodit neuložené změny?"
        message="Šablona má neuložené změny. Pokud stránku opustíte, změny se ztratí."
        confirmLabel="Zahodit a odejít"
        cancelLabel="Zůstat"
        onCancel={() => blocker.reset?.()}
        onConfirm={() => blocker.proceed?.()}
      />
    </TemplateEditorContext.Provider>
  );
}

function groupByCid(issues: Issue[]): Map<string, Issue[]> {
  const map = new Map<string, Issue[]>();
  for (const issue of issues) map.set(issue.cid, [...(map.get(issue.cid) ?? []), issue]);
  return map;
}
