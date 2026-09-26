import * as React from "react";
import { TemplateItemType } from "../../services/templates-api";
import type { ItemFieldDto } from "../../services/item-fields-api";
import { useItemFields, useSetItemFields } from "../../services/item-fields-queries";
import { ApiError } from "../../services/http-client";

/** Renders the item's template-driven fields on a CSS grid and saves edited values in one PUT. */
export function ItemDetailForm({ itemId }: { itemId: number }) {
  const { data, isLoading, error } = useItemFields(itemId);
  const setFields = useSetItemFields(itemId);

  const [values, setValues] = React.useState<Record<number, string | null>>({});
  const [saveError, setSaveError] = React.useState<string | null>(null);
  const [savedAt, setSavedAt] = React.useState<number | null>(null);

  React.useEffect(() => {
    if (data) {
      setValues(Object.fromEntries(data.fields.map((f) => [f.templateItemId, f.value])));
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
      <div
        className="grid gap-4"
        style={{ gridTemplateColumns: `repeat(${data.columnCount}, minmax(0, 1fr))` }}
      >
        {data.fields.map((field) => (
          <FieldInput
            key={field.templateItemId}
            field={field}
            value={values[field.templateItemId] ?? null}
            onChange={(v) => setValues((prev) => ({ ...prev, [field.templateItemId]: v }))}
          />
        ))}
      </div>

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

function FieldInput({
  field,
  value,
  onChange,
}: {
  field: ItemFieldDto;
  value: string | null;
  onChange: (value: string | null) => void;
}) {
  const style: React.CSSProperties = {
    gridColumn: `${field.columnStart} / span ${field.columnSpan}`,
    gridRow: `${field.rowStart} / span ${field.rowSpan}`,
  };

  if (field.type === TemplateItemType.LabelOnly) {
    return (
      <div style={style} className="flex items-end pb-2 font-medium text-gray-700">
        {field.title}
      </div>
    );
  }

  return (
    <div style={style}>
      <label className="mb-1 block text-sm font-medium text-gray-700">{field.title}</label>
      {renderControl(field, value, onChange)}
    </div>
  );
}

function renderControl(field: ItemFieldDto, value: string | null, onChange: (value: string | null) => void) {
  const inputClassName =
    "w-full rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500";

  switch (field.type) {
    case TemplateItemType.NextlineTextArea:
    case TemplateItemType.Comments:
    case TemplateItemType.Untemplated:
      return (
        <textarea
          className={inputClassName}
          rows={field.type === TemplateItemType.NextlineTextArea ? 4 : 3}
          value={value ?? ""}
          onChange={(e) => onChange(e.target.value === "" ? null : e.target.value)}
        />
      );
    case TemplateItemType.InlineInt:
    case TemplateItemType.NextlineInt:
      return (
        <input
          type="number"
          step={1}
          className={inputClassName}
          value={value ?? ""}
          onChange={(e) => onChange(e.target.value === "" ? null : e.target.value)}
        />
      );
    case TemplateItemType.InlineDouble:
    case TemplateItemType.NNextlineDouble:
      // Plain text (not type="number") on purpose: the backend accepts a comma decimal separator too.
      return (
        <input
          type="text"
          inputMode="decimal"
          className={inputClassName}
          value={value ?? ""}
          onChange={(e) => onChange(e.target.value === "" ? null : e.target.value)}
        />
      );
    case TemplateItemType.Checkbox:
      return (
        <input
          type="checkbox"
          className="h-5 w-5"
          checked={value === "true"}
          onChange={(e) => onChange(e.target.checked ? "true" : "false")}
        />
      );
    default:
      return (
        <input
          type="text"
          className={inputClassName}
          value={value ?? ""}
          onChange={(e) => onChange(e.target.value === "" ? null : e.target.value)}
        />
      );
  }
}
