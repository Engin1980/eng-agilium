import {
  TemplateItemType,
  type TemplateDto,
  type TemplateTableInput,
} from "../../../services/templates-api";
import type { LayoutTable } from "../template-layout";

/**
 * Editable copy of a template (tables → columns → sections → attributes). Every element has a
 * client-side `cid` (stable React key / reducer address) and the server `id` if it already exists
 * (`null` = new). Pure logic only (no React) so it is easy to reason about and test.
 */
export type DraftAttribute = {
  cid: string;
  id: number | null;
  key: string;
  title: string;
  type: TemplateItemType;
  validatingRegex: string;
};
export type DraftSection = { cid: string; id: number | null; title: string; items: DraftAttribute[] };
export type DraftColumn = { cid: string; id: number | null; width: number; sections: DraftSection[] };
export type DraftTable = { cid: string; id: number | null; columns: DraftColumn[] };
export type Draft = DraftTable[];

export const EXPECTED_WIDTH_SUM = 12;

const newCid = () => crypto.randomUUID();

export function draftFromDto(dto: TemplateDto): Draft {
  return dto.tables.map((table) => ({
    cid: newCid(),
    id: table.id,
    columns: table.columns.map((column) => ({
      cid: newCid(),
      id: column.id,
      width: column.width,
      sections: column.sections.map((section) => ({
        cid: newCid(),
        id: section.id,
        title: section.title,
        items: section.items.map((item) => ({
          cid: newCid(),
          id: item.id,
          key: item.key,
          title: item.title,
          type: item.type,
          validatingRegex: item.validatingRegex ?? "",
        })),
      })),
    })),
  }));
}

/** The PUT payload; the array order is the element order, and it carries no client ids (so it is also a dirty-check key). */
export function draftToInput(draft: Draft): TemplateTableInput[] {
  return draft.map((table) => ({
    id: table.id,
    columns: table.columns.map((column) => ({
      id: column.id,
      width: column.width,
      sections: column.sections.map((section) => ({
        id: section.id,
        title: section.title,
        items: section.items.map((item) => ({
          id: item.id,
          key: item.key,
          title: item.title,
          type: item.type,
          validatingRegex: item.validatingRegex.trim() === "" ? null : item.validatingRegex,
        })),
      })),
    })),
  }));
}

/** The draft in the shape the shared layout renderer expects (client ids as keys, so new elements render too). */
export const draftToLayout = (draft: Draft): LayoutTable<DraftAttribute>[] =>
  draft.map((t) => ({
    id: t.cid,
    columns: t.columns.map((c) => ({
      id: c.cid,
      width: c.width,
      sections: c.sections.map((s) => ({ id: s.cid, title: s.title, items: s.items })),
    })),
  }));

export const allAttributes = (draft: Draft): DraftAttribute[] =>
  draft.flatMap((t) => t.columns.flatMap((c) => c.sections.flatMap((s) => s.items)));

/** Attributes the server already has (and which therefore own stored values). */
export const savedAttributeIds = (draft: Draft): Set<number> =>
  new Set(allAttributes(draft).flatMap((a) => (a.id === null ? [] : [a.id])));

/** Number of saved attributes that exist in `original` but not in `draft` - their values are lost on save. */
export function countRemovedSavedAttributes(original: Draft, draft: Draft): number {
  const kept = savedAttributeIds(draft);
  return [...savedAttributeIds(original)].filter((id) => !kept.has(id)).length;
}

export type NodeKind = "table" | "column" | "section";
export type AnyNode = DraftTable | DraftColumn | DraftSection;

export function attributesIn(node: AnyNode): DraftAttribute[] {
  if ("items" in node) return node.items;
  if ("sections" in node) return node.sections.flatMap((s) => s.items);
  return allAttributes([node]);
}

