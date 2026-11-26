import * as React from "react";
import { ApiError } from "../../services/http-client";
import type { CreatedInvitation, InvitationStatus } from "../../services/members-api";
import {
  useChangeMemberRole,
  useCreateInvitation,
  useInvitations,
  useMembers,
  useRemoveMember,
  useRevokeInvitation,
  useRoles,
} from "../../services/members-queries";
import { useProjectPermissions } from "../../services/projects-queries";

const STATUS_LABELS: Record<InvitationStatus, string> = {
  1: "aktivní",
  2: "vypršela",
  3: "vyčerpána",
  4: "zrušena",
};

function errorText(error: unknown): string {
  return error instanceof ApiError ? error.message : "Operace se nezdařila.";
}

function joinLink(key: string): string {
  return `${window.location.origin}/join?key=${encodeURIComponent(key)}`;
}

export function MembersEditor({ projectId }: { projectId: number }) {
  const { canManageMembers } = useProjectPermissions(projectId);
  const members = useMembers(projectId);
  const roles = useRoles(projectId);
  const changeRole = useChangeMemberRole(projectId);
  const removeMember = useRemoveMember(projectId);

  const mutationError = changeRole.error ?? removeMember.error;

  return (
    <div>
      <section>
        <h2 className="text-lg font-semibold">Členové</h2>
        {members.isLoading && <p className="text-gray-500">Načítání…</p>}
        {members.error && <p className="text-red-600">{errorText(members.error)}</p>}
        {mutationError && (
          <p role="alert" className="mt-2 text-sm text-red-600">
            {errorText(mutationError)}
          </p>
        )}
        <ul className="mt-2 space-y-2">
          {members.data?.map((m) => (
            <li key={m.membershipId} className="flex flex-wrap items-center gap-3 rounded border border-gray-200 p-3">
              <span className="font-medium">
                {m.name} {m.surname}
              </span>
              <span className="text-sm text-gray-500">{m.email}</span>
              <span className="mr-auto" />
              {canManageMembers ? (
                <select
                  aria-label={`Role uživatele ${m.email}`}
                  value={m.roleId}
                  disabled={changeRole.isPending}
                  onChange={(e) => changeRole.mutate({ membershipId: m.membershipId, roleId: Number(e.target.value) })}
                  className="rounded border border-gray-300 px-2 py-1 text-sm"
                >
                  {roles.data?.map((r) => (
                    <option key={r.id} value={r.id}>
                      {r.title}
                    </option>
                  ))}
                </select>
              ) : (
                <span className="text-sm">{m.roleTitle}</span>
              )}
              {canManageMembers && (
                <button
                  type="button"
                  disabled={removeMember.isPending}
                  onClick={() => {
                    if (window.confirm(`Odebrat ${m.name} ${m.surname} z projektu?`)) removeMember.mutate(m.membershipId);
                  }}
                  className="text-sm text-red-600 hover:underline"
                >
                  Odebrat
                </button>
              )}
            </li>
          ))}
        </ul>
      </section>

      {canManageMembers && <InvitationsSection projectId={projectId} />}
    </div>
  );
}

