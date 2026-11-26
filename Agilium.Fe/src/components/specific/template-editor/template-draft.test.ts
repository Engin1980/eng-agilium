import { describe, expect, it } from "vitest";
import { TemplateItemType, type TemplateDto } from "../../../services/templates-api";
import {
  countRemovedSavedAttributes,
  draftFromDto,
  draftReducer,
  draftToInput,
  slugify,
  validateDraft,
  type Draft,
} from "./template-draft";

const dto: TemplateDto = {
  templateId: 1,
  tables: [
    {
      id: 10,
      columns: [
        {
          id: 100,
          width: 8,
          sections: [
            {
              id: 1000,
              title: "Main",
              items: [
                { id: 5, key: "description", title: "Description", type: TemplateItemType.MultiLineText, validatingRegex: null },
              ],
            },
          ],
        },
        {
          id: 101,
          width: 4,
          sections: [
            {
              id: 1001,
              title: "",
              items: [
                { id: 6, key: "priority", title: "Priority", type: TemplateItemType.Integer, validatingRegex: "^\\d+$" },
              ],
            },
          ],
        },
      ],
    },
  ],
};

const load = (): Draft => draftFromDto(dto);

describe("template draft", () => {
  it("round-trips the server template into the update payload", () => {
    const input = draftToInput(load());

    expect(input[0].id).toBe(10);
    expect(input[0].columns.map((c) => c.width)).toEqual([8, 4]);
    expect(input[0].columns[1].sections[0].items[0]).toMatchObject({ id: 6, key: "priority", validatingRegex: "^\\d+$" });
  });

  it("is valid when widths sum to 12 and keys are unique", () => {
    const { errors, warnings } = validateDraft(load());

    expect(errors).toEqual([]);
    expect(warnings).toEqual([]);
  });

  it("only warns (does not block) when widths do not sum to 12", () => {
    let draft = load();
    draft = draftReducer(draft, { type: "setColumnWidth", cid: draft[0].columns[0].cid, width: 3 });

    const { errors, warnings } = validateDraft(draft);

    expect(errors).toEqual([]);
    expect(warnings).toHaveLength(1);
  });

  it("blocks a column width below 1, a table without columns, an empty title and a bad regex", () => {
    let draft = load();
    draft = draftReducer(draft, { type: "setColumnWidth", cid: draft[0].columns[0].cid, width: 0 });
    const attribute = draft[0].columns[1].sections[0].items[0];
    draft = draftReducer(draft, { type: "updateAttribute", cid: attribute.cid, patch: { title: "  ", validatingRegex: "([x" } });
    draft = draftReducer(draft, { type: "addTable" });
    draft = draftReducer(draft, { type: "removeColumn", cid: draft[1].columns[0].cid });

    const messages = validateDraft(draft).errors.map((e) => e.message);

    expect(messages).toContain("Šířka sloupce musí být celé číslo, nejméně 1.");
    expect(messages).toContain("Titulek atributu nesmí být prázdný.");
    expect(messages).toContain("Neplatný regulární výraz.");
    expect(messages).toContain("Tabulka musí mít aspoň jeden sloupec.");
  });

  it("detects duplicate keys (case-insensitive)", () => {
    let draft = load();
    const section = draft[0].columns[0].sections[0];
    draft = draftReducer(draft, { type: "addAttribute", sectionCid: section.cid });
    draft = structuredClone(draft);
    draft[0].columns[0].sections[0].items[1].key = "PRIORITY";

    expect(validateDraft(draft).errors.some((e) => e.message.includes("opakuje"))).toBe(true);
  });

  it("derives the key of a new attribute from its title, but keeps the key of a saved one", () => {
    let draft = load();
    const section = draft[0].columns[0].sections[0];
    draft = draftReducer(draft, { type: "addAttribute", sectionCid: section.cid });
    const created = draft[0].columns[0].sections[0].items[1];

    draft = draftReducer(draft, { type: "updateAttribute", cid: created.cid, patch: { title: "Čas strávený" } });
    expect(draft[0].columns[0].sections[0].items[1].key).toBe("cas-straveny");

    const saved = draft[0].columns[0].sections[0].items[0];
    draft = draftReducer(draft, { type: "updateAttribute", cid: saved.cid, patch: { title: "Renamed" } });
    expect(draft[0].columns[0].sections[0].items[0].key).toBe("description");
  });

  it("moves elements and does nothing at the edge of the list", () => {
    let draft = load();
    draft = draftReducer(draft, { type: "moveColumn", cid: draft[0].columns[0].cid, dir: 1 });
    expect(draft[0].columns.map((c) => c.width)).toEqual([4, 8]);

    draft = draftReducer(draft, { type: "moveColumn", cid: draft[0].columns[1].cid, dir: 1 });
    expect(draft[0].columns.map((c) => c.width)).toEqual([4, 8]);
  });

  it("moves a section into another column", () => {
    let draft = load();
    const section = draft[0].columns[0].sections[0];
    draft = draftReducer(draft, { type: "moveSectionTo", cid: section.cid, columnCid: draft[0].columns[1].cid });

    expect(draft[0].columns[0].sections).toHaveLength(0);
    expect(draft[0].columns[1].sections.map((s) => s.title)).toEqual(["", "Main"]);
  });

  it("counts the saved attributes removed from the draft (to warn about lost values)", () => {
    const original = load();
    let draft = load();
    draft = draftReducer(draft, { type: "removeAttribute", cid: draft[0].columns[1].sections[0].items[0].cid });

    expect(countRemovedSavedAttributes(original, draft)).toBe(1);
  });

  it("slugifies titles without diacritics or odd characters", () => {
    expect(slugify("Čas strávený (h)")).toBe("cas-straveny-h");
    expect(slugify("!!!")).toBe("attribute");
  });
});