/** Confirmation text for removing a non-empty element or a saved attribute; `null` = remove without asking. */
export function removalMessage(what: string, node: AnyNode | DraftAttribute): string | null {
  const attributes = "type" in node ? [node] : attributesIn(node);
  const saved = attributes.filter((a) => a.id !== null).length;
  if (attributes.length === 0) return null;

  const base = "type" in node ? `Atribut „${node.title || node.key}“ bude odebrán.` : `${what} s ${attributes.length} atributy bude odebrána.`;
  return saved > 0
    ? `${base} Po uložení se smažou hodnoty ${saved} uložených atributů u všech položek tohoto typu.`
    : base;
}

// ---------- validation ----------

export type Issue = { cid: string; message: string };
export type Validation = { errors: Issue[]; warnings: Issue[] };

/** Blocking errors mirror the backend rules; a width sum different from 12 is only a warning. */
export function validateDraft(draft: Draft): Validation {
  const errors: Issue[] = [];
  const warnings: Issue[] = [];
  const keys = new Map<string, string>();

  for (const table of draft) {
    if (table.columns.length < 1) errors.push({ cid: table.cid, message: "Tabulka musí mít aspoň jeden sloupec." });

    const sum = table.columns.reduce((acc, c) => acc + (Number.isFinite(c.width) ? c.width : 0), 0);
    if (table.columns.length > 0 && sum !== EXPECTED_WIDTH_SUM)
      warnings.push({
        cid: table.cid,
        message: `Součet šířek sloupců je ${sum}, očekává se ${EXPECTED_WIDTH_SUM} (sloupce se vykreslí poměrově).`,
      });

    for (const column of table.columns) {
      if (!Number.isInteger(column.width) || column.width < 1)
        errors.push({ cid: column.cid, message: "Šířka sloupce musí být celé číslo, nejméně 1." });

      for (const section of column.sections) {
        for (const item of section.items) {
          if (item.title.trim() === "") errors.push({ cid: item.cid, message: "Titulek atributu nesmí být prázdný." });
          if (item.validatingRegex.trim() !== "") {
            try {
              new RegExp(item.validatingRegex);
            } catch {
              errors.push({ cid: item.cid, message: "Neplatný regulární výraz." });
            }
          }
          const normalizedKey = item.key.trim().toLowerCase();
          if (normalizedKey === "") {
            errors.push({ cid: item.cid, message: "Atribut nemá klíč (zadejte titulek)." });
          } else if (keys.has(normalizedKey)) {
            errors.push({ cid: item.cid, message: `Klíč '${item.key}' se opakuje.` });
          } else {
            keys.set(normalizedKey, item.cid);
          }
        }
      }
    }
  }

  return { errors, warnings };
}

// ---------- keys ----------

const stripDiacritics = (s: string) => s.normalize("NFD").replace(/[̀-ͯ]/g, "");

export function slugify(title: string): string {
  const slug = stripDiacritics(title)
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
  return slug === "" ? "attribute" : slug;
}

function uniqueKey(draft: Draft, base: string, ownCid: string): string {
  const taken = new Set(allAttributes(draft).filter((a) => a.cid !== ownCid).map((a) => a.key.toLowerCase()));
  if (!taken.has(base)) return base;
  let n = 2;
  while (taken.has(`${base}-${n}`)) n++;
  return `${base}-${n}`;
}

// ---------- reducer ----------

export type Direction = -1 | 1;

export type DraftAction =
  | { type: "reset"; draft: Draft }
  | { type: "addTable" }
  | { type: "removeTable"; cid: string }
  | { type: "moveTable"; cid: string; dir: Direction }
  | { type: "addColumn"; tableCid: string }
  | { type: "removeColumn"; cid: string }
  | { type: "moveColumn"; cid: string; dir: Direction }
  | { type: "setColumnWidth"; cid: string; width: number }
  | { type: "addSection"; columnCid: string }
  | { type: "removeSection"; cid: string }
  | { type: "moveSection"; cid: string; dir: Direction }
  | { type: "moveSectionTo"; cid: string; columnCid: string }
  | { type: "setSectionTitle"; cid: string; title: string }
  | { type: "addAttribute"; sectionCid: string }
  | { type: "removeAttribute"; cid: string }
  | { type: "moveAttribute"; cid: string; dir: Direction }
  | { type: "moveAttributeTo"; cid: string; sectionCid: string }
  | { type: "updateAttribute"; cid: string; patch: Partial<Pick<DraftAttribute, "title" | "type" | "validatingRegex">> };

