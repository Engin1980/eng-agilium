import * as React from "react";
import { TemplateItemType } from "../../services/templates-api";
import type { TemplateDto, TemplateFieldInput } from "../../services/templates-api";
import { ApiError } from "../../services/http-client";

const TYPE_LABELS: Record<number, string> = {
  [TemplateItemType.InlineText]: "Text (řádek)",
  [TemplateItemType.NextlineText]: "Text (víceřádkový)",
  [TemplateItemType.NextlineTextArea]: "Textová oblast",
  [TemplateItemType.InlineInt]: "Celé číslo",
  [TemplateItemType.NextlineInt]: "Celé číslo (víceřádkové)",
  [TemplateItemType.InlineDouble]: "Desetinné číslo",
  [TemplateItemType.NNextlineDouble]: "Desetinné číslo (víceřádkové)",
  [TemplateItemType.Comments]: "Komentáře",
  [TemplateItemType.Untemplated]: "Neurčeno",
  [TemplateItemType.Checkbox]: "Zaškrtávací pole",
  [TemplateItemType.LabelOnly]: "Jen popisek",
};

let nextTempId = -1;

function toInput(field: TemplateDto["items"][number]): TemplateFieldInput {
  return { ...field };
}

export function TemplateFieldsEditor({
  template,
  onSave,
  isSaving,
  saveError,
}: {
  template: TemplateDto;
  onSave: (columnCount: number, items: TemplateFieldInput[]) => void;
  isSaving: boolean;
  saveError: unknown;
}) {
  const [columnCount, setColumnCount] = React.useState(template.columnCount);
  const [fields, setFields] = React.useState<TemplateFieldInput[]>(() => template.items.map(toInput));

  React.useEffect(() => {
    setColumnCount(template.columnCount);
    setFields(template.items.map(toInput));
  }, [template]);

  function updateField(index: number, patch: Partial<TemplateFieldInput>) {
    setFields((prev) => prev.map((f, i) => (i === index ? { ...f, ...patch } : f)));
  }

  function removeField(index: number) {
    setFields((prev) => prev.filter((_, i) => i !== index));
  }

  function addField() {
    setFields((prev) => [
      ...prev,
      {
        id: nextTempId--,
        key: "",
        title: "",
        type: TemplateItemType.InlineText,
        validatingRegex: null,
        orderIndex: prev.length + 1,
        columnStart: 1,
        columnSpan: 1,
        rowStart: prev.length + 1,
        rowSpan: 1,
      },
    ]);
  }

  function handleSave() {
    // Fields added in this session have a synthetic negative id - the backend treats a missing id
    // (null) as "create new".
    onSave(
      columnCount,
      fields.map((f) => ({ ...f, id: f.id !== null && f.id < 0 ? null : f.id })),
    );
  }

  return (
    <div>
      <div className="mb-4 flex items-center gap-2">
        <label className="text-sm font-medium text-gray-700">Počet sloupců gridu</label>
        <input
          type="number"
          min={1}
          value={columnCount}
          onChange={(e) => setColumnCount(Number(e.target.value))}
          className="w-20 rounded border border-gray-300 px-2 py-1 text-sm"
        />
      </div>

      <div className="overflow-x-auto">
        <table className="w-full min-w-[900px] border-collapse text-sm">
          <thead>
            <tr className="border-b border-gray-200 text-left text-gray-500">
              <th className="py-1 pr-2">Klíč</th>
              <th className="py-1 pr-2">Název</th>
              <th className="py-1 pr-2">Typ</th>
              <th className="py-1 pr-2">Regex</th>
              <th className="py-1 pr-2">Pořadí</th>
              <th className="py-1 pr-2">Sloupec od</th>
              <th className="py-1 pr-2">Šířka</th>
              <th className="py-1 pr-2">Řádek od</th>
              <th className="py-1 pr-2">Výška</th>
              <th className="py-1"></th>
            </tr>
          </thead>
          <tbody>
            {fields.map((field, index) => (
              <tr key={field.id ?? index} className="border-b border-gray-100">
                <td className="py-1 pr-2">
                  <input
                    className="w-28 rounded border border-gray-300 px-1 py-0.5"
                    value={field.key}
                    onChange={(e) => updateField(index, { key: e.target.value })}
                  />
                </td>
                <td className="py-1 pr-2">
                  <input
                    className="w-32 rounded border border-gray-300 px-1 py-0.5"
                    value={field.title}
                    onChange={(e) => updateField(index, { title: e.target.value })}
                  />
                </td>
                <td className="py-1 pr-2">
                  <select
                    className="rounded border border-gray-300 px-1 py-0.5"
                    value={field.type}
                    onChange={(e) => updateField(index, { type: Number(e.target.value) as never })}
                  >
                    {Object.entries(TYPE_LABELS).map(([value, label]) => (
                      <option key={value} value={value}>
                        {label}
                      </option>
                    ))}
                  </select>
                </td>
                <td className="py-1 pr-2">
                  <input
                    className="w-24 rounded border border-gray-300 px-1 py-0.5"
                    value={field.validatingRegex ?? ""}
                    onChange={(e) => updateField(index, { validatingRegex: e.target.value || null })}
                  />
                </td>
                <td className="py-1 pr-2">
                  <input
                    type="number"
                    className="w-14 rounded border border-gray-300 px-1 py-0.5"
                    value={field.orderIndex}
                    onChange={(e) => updateField(index, { orderIndex: Number(e.target.value) })}
                  />
                </td>
                <td className="py-1 pr-2">
                  <input
                    type="number"
                    min={1}
                    className="w-14 rounded border border-gray-300 px-1 py-0.5"
                    value={field.columnStart}
                    onChange={(e) => updateField(index, { columnStart: Number(e.target.value) })}
                  />
                </td>
                <td className="py-1 pr-2">
                  <input
                    type="number"
                    min={1}
                    className="w-14 rounded border border-gray-300 px-1 py-0.5"
                    value={field.columnSpan}
                    onChange={(e) => updateField(index, { columnSpan: Number(e.target.value) })}
                  />
                </td>
                <td className="py-1 pr-2">
                  <input
                    type="number"
                    min={1}
                    className="w-14 rounded border border-gray-300 px-1 py-0.5"
                    value={field.rowStart}
                    onChange={(e) => updateField(index, { rowStart: Number(e.target.value) })}
                  />
                </td>
                <td className="py-1 pr-2">
                  <input
                    type="number"
                    min={1}
                    className="w-14 rounded border border-gray-300 px-1 py-0.5"
                    value={field.rowSpan}
                    onChange={(e) => updateField(index, { rowSpan: Number(e.target.value) })}
                  />
                </td>
                <td className="py-1">
                  <button className="text-red-600 hover:underline" onClick={() => removeField(index)}>
                    Smazat
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <button onClick={addField} className="mt-3 text-sm text-emerald-700 hover:underline">
        + Přidat pole
      </button>

      <div className="mt-4">
        <h3 className="mb-2 text-sm font-medium text-gray-700">Náhled rozložení</h3>
        <div
          className="grid gap-2 rounded border border-dashed border-gray-300 p-3"
          style={{ gridTemplateColumns: `repeat(${columnCount}, minmax(0, 1fr))` }}
        >
          {fields.map((field, index) => (
            <div
              key={field.id ?? index}
              style={{
                gridColumn: `${field.columnStart} / span ${field.columnSpan}`,
                gridRow: `${field.rowStart} / span ${field.rowSpan}`,
              }}
              className="rounded bg-blue-50 px-2 py-1 text-xs text-blue-800"
            >
              {field.title || field.key || "(bez názvu)"}
            </div>
          ))}
        </div>
      </div>

      <div className="mt-4 flex items-center gap-3">
        <button
          onClick={handleSave}
          disabled={isSaving}
          className="rounded bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700"
        >
          {isSaving ? "Ukládám…" : "Uložit šablonu"}
        </button>
        {saveError != null && (
          <span className="text-sm text-red-600">
            {saveError instanceof ApiError ? saveError.message : "Uložení se nezdařilo."}
          </span>
        )}
      </div>
    </div>
  );
}
