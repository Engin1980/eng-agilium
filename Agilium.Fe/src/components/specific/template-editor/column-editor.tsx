import { AddButton, IconButton, Issues } from "./editor-controls";
import { SectionEditor } from "./section-editor";
import { removalMessage, type DraftColumn } from "./template-draft";
import { useTemplateEditor } from "./template-editor-context";

export function ColumnEditor({
  column,
  index,
  isFirst,
  isLast,
  isOnlyColumn,
}: {
  column: DraftColumn;
  index: number;
  isFirst: boolean;
  isLast: boolean;
  isOnlyColumn: boolean;
}) {
  const { dispatch, confirm } = useTemplateEditor();
  const label = `sloupec ${index + 1}`;

  return (
    <div className="space-y-2 rounded border border-gray-400 bg-white p-2">
      <div className="flex flex-wrap items-center gap-2">
        <span className="text-xs font-semibold text-gray-600">Sloupec {index + 1}</span>
        <label className="flex items-center gap-1 text-xs text-gray-600">
          Šířka
          <input
            type="number"
            min={1}
            step={1}
            aria-label={`Šířka (${label})`}
            className="w-16 rounded border border-gray-300 px-1 py-1 text-sm"
            value={Number.isFinite(column.width) ? column.width : ""}
            onChange={(e) => dispatch({ type: "setColumnWidth", cid: column.cid, width: e.target.valueAsNumber })}
          />
        </label>
        <span className="mr-auto" />
        <IconButton
          label={`Posunout ${label} doleva`}
          disabled={isFirst}
          onClick={() => dispatch({ type: "moveColumn", cid: column.cid, dir: -1 })}
        >
          ←
        </IconButton>
        <IconButton
          label={`Posunout ${label} doprava`}
          disabled={isLast}
          onClick={() => dispatch({ type: "moveColumn", cid: column.cid, dir: 1 })}
        >
          →
        </IconButton>
        <IconButton
          danger
          label={`Odebrat ${label}`}
          disabled={isOnlyColumn}
          onClick={() => confirm(removalMessage("Sloupec", column), () => dispatch({ type: "removeColumn", cid: column.cid }))}
        >
          ✕
        </IconButton>
      </div>
      <Issues cid={column.cid} />

      <div className="space-y-2">
        {column.sections.map((section, i) => (
          <SectionEditor
            key={section.cid}
            section={section}
            columnCid={column.cid}
            isFirst={i === 0}
            isLast={i === column.sections.length - 1}
          />
        ))}
      </div>
      <AddButton onClick={() => dispatch({ type: "addSection", columnCid: column.cid })}>Sekce</AddButton>
    </div>
  );
}