/** Swaps the element with its neighbour; does nothing at the edge of the list. */
function shift<T>(list: T[], index: number, dir: Direction) {
  const target = index + dir;
  if (index < 0 || target < 0 || target >= list.length) return;
  [list[index], list[target]] = [list[target], list[index]];
}

export function draftReducer(state: Draft, action: DraftAction): Draft {
  if (action.type === "reset") return action.draft;

  const next = structuredClone(state);
  const columns = () => next.flatMap((t) => t.columns);
  const sections = () => columns().flatMap((c) => c.sections);

  switch (action.type) {
    case "addTable":
      next.push({
        cid: newCid(),
        id: null,
        columns: [{ cid: newCid(), id: null, width: EXPECTED_WIDTH_SUM, sections: [] }],
      });
      break;
    case "removeTable":
      return next.filter((t) => t.cid !== action.cid);
    case "moveTable":
      shift(next, next.findIndex((t) => t.cid === action.cid), action.dir);
      break;

    case "addColumn": {
      const table = next.find((t) => t.cid === action.tableCid);
      table?.columns.push({ cid: newCid(), id: null, width: 1, sections: [] });
      break;
    }
    case "removeColumn":
      for (const t of next) t.columns = t.columns.filter((c) => c.cid !== action.cid);
      break;
    case "moveColumn":
      for (const t of next) shift(t.columns, t.columns.findIndex((c) => c.cid === action.cid), action.dir);
      break;
    case "setColumnWidth": {
      const column = columns().find((c) => c.cid === action.cid);
      if (column) column.width = action.width;
      break;
    }

    case "addSection": {
      const column = columns().find((c) => c.cid === action.columnCid);
      column?.sections.push({ cid: newCid(), id: null, title: "", items: [] });
      break;
    }
    case "removeSection":
      for (const c of columns()) c.sections = c.sections.filter((s) => s.cid !== action.cid);
      break;
    case "moveSection":
      for (const c of columns()) shift(c.sections, c.sections.findIndex((s) => s.cid === action.cid), action.dir);
      break;
    case "moveSectionTo": {
      const section = sections().find((s) => s.cid === action.cid);
      const target = columns().find((c) => c.cid === action.columnCid);
      if (!section || !target || target.sections.includes(section)) break;
      for (const c of columns()) c.sections = c.sections.filter((s) => s.cid !== action.cid);
      target.sections.push(section);
      break;
    }
    case "setSectionTitle": {
      const section = sections().find((s) => s.cid === action.cid);
      if (section) section.title = action.title;
      break;
    }

    case "addAttribute": {
      const section = sections().find((s) => s.cid === action.sectionCid);
      if (!section) break;
      const cid = newCid();
      section.items.push({
        cid,
        id: null,
        key: uniqueKey(next, "attribute", cid),
        title: "",
        type: TemplateItemType.SingleLineText,
        validatingRegex: "",
      });
      break;
    }
    case "removeAttribute":
      for (const s of sections()) s.items = s.items.filter((a) => a.cid !== action.cid);
      break;
    case "moveAttribute":
      for (const s of sections()) shift(s.items, s.items.findIndex((a) => a.cid === action.cid), action.dir);
      break;
    case "moveAttributeTo": {
      const item = allAttributes(next).find((a) => a.cid === action.cid);
      const target = sections().find((s) => s.cid === action.sectionCid);
      if (!item || !target || target.items.includes(item)) break;
      for (const s of sections()) s.items = s.items.filter((a) => a.cid !== action.cid);
      target.items.push(item);
      break;
    }
    case "updateAttribute": {
      const item = allAttributes(next).find((a) => a.cid === action.cid);
      if (!item) break;
      Object.assign(item, action.patch);
      // The key of a not yet saved attribute follows its title; a saved one keeps its key forever.
      if (item.id === null && action.patch.title !== undefined) item.key = uniqueKey(next, slugify(item.title), item.cid);
      break;
    }
  }

  return next;
}
