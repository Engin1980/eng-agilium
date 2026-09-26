import { createFileRoute, Link, useParams } from "@tanstack/react-router";
import { ItemType } from "../../../../../services/items-api";
import type { ItemNode } from "../../../../../services/items-api";
import { useProjectItems } from "../../../../../services/items-queries";
import { ItemDetailForm } from "../../../../../components/specific/item-detail-form";

export const Route = createFileRoute("/projects/$id/items/$itemId/")({
  component: RouteComponent,
});

const TYPE_LABELS: Record<ItemType, string> = {
  [ItemType.Feature]: "Feature",
  [ItemType.UserStory]: "User Story",
  [ItemType.Task]: "Task",
  [ItemType.Bug]: "Bug",
};

function findItem(nodes: ItemNode[], id: number): ItemNode | null {
  for (const node of nodes) {
    if (node.id === id) return node;
    const found = findItem(node.subItems, id);
    if (found) return found;
  }
  return null;
}

function RouteComponent() {
  const { id, itemId } = useParams({ from: Route.id });
  const projectId = Number(id);
  const { data, isLoading } = useProjectItems(projectId);
  const item = data ? findItem(data.items, Number(itemId)) : null;

  return (
    <div className="p-6">
      <Link to="/projects/$id" params={{ id }} className="text-sm text-blue-600 hover:underline">
        ← Zpět na projekt
      </Link>

      {isLoading && <p className="mt-4 text-gray-500">Načítání…</p>}
      {!isLoading && !item && <p className="mt-4 text-gray-500">Položka nenalezena.</p>}

      {item && (
        <>
          <header className="mt-4">
            <span className="rounded bg-gray-100 px-2 py-0.5 text-xs font-medium text-gray-600">
              {TYPE_LABELS[item.type]}
            </span>
            <h1 className="mt-1 text-2xl font-semibold">{item.title}</h1>
          </header>

          <main className="mt-6">
            <ItemDetailForm itemId={item.id} />
          </main>
        </>
      )}
    </div>
  );
}