function InvitationsSection({ projectId }: { projectId: number }) {
  const roles = useRoles(projectId);
  const invitations = useInvitations(projectId);
  const create = useCreateInvitation(projectId);
  const revoke = useRevokeInvitation(projectId);

  const [roleId, setRoleId] = React.useState<number | null>(null);
  const [days, setDays] = React.useState("7");
  const [maxUses, setMaxUses] = React.useState("10");
  const [created, setCreated] = React.useState<CreatedInvitation | null>(null);
  const [copied, setCopied] = React.useState(false);

  // Owner cannot be invited (an owner is appointed by changing the role of an existing member)
  const invitableRoles = roles.data?.filter((r) => r.systemKind !== 1) ?? [];
  const selectedRole = roleId ?? invitableRoles.find((r) => r.systemKind === 3)?.id ?? invitableRoles[0]?.id ?? null;

  async function handleCreate(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    if (selectedRole === null) return;
    setCopied(false);
    // event handler, not render: the current time is fine here
    // eslint-disable-next-line react-hooks/purity
    const expiresAt = new Date(Date.now() + Number(days) * 24 * 60 * 60 * 1000).toISOString();
    try {
      setCreated(await create.mutateAsync({ roleId: selectedRole, expiresAt, maxUses: Number(maxUses) }));
    } catch {
      setCreated(null); // the error is shown from create.error
    }
  }

  async function copy(text: string) {
    await navigator.clipboard.writeText(text);
    setCopied(true);
  }

  return (
    <section className="mt-8">
      <h2 className="text-lg font-semibold">Pozvánky</h2>

      <form onSubmit={handleCreate} className="mt-2 flex flex-wrap items-end gap-3">
        <label className="text-sm">
          Role
          <select
            value={selectedRole ?? ""}
            onChange={(e) => setRoleId(Number(e.target.value))}
            className="ml-2 rounded border border-gray-300 px-2 py-1"
          >
            {invitableRoles.map((r) => (
              <option key={r.id} value={r.id}>
                {r.title}
              </option>
            ))}
          </select>
        </label>
        <label className="text-sm">
          Platnost (dní)
          <input
            type="number"
            min={1}
            max={90}
            value={days}
            onChange={(e) => setDays(e.target.value)}
            className="ml-2 w-20 rounded border border-gray-300 px-2 py-1"
          />
        </label>
        <label className="text-sm">
          Počet použití
          <input
            type="number"
            min={1}
            max={1000}
            value={maxUses}
            onChange={(e) => setMaxUses(e.target.value)}
            className="ml-2 w-24 rounded border border-gray-300 px-2 py-1"
          />
        </label>
        <button
          type="submit"
          disabled={create.isPending || selectedRole === null}
          className="rounded bg-blue-600 px-3 py-1 text-sm font-medium text-white hover:bg-blue-700"
        >
          Vytvořit pozvánku
        </button>
      </form>

      {create.error && (
        <p role="alert" className="mt-2 text-sm text-red-600">
          {errorText(create.error)}
        </p>
      )}

      {created && (
        <div role="status" className="mt-3 rounded border border-amber-300 bg-amber-50 p-3 text-sm">
          <p className="font-medium">Klíč se zobrazí pouze teď — uložte si ho.</p>
          <p className="mt-1 break-all font-mono">{created.key}</p>
          <p className="mt-1 break-all text-gray-600">{joinLink(created.key)}</p>
          <div className="mt-2 flex gap-3">
            <button type="button" onClick={() => copy(created.key)} className="text-blue-600 hover:underline">
              Kopírovat klíč
            </button>
            <button type="button" onClick={() => copy(joinLink(created.key))} className="text-blue-600 hover:underline">
              Kopírovat odkaz
            </button>
            <button type="button" onClick={() => setCreated(null)} className="text-gray-500 hover:underline">
              Skrýt
            </button>
            {copied && <span className="text-emerald-700">Zkopírováno</span>}
          </div>
        </div>
      )}

      {revoke.error && (
        <p role="alert" className="mt-2 text-sm text-red-600">
          {errorText(revoke.error)}
        </p>
      )}

      <ul className="mt-4 space-y-2">
        {invitations.data?.length === 0 && <li className="text-gray-500">Žádné pozvánky.</li>}
        {invitations.data?.map((i) => (
          <li key={i.id} className="flex flex-wrap items-center gap-3 rounded border border-gray-200 p-3 text-sm">
            <span className="font-mono">{i.keyPrefix}…</span>
            <span>{i.roleTitle}</span>
            <span className="text-gray-500">
              použito {i.usedCount} / {i.maxUses}
            </span>
            <span className="text-gray-500">do {new Date(i.expiresAt).toLocaleDateString("cs-CZ")}</span>
            <span className={i.status === 1 ? "text-emerald-700" : "text-gray-500"}>{STATUS_LABELS[i.status]}</span>
            <span className="mr-auto" />
            {i.status === 1 && (
              <button
                type="button"
                disabled={revoke.isPending}
                onClick={() => revoke.mutate(i.id)}
                className="text-red-600 hover:underline"
              >
                Zrušit
              </button>
            )}
          </li>
        ))}
      </ul>
    </section>
  );
}
