import { ColumnEditor } from "./column-editor";
import { AddButton, IconButton, Issues } from "./editor-controls";
import { removalMessage, type DraftTable } from "./template-draft";
import { useTemplateEditor } from "./template-editor-context";

export function TableEditor({
  table,
  index,
  isFirst,
  isLast,
}: {
  table: DraftTable;
  index: number;
  isFirst: boolean;
  isLast: boolean;
}) {
  const { dispatch, confirm } = useTemplateEditor();
  const label = `tabulku ${index + 1}`;

  return (
    <div className="space-y-2 rounded-lg border-2 border-gray-300 p-3">
      <div className="flex flex-wrap items-center gap-2">
        <h3 className="mr-auto font-semibold text-gray-800">Tabulka {index + 1}</h3>
        <IconButton
          label={`Posunout ${label} nahoru`}
          disabled={isFirst}
          onClick={() => dispatch({ type: "moveTable", cid: table.cid, dir: -1 })}
        >
          ↑
        </IconButton>
        <IconButton
          label={`Posunout ${label} dolů`}
          disabled={isLast}
          onClick={() => dispatch({ type: "moveTable", cid: table.cid, dir: 1 })}
        >
          ↓
        </IconButton>
        <IconButton
          danger
          label={`Odebrat ${label}`}
          onClick={() => confirm(removalMessage("Tabulka", table), () => dispatch({ type: "removeTable", cid: table.cid }))}
        >
          ✕
        </IconButton>
      </div>
      <Issues cid={table.cid} />

      <div className="grid gap-2 md:grid-flow-col md:auto-cols-fr">
        {table.columns.map((column, i) => (
          <ColumnEditor
            key={column.cid}
            column={column}
            index={i}
            isFirst={i === 0}
            isLast={i === table.columns.length - 1}
            isOnlyColumn={table.columns.length === 1}
          />
        ))}
      </div>
      <AddButton onClick={() => dispatch({ type: "addColumn", tableCid: table.cid })}>Sloupec</AddButton>
    </div>
  );
}
