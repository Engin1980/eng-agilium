import { TEMPLATE_ITEM_TYPE_LABELS, TemplateItemType } from "../../../services/templates-api";
import { IconButton, Issues, MoveToSelect } from "./editor-controls";
import { removalMessage, type DraftAttribute } from "./template-draft";
import { useTemplateEditor } from "./template-editor-context";

const TEXT_TYPES: TemplateItemType[] = [TemplateItemType.SingleLineText, TemplateItemType.MultiLineText];
const inputClass = "w-full rounded border border-gray-300 px-2 py-1 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500";

export function AttributeEditor({
  attribute,
  sectionCid,
  isFirst,
  isLast,
}: {
  attribute: DraftAttribute;
  sectionCid: string;
  isFirst: boolean;
  isLast: boolean;
}) {
  const { dispatch, confirm, sectionTargets } = useTemplateEditor();
  const isSaved = attribute.id !== null;
  const label = attribute.title || "nový atribut";

  return (
    <div className="space-y-1 rounded border border-gray-200 bg-white p-2">
      <div className="flex flex-wrap items-center gap-2">
        <input
          aria-label={`Titulek atributu ${label}`}
          className={`${inputClass} min-w-40 flex-1`}
          placeholder="Titulek"
          value={attribute.title}
          // Focus a freshly added attribute so the user can type its title right away.
          autoFocus={!isSaved && attribute.title === ""}
          onChange={(e) => dispatch({ type: "updateAttribute", cid: attribute.cid, patch: { title: e.target.value } })}
        />
        <select
          aria-label={`Typ atributu ${label}`}
          className="rounded border border-gray-300 px-1 py-1 text-sm disabled:bg-gray-50"
          value={attribute.type}
          // The type of a saved attribute is fixed (remove it and add a new one instead).
          disabled={isSaved}
          title={isSaved ? "Typ uloženého atributu nelze změnit – odeberte ho a přidejte nový." : undefined}
          onChange={(e) =>
            dispatch({
              type: "updateAttribute",
              cid: attribute.cid,
              patch: { type: Number(e.target.value) as TemplateItemType },
            })
          }
        >
          {Object.entries(TEMPLATE_ITEM_TYPE_LABELS).map(([value, text]) => (
            <option key={value} value={value}>
              {text}
            </option>
          ))}
        </select>
      </div>

      {TEXT_TYPES.includes(attribute.type) && (
        <input
          aria-label={`Regulární výraz pro ${label}`}
          className={inputClass}
          placeholder="Validační regulární výraz (nepovinné)"
          value={attribute.validatingRegex}
          onChange={(e) =>
            dispatch({ type: "updateAttribute", cid: attribute.cid, patch: { validatingRegex: e.target.value } })
          }
        />
      )}

      <div className="flex flex-wrap items-center gap-1">
        <span className="mr-auto text-xs text-gray-400">klíč: {attribute.key}</span>
        <IconButton
          label={`Posunout atribut ${label} nahoru`}
          disabled={isFirst}
          onClick={() => dispatch({ type: "moveAttribute", cid: attribute.cid, dir: -1 })}
        >
          ↑
        </IconButton>
        <IconButton
          label={`Posunout atribut ${label} dolů`}
          disabled={isLast}
          onClick={() => dispatch({ type: "moveAttribute", cid: attribute.cid, dir: 1 })}
        >
          ↓
        </IconButton>
        <MoveToSelect
          label="Přesunout do…"
          targets={sectionTargets}
          currentCid={sectionCid}
          onPick={(cid) => dispatch({ type: "moveAttributeTo", cid: attribute.cid, sectionCid: cid })}
        />
        <IconButton
          danger
          label={`Odebrat atribut ${label}`}
          onClick={() =>
            confirm(removalMessage("Atribut", attribute), () => dispatch({ type: "removeAttribute", cid: attribute.cid }))
          }
        >
          ✕
        </IconButton>
      </div>
      <Issues cid={attribute.cid} />
    </div>
  );
}
