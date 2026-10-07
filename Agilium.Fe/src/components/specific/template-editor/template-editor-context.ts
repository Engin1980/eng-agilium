import * as React from "react";
import type { DraftAction, Issue } from "./template-draft";

export type MoveTarget = { cid: string; label: string };

export type TemplateEditorContextValue = {
  dispatch: React.Dispatch<DraftAction>;
  /** Blocking validation errors and non-blocking warnings, keyed by the element cid. */
  errors: Map<string, Issue[]>;
  warnings: Map<string, Issue[]>;
  columnTargets: MoveTarget[];
  sectionTargets: MoveTarget[];
  /** Runs `action` right away, or after the user confirms `message` in a dialog. */
  confirm: (message: string | null, action: () => void) => void;
};

export const TemplateEditorContext = React.createContext<TemplateEditorContextValue | null>(null);

export function useTemplateEditor(): TemplateEditorContextValue {
  const value = React.useContext(TemplateEditorContext);
  if (!value) throw new Error("useTemplateEditor must be used inside <TemplateEditor>");
  return value;
}
