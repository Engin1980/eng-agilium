import { TemplateItemType } from "../../services/templates-api";

const inputClassName =
  "w-full rounded border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500 disabled:bg-gray-50";

/** Title + the input matching the attribute type (`LabelOnly` is just the title as text). */
export function AttributeInput({
  title,
  type,
  value,
  onChange,
  disabled = false,
}: {
  title: string;
  type: TemplateItemType;
  value: string | null;
  onChange: (value: string | null) => void;
  disabled?: boolean;
}) {
  if (type === TemplateItemType.LabelOnly) {
    return <p className="text-sm text-gray-700">{title}</p>;
  }

  const set = (raw: string) => onChange(raw === "" ? null : raw);

  if (type === TemplateItemType.Boolean) {
    return (
      <label className="flex items-center gap-2 text-sm font-medium text-gray-700">
        <input
          type="checkbox"
          className="h-5 w-5"
          disabled={disabled}
          checked={value === "true"}
          onChange={(e) => onChange(e.target.checked ? "true" : "false")}
        />
        {title}
      </label>
    );
  }

  return (
    <div>
      <label className="mb-1 block text-sm font-medium text-gray-700">{title}</label>
      {renderControl(type, value, set, disabled)}
    </div>
  );
}

function renderControl(type: TemplateItemType, value: string | null, set: (raw: string) => void, disabled: boolean) {
  switch (type) {
    case TemplateItemType.MultiLineText:
    case TemplateItemType.Comments: // plain text for now, a dedicated comments component comes later
      return (
        <textarea
          className={inputClassName}
          rows={type === TemplateItemType.MultiLineText ? 4 : 3}
          disabled={disabled}
          value={value ?? ""}
          onChange={(e) => set(e.target.value)}
        />
      );
    case TemplateItemType.Integer:
      return (
        <input
          type="number"
          step={1}
          className={inputClassName}
          disabled={disabled}
          value={value ?? ""}
          onChange={(e) => set(e.target.value)}
        />
      );
    case TemplateItemType.Decimal:
      // Plain text (not type="number") on purpose: the backend accepts a comma decimal separator too.
      return (
        <input
          type="text"
          inputMode="decimal"
          className={inputClassName}
          disabled={disabled}
          value={value ?? ""}
          onChange={(e) => set(e.target.value)}
        />
      );
    default:
      return (
        <input
          type="text"
          className={inputClassName}
          disabled={disabled}
          value={value ?? ""}
          onChange={(e) => set(e.target.value)}
        />
      );
  }
}
