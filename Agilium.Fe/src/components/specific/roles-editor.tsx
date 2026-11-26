import * as React from "react";
import { ApiError } from "../../services/http-client";
import type { ProjectRole, RoleFlags } from "../../services/members-api";
import { useCreateRole, useDeleteRole, useRoles, useUpdateRole } from "../../services/members-queries";
import { useProjectPermissions } from "../../services/projects-queries";

const FLAGS: { key: keyof RoleFlags; label: string }[] = [
  { key: "canViewProject", label: "Zobrazit projekt" },
  { key: "canEditItems", label: "Upravovat položky" },
  { key: "canManageSprints", label: "Spravovat sprinty" },
  { key: "canViewMembers", label: "Zobrazit členy" },
  { key: "canManageMembers", label: "Spravovat členy a role" },
  { key: "canManageProject", label: "Spravovat projekt" },
];

const NO_FLAGS: RoleFlags = {
  canViewProject: true,
  canManageProject: false,
  canViewMembers: true,
  canManageMembers: false,
  canManageSprints: false,
  canEditItems: false,
};

function errorText(error: unknown): string {
  return error instanceof ApiError ? error.message : "Operace se nezdařila.";
}

export function RolesEditor({ projectId }: { projectId: number }) {
  const { canManageMembers } = useProjectPermissions(projectId);
  const roles = useRoles(projectId);
  const create = useCreateRole(projectId);
  const [newTitle, setNewTitle] = React.useState("");
  const [newFlags, setNewFlags] = React.useState<RoleFlags>(NO_FLAGS);

  async function handleCreate(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    try {
      await create.mutateAsync({ title: newTitle, ...newFlags });
      setNewTitle("");
      setNewFlags(NO_FLAGS);
    } catch {
      // shown from create.error
    }
  }

  return (
    <div>
      {roles.isLoading && <p className="text-gray-500">Načítání…</p>}
      {roles.error && <p className="text-red-600">{errorText(roles.error)}</p>}

      <ul className="space-y-3">
        {roles.data?.map((role) => (
          <RoleRow key={role.id} projectId={projectId} role={role} canEdit={canManageMembers} />
        ))}
      </ul>

      {canManageMembers && (
        <form onSubmit={handleCreate} className="mt-8 rounded border border-gray-200 p-3">
          <h2 className="font-semibold">Nová role</h2>
          <input
            aria-label="Název nové role"
            value={newTitle}
            onChange={(e) => setNewTitle(e.target.value)}
            placeholder="Název"
            className="mt-2 rounded border border-gray-300 px-2 py-1 text-sm"
          />
          <FlagBoxes flags={newFlags} onChange={setNewFlags} />
          {create.error && (
            <p role="alert" className="mt-2 text-sm text-red-600">
              {errorText(create.error)}
            </p>
          )}
          <button
            type="submit"
            disabled={create.isPending || newTitle.trim() === ""}
            className="mt-2 rounded bg-blue-600 px-3 py-1 text-sm font-medium text-white hover:bg-blue-700"
          >
            Vytvořit roli
          </button>
        </form>
      )}
    </div>
  );
}

function FlagBoxes({
  flags,
  onChange,
  disabled,
}: {
  flags: RoleFlags;
  onChange?: (flags: RoleFlags) => void;
  disabled?: boolean;
}) {
  return (
    <div className="mt-2 flex flex-wrap gap-4">
      {FLAGS.map((f) => (
        <label key={f.key} className="flex items-center gap-1 text-sm">
          <input
            type="checkbox"
            checked={flags[f.key]}
            disabled={disabled}
            onChange={(e) => onChange?.({ ...flags, [f.key]: e.target.checked })}
          />
          {f.label}
        </label>
      ))}
    </div>
  );
}

function RoleRow({ projectId, role, canEdit }: { projectId: number; role: ProjectRole; canEdit: boolean }) {
  const update = useUpdateRole(projectId);
  const remove = useDeleteRole(projectId);
  const [title, setTitle] = React.useState(role.title);
  const [flags, setFlags] = React.useState<RoleFlags>(role);
  const readOnly = role.isSystem || !canEdit;
  const error = update.error ?? remove.error;

  return (
    <li className="rounded border border-gray-200 p-3">
      <div className="flex flex-wrap items-center gap-3">
        <input
          aria-label={`Název role ${role.title}`}
          value={title}
          disabled={readOnly}
          onChange={(e) => setTitle(e.target.value)}
          className="rounded border border-gray-300 px-2 py-1 text-sm disabled:bg-gray-100"
        />
        {role.isSystem && <span className="rounded bg-gray-100 px-2 py-0.5 text-xs text-gray-600">systémová</span>}
        <span className="text-sm text-gray-500">členů: {role.memberCount}</span>
        <span className="mr-auto" />
        {!readOnly && (
          <>
            <button
              type="button"
              disabled={update.isPending}
              onClick={() => update.mutate({ id: role.id, title, ...flags })}
              className="text-sm text-blue-600 hover:underline"
            >
              Uložit
            </button>
            <button
              type="button"
              disabled={remove.isPending}
              onClick={() => {
                if (window.confirm(`Smazat roli „${role.title}“?`)) remove.mutate(role.id);
              }}
              className="text-sm text-red-600 hover:underline"
            >
              Smazat
            </button>
          </>
        )}
      </div>
      <FlagBoxes flags={flags} onChange={setFlags} disabled={readOnly} />
      {error && (
        <p role="alert" className="mt-2 text-sm text-red-600">
          {errorText(error)}
        </p>
      )}
    </li>
  );
}
