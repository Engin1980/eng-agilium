import * as React from "react";
import { Link } from "@tanstack/react-router";
import { ItemType } from "../../services/items-api";
import type { ItemNode } from "../../services/items-api";
import {
  useCreateItem,
  useDeleteItem,
  useProjectItems,
  useUpdateItem,
  useUpdateItemAssignee,
  useUpdateItemParent,
} from "../../services/items-queries";
import { ApiError } from "../../services/http-client";

const TYPE_LABELS: Record<ItemType, string> = {
  [ItemType.Feature]: "Feature",
  [ItemType.UserStory]: "User Story",
  [ItemType.Task]: "Task",
  [ItemType.Bug]: "Bug",
};

/** Types the user is allowed to add directly under a node of the given type. */
function addableChildTypes(parentType: ItemType): ItemType[] {
  if (parentType === ItemType.Feature) return [ItemType.UserStory];
  if (parentType === ItemType.UserStory) return [ItemType.Task, ItemType.Bug];
  return [];
}

/** The type a valid reparent target must have, for an item of the given type. */
function requiredParentType(itemType: ItemType): ItemType | null {
  if (itemType === ItemType.UserStory) return ItemType.Feature;
  if (itemType === ItemType.Task || itemType === ItemType.Bug) return ItemType.UserStory;
  return null; // Feature is always a root, cannot be reparented
}

function flatten(nodes: ItemNode[]): ItemNode[] {
  return nodes.flatMap((n) => [n, ...flatten(n.subItems)]);
}

export function ItemTree({ projectId }: { projectId: number }) {
  const { data, isLoading, error } = useProjectItems(projectId);
  const createItem = useCreateItem(projectId);
  const [addingFeature, setAddingFeature] = React.useState(false);

  if (isLoading) return <p className="text-gray-500">Načítání položek…</p>;
  if (error)
    return (
      <p className="text-red-600">
        {error instanceof ApiError ? error.message : "Nepodařilo se načíst položky."}
      </p>
    );

  const items = data?.items ?? [];
  const allItems = flatten(items);

  return (
    <div>
      <div className="mb-3 flex items-center justify-between">
        <h2 className="text-lg font-semibold">Položky</h2>
        <button
          onClick={() => setAddingFeature(true)}
          className="rounded bg-blue-600 px-3 py-1 text-sm font-medium text-white hover:bg-blue-700"
        >
          + Feature
        </button>
      </div>

      {addingFeature && (
        <div className="mb-3">
          <CreateItemInlineForm
            type={ItemType.Feature}
            onCancel={() => setAddingFeature(false)}
            onSubmit={async (title) => {
              await createItem.mutateAsync({ title, type: ItemType.Feature, projectId, parentId: null });
              setAddingFeature(false);
            }}
          />
        </div>
      )}

      {items.length === 0 && !addingFeature && (
        <p className="text-gray-500">Zatím žádné položky. Začněte přidáním feature.</p>
      )}

      <ul className="space-y-2">
        {items.map((item) => (
          <ItemNodeView key={item.id} node={item} projectId={projectId} allItems={allItems} depth={0} />
        ))}
      </ul>
    </div>
  );
}

