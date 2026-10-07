import { AttributeEditor } from "./attribute-editor";
import { AddButton, IconButton, Issues, MoveToSelect } from "./editor-controls";
import { removalMessage, type DraftSection } from "./template-draft";
import { useTemplateEditor } from "./template-editor-context";

export function SectionEditor({
  section,
  columnCid,
  isFirst,
  isLast,
}: {
  section: DraftSection;
  columnCid: string;
  isFirst: boolean;
  isLast: boolean;
}) {
  const { dispatch, confirm, columnTargets } = useTemplateEditor();
  const label = section.title || "bez titulku";

  return (
    <div className="space-y-2 rounded border border-gray-300 bg-gray-50 p-2">
      <div className="flex flex-wrap items-center gap-2">
        <input
          aria-label={`Titulek sekce ${label}`}
          className="min-w-32 flex-1 rounded border border-gray-300 px-2 py-1 text-sm font-semibold focus:outline-none focus:ring-2 focus:ring-indigo-500"
          placeholder="Titulek sekce (může být prázdný)"
          value={section.title}
          onChange={(e) => dispatch({ type: "setSectionTitle", cid: section.cid, title: e.target.value })}
        />
        <IconButton
          label={`Posunout sekci ${label} nahoru`}
          disabled={isFirst}
          onClick={() => dispatch({ type: "moveSection", cid: section.cid, dir: -1 })}
        >
          ↑
        </IconButton>
        <IconButton
          label={`Posunout sekci ${label} dolů`}
          disabled={isLast}
          onClick={() => dispatch({ type: "moveSection", cid: section.cid, dir: 1 })}
        >
          ↓
        </IconButton>
        <MoveToSelect
          label="Přesunout do…"
          targets={columnTargets}
          currentCid={columnCid}
          onPick={(cid) => dispatch({ type: "moveSectionTo", cid: section.cid, columnCid: cid })}
        />
        <IconButton
          danger
          label={`Odebrat sekci ${label}`}
          onClick={() => confirm(removalMessage("Sekce", section), () => dispatch({ type: "removeSection", cid: section.cid }))}
        >
          ✕
        </IconButton>
      </div>

      <div className="space-y-2">
        {section.items.map((item, i) => (
          <AttributeEditor
            key={item.cid}
            attribute={item}
            sectionCid={section.cid}
            isFirst={i === 0}
            isLast={i === section.items.length - 1}
          />
        ))}
        {section.items.length === 0 && <p className="text-xs text-gray-400">Prázdná sekce.</p>}
      </div>
      <AddButton onClick={() => dispatch({ type: "addAttribute", sectionCid: section.cid })}>Atribut</AddButton>
      <Issues cid={section.cid} />
    </div>
  );
}
