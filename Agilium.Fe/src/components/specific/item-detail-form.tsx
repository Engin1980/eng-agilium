import * as React from "react";
import { AttributeInput } from "./attribute-input";
import { CommentsField } from "./comments-field";
import { TemplateItemType } from "../../services/templates-api";
import { useProjectPermissions } from "../../services/projects-queries";
import { TemplateLayout } from "./template-layout";
import { useItemFields, useSetItemFields } from "../../services/item-fields-queries";
import { ApiError } from "../../services/http-client";

/** Renders the item's template-driven fields (tables → columns → sections → attributes) and saves edited values in one PUT. */
export function ItemDetailForm({ itemId, projectId }: { itemId: number; projectId: number }) {
  const { canEditItems } = useProjectPermissions(projectId);
  const { data, isLoading, error } = useItemFields(itemId);
  const setFields = useSetItemFields(itemId);

  const [values, setValues] = React.useState<Record<number, string | null>>({});
  const [saveError, setSaveError] = React.useState<string | null>(null);
  const [savedAt, setSavedAt] = React.useState<number | null>(null);

  React.useEffect(() => {
    if (data) {
      setValues(
        Object.fromEntries(
          data.tables.flatMap((table) =>
            table.columns.flatMap((column) =>
              column.sections.flatMap((section) => section.items.filter((f) => f.type !== TemplateItemType.Comments).map((f) => [f.templateItemId, f.value])),
            ),
          ),
        ),
      );
    }
  }, [data]);

  if (isLoading) return <p className="text-gray-500">Načítání polí…</p>;
  if (error)
    return (
      <p className="text-red-600">
        {error instanceof ApiError ? error.message : "Nepodařilo se načíst pole položky."}
      </p>
    );
  if (!data) return null;

  async function handleSave() {
    setSaveError(null);
    setSavedAt(null);
    try {
      await setFields.mutateAsync(
        Object.entries(values).map(([templateItemId, value]) => ({
          templateItemId: Number(templateItemId),
          value,
        })),
      );
      setSavedAt(Date.now());
    } catch (err) {
      setSaveError(err instanceof ApiError ? err.message : "Uložení se nezdařilo.");
    }
  }

  return (
    <div>
      <TemplateLayout
        tables={data.tables}
        getAttributeKey={(field) => field.templateItemId}
        renderAttribute={(field) =>
          field.type === TemplateItemType.Comments ? (
            // comments are a list saved immediately through their own endpoints, not part of the form's PUT
            <CommentsField
              itemId={itemId}
              templateItemId={field.templateItemId}
              title={field.title}
              canWrite={canEditItems}
            />
          ) : (
            <AttributeInput
              title={field.title}
              type={field.type}
              value={values[field.templateItemId] ?? null}
              onChange={(v) => setValues((prev) => ({ ...prev, [field.templateItemId]: v }))}
            />
          )
        }
      />

      <div className="mt-4 flex items-center gap-3">
        <button
          onClick={handleSave}
          disabled={setFields.isPending}
          className="rounded bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700"
        >
          {setFields.isPending ? "Ukládám…" : "Uložit"}
        </button>
        {saveError && <span className="text-sm text-red-600">{saveError}</span>}
        {savedAt && !saveError && <span className="text-sm text-emerald-600">Uloženo.</span>}
      </div>
    </div>
  );
}