function ItemNodeView({
  node,
  projectId,
  allItems,
  depth,
}: {
  node: ItemNode;
  projectId: number;
  allItems: ItemNode[];
  depth: number;
}) {
  const updateItem = useUpdateItem(projectId);
  const updateParent = useUpdateItemParent(projectId);
  const updateAssignee = useUpdateItemAssignee(projectId);
  const deleteItem = useDeleteItem(projectId);

  const [isEditing, setIsEditing] = React.useState(false);
  const [editTitle, setEditTitle] = React.useState(node.title);
  const [addingChildType, setAddingChildType] = React.useState<ItemType | null>(null);
  const [assigneeInput, setAssigneeInput] = React.useState("");
  const [errorMessage, setErrorMessage] = React.useState<string | null>(null);

  const createChild = useCreateItem(projectId);
  const childTypes = addableChildTypes(node.type);
  const parentType = requiredParentType(node.type);
  const reparentTargets = parentType
    ? allItems.filter((i) => i.type === parentType && i.id !== node.id)
    : [];

  async function runAction(action: () => Promise<unknown>) {
    setErrorMessage(null);
    try {
      await action();
    } catch (err) {
      setErrorMessage(err instanceof ApiError ? err.message : "Akce se nezdařila.");
    }
  }

  return (
    <li style={{ marginLeft: depth * 20 }}>
      <div className="flex flex-wrap items-center gap-2 rounded border border-gray-200 bg-white px-3 py-2">
        <span className="rounded bg-gray-100 px-2 py-0.5 text-xs font-medium text-gray-600">
          {TYPE_LABELS[node.type]}
        </span>

        {isEditing ? (
          <>
            <input
              className="rounded border border-gray-300 px-2 py-1 text-sm"
              value={editTitle}
              onChange={(e) => setEditTitle(e.target.value)}
              autoFocus
            />
            <button
              className="text-sm text-blue-600 hover:underline"
              onClick={() =>
                runAction(async () => {
                  await updateItem.mutateAsync({ id: node.id, title: editTitle, type: node.type });
                  setIsEditing(false);
                })
              }
            >
              Uložit
            </button>
            <button
              className="text-sm text-gray-500 hover:underline"
              onClick={() => {
                setEditTitle(node.title);
                setIsEditing(false);
              }}
            >
              Zrušit
            </button>
          </>
        ) : (
          <Link
            to="/projects/$id/items/$itemId"
            params={{ id: String(projectId), itemId: String(node.id) }}
            className="font-medium text-blue-700 hover:underline"
          >
            {node.title}
          </Link>
        )}

        {node.isGeneric && (
          <span className="rounded bg-amber-100 px-2 py-0.5 text-xs text-amber-700">
            automatická položka
          </span>
        )}

        <span className="text-sm text-gray-500">
          {node.assignee ? `${node.assignee.name} ${node.assignee.surname}` : "Nepřiřazeno"}
        </span>

        {!node.isGeneric && !isEditing && (
          <>
            <button className="text-sm text-blue-600 hover:underline" onClick={() => setIsEditing(true)}>
              Přejmenovat
            </button>
            <button
              className="text-sm text-red-600 hover:underline"
              onClick={() => {
                if (window.confirm(`Smazat "${node.title}" včetně případných podřízených položek?`)) {
                  runAction(() => deleteItem.mutateAsync(node.id));
                }
              }}
            >
              Smazat
            </button>
          </>
        )}

        {reparentTargets.length > 0 && !node.isGeneric && (
          <select
            className="rounded border border-gray-300 px-2 py-1 text-sm"
            value={node.parentId ?? ""}
            onChange={(e) =>
              runAction(() => updateParent.mutateAsync({ id: node.id, parentId: Number(e.target.value) }))
            }
          >
            {reparentTargets.map((t) => (
              <option key={t.id} value={t.id}>
                Přesunout pod: {t.title}
              </option>
            ))}
          </select>
        )}

        <span className="flex items-center gap-1">
          <input
            className="w-16 rounded border border-gray-300 px-2 py-1 text-sm"
            placeholder="ID uživatele"
            value={assigneeInput}
            onChange={(e) => setAssigneeInput(e.target.value)}
          />
          <button
            className="text-sm text-blue-600 hover:underline"
            onClick={() =>
              runAction(async () => {
                const id = assigneeInput.trim() === "" ? null : Number(assigneeInput);
                await updateAssignee.mutateAsync({ id: node.id, assigneeId: id });
                setAssigneeInput("");
              })
            }
          >
            Přiřadit
          </button>
        </span>

        {childTypes.map((t) => (
          <button
            key={t}
            className="text-sm text-emerald-700 hover:underline"
            onClick={() => setAddingChildType(t)}
          >
            + {TYPE_LABELS[t]}
          </button>
        ))}
      </div>

      {errorMessage && <div className="ml-3 mt-1 text-sm text-red-600">{errorMessage}</div>}

      {addingChildType !== null && (
        <div className="mt-2" style={{ marginLeft: 20 }}>
          <CreateItemInlineForm
            type={addingChildType}
            onCancel={() => setAddingChildType(null)}
            onSubmit={async (title) => {
              await createChild.mutateAsync({
                title,
                type: addingChildType,
                projectId,
                parentId: node.id,
              });
              setAddingChildType(null);
            }}
          />
        </div>
      )}

      {node.subItems.length > 0 && (
        <ul className="mt-2 space-y-2">
          {node.subItems.map((child) => (
            <ItemNodeView key={child.id} node={child} projectId={projectId} allItems={allItems} depth={depth + 1} />
          ))}
        </ul>
      )}
    </li>
  );
}

function CreateItemInlineForm({
  type,
  onSubmit,
  onCancel,
}: {
  type: ItemType;
  onSubmit: (title: string) => Promise<void>;
  onCancel: () => void;
}) {
  const [title, setTitle] = React.useState("");
  const [error, setError] = React.useState<string | null>(null);
  const [isSubmitting, setIsSubmitting] = React.useState(false);

  async function handleSubmit(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setError(null);
    setIsSubmitting(true);
    try {
      await onSubmit(title);
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Založení položky se nezdařilo.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <form onSubmit={handleSubmit} className="flex items-center gap-2">
      <input
        className="rounded border border-gray-300 px-2 py-1 text-sm"
        placeholder={`Název (${TYPE_LABELS[type]})`}
        value={title}
        onChange={(e) => setTitle(e.target.value)}
        autoFocus
      />
      <button
        type="submit"
        disabled={isSubmitting}
        className="rounded bg-emerald-600 px-2 py-1 text-sm text-white hover:bg-emerald-700"
      >
        Přidat
      </button>
      <button type="button" onClick={onCancel} className="text-sm text-gray-500 hover:underline">
        Zrušit
      </button>
      {error && <span className="text-sm text-red-600">{error}</span>}
    </form>
  );
}
