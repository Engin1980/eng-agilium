import * as React from "react";
import { useTemplateEditor, type MoveTarget } from "./template-editor-context";

export function IconButton({
  label,
  onClick,
  disabled,
  danger,
  children,
}: {
  label: string;
  onClick: () => void;
  disabled?: boolean;
  danger?: boolean;
  children: React.ReactNode;
}) {
  return (
    <button
      type="button"
      aria-label={label}
      title={label}
      onClick={onClick}
      disabled={disabled}
      className={`rounded border border-gray-300 px-2 py-1 text-xs hover:bg-gray-50 disabled:cursor-not-allowed disabled:opacity-40 ${
        danger ? "text-red-600" : "text-gray-700"
      }`}
    >
      {children}
    </button>
  );
}

export function AddButton({ onClick, children }: { onClick: () => void; children: React.ReactNode }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className="rounded border border-dashed border-gray-400 px-2 py-1 text-xs text-gray-600 hover:bg-gray-50"
    >
      + {children}
    </button>
  );
}

/** Errors (red) and warnings (amber) attached to the element with the given cid. */
export function Issues({ cid }: { cid: string }) {
  const { errors, warnings } = useTemplateEditor();
  return (
    <>
      {errors.get(cid)?.map((issue, i) => (
        <p key={`e${i}`} role="alert" className="text-xs text-red-600">
          {issue.message}
        </p>
      ))}
      {warnings.get(cid)?.map((issue, i) => (
        <p key={`w${i}`} className="text-xs text-amber-600">
          {issue.message}
        </p>
      ))}
    </>
  );
}

/** "Move to…" select; picking a target calls `onPick` and the select goes back to its placeholder. */
export function MoveToSelect({
  label,
  targets,
  currentCid,
  onPick,
}: {
  label: string;
  targets: MoveTarget[];
  currentCid: string | undefined;
  onPick: (cid: string) => void;
}) {
  const options = targets.filter((t) => t.cid !== currentCid);
  if (options.length === 0) return null;
  return (
    <select
      aria-label={label}
      value=""
      onChange={(e) => e.target.value && onPick(e.target.value)}
      className="rounded border border-gray-300 px-1 py-1 text-xs text-gray-700"
    >
      <option value="">{label}</option>
      {options.map((t) => (
        <option key={t.cid} value={t.cid}>
          {t.label}
        </option>
      ))}
    </select>
  );
}
