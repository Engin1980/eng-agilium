import * as React from "react";
import { Dialog } from "../global/dialogs/Dialog";
import { ApiError } from "../../services/http-client";
import { useDeleteProject, useProject, useProjectPermissions } from "../../services/projects-queries";

/** "Danger zone" of the project settings: permanent deletion, visible to the owner only (the BE enforces it). */
export function DeleteProjectSection({ projectId, onDeleted }: { projectId: number; onDeleted: () => void }) {
  const { isOwner } = useProjectPermissions(projectId);
  const project = useProject(projectId);
  const remove = useDeleteProject();
  const [open, setOpen] = React.useState(false);
  const [typed, setTyped] = React.useState("");

  if (!isOwner || !project.data) return null;
  const title = project.data.title;

  async function handleDelete() {
    try {
      await remove.mutateAsync(projectId);
      setOpen(false);
      onDeleted();
    } catch {
      // shown from remove.error
    }
  }

  return (
    <section className="mt-10 rounded border border-red-200 p-4">
      <h2 className="font-semibold text-red-700">Nebezpečná zóna</h2>
      <p className="mt-1 text-sm text-gray-600">
        Smazání projektu je trvalé: odstraní všechny položky, sprinty, šablony, členy a pozvánky.
      </p>
      <button
        type="button"
        onClick={() => {
          setTyped("");
          remove.reset();
          setOpen(true);
        }}
        className="mt-3 rounded bg-red-600 px-3 py-2 text-sm font-medium text-white hover:bg-red-700"
      >
        Smazat projekt
      </button>

      <Dialog open={open} onOpenChange={setOpen} title="Smazat projekt">
        <p className="text-sm text-gray-700">
          Tuto akci nelze vrátit. Pro potvrzení opište název projektu: <strong>{title}</strong>
        </p>
        <input
          aria-label="Název projektu pro potvrzení"
          value={typed}
          onChange={(e) => setTyped(e.target.value)}
          className="mt-3 w-full rounded border border-gray-300 px-2 py-1 text-sm"
        />
        {remove.error && (
          <p role="alert" className="mt-2 text-sm text-red-600">
            {remove.error instanceof ApiError ? remove.error.message : "Smazání se nezdařilo."}
          </p>
        )}
        <div className="mt-4 flex justify-end gap-2">
          <button
            type="button"
            onClick={() => setOpen(false)}
            className="rounded border border-gray-300 px-3 py-2 text-sm hover:bg-gray-50"
          >
            Zrušit
          </button>
          <button
            type="button"
            disabled={typed !== title || remove.isPending}
            onClick={handleDelete}
            className="rounded bg-red-600 px-3 py-2 text-sm font-medium text-white hover:bg-red-700 disabled:opacity-50"
          >
            Trvale smazat
          </button>
        </div>
      </Dialog>
    </section>
  );
}
