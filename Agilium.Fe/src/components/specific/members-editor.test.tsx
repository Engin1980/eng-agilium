import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import { describe, expect, it, vi } from "vitest";
import { server } from "../../test/server";
import { renderWithProviders } from "../../test/render";
import { MembersEditor } from "./members-editor";
import { RolesEditor } from "./roles-editor";

const API = "http://localhost:5049/api/v1";

const allPermissions = {
  canViewProject: true,
  canManageProject: true,
  canViewMembers: true,
  canManageMembers: true,
  canManageSprints: true,
  canEditItems: true,
  isOwner: true,
};

const roleFlags = {
  canViewProject: true,
  canManageProject: false,
  canViewMembers: true,
  canManageMembers: false,
  canManageSprints: false,
  canEditItems: false,
};

const roles = [
  { id: 1, title: "Owner", isSystem: true, systemKind: 1, memberCount: 1, ...roleFlags, canManageMembers: true },
  { id: 3, title: "Member", isSystem: true, systemKind: 3, memberCount: 1, ...roleFlags },
  { id: 9, title: "Tester", isSystem: false, systemKind: 0, memberCount: 0, ...roleFlags },
];

const members = [
  { membershipId: 10, userId: 1, name: "Ada", surname: "Owner", email: "ada@example.com", roleId: 1, roleTitle: "Owner" },
  { membershipId: 11, userId: 2, name: "Bob", surname: "Member", email: "bob@example.com", roleId: 3, roleTitle: "Member" },
];

function serve(permissions = allPermissions) {
  server.use(
    http.get(`${API}/projects/7`, () =>
      HttpResponse.json({ id: 7, title: "P", description: "", status: 1, memberCount: 2, roleId: 1, roleTitle: "Owner", permissions }),
    ),
    http.get(`${API}/projects/7/members`, () => HttpResponse.json({ members })),
    http.get(`${API}/projects/7/roles`, () => HttpResponse.json({ roles })),
    http.get(`${API}/projects/7/invitations`, () =>
      HttpResponse.json({
        invitations: [
          { id: 5, keyPrefix: "abc123", roleId: 3, roleTitle: "Member", expiresAt: "2030-01-01T00:00:00Z", maxUses: 10, usedCount: 2, createdAt: "2029-12-01T00:00:00Z", status: 1 },
          { id: 6, keyPrefix: "zzz999", roleId: 3, roleTitle: "Member", expiresAt: "2030-01-01T00:00:00Z", maxUses: 10, usedCount: 0, createdAt: "2029-12-01T00:00:00Z", status: 4 },
        ],
      }),
    ),
  );
}

describe("MembersEditor", () => {
  it("lists members and changes a role", async () => {
    serve();
    const changed: unknown[] = [];
    server.use(
      http.patch(`${API}/projects/members/11`, async ({ request }) => {
        changed.push(await request.json());
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderWithProviders(<MembersEditor projectId={7} />);

    expect(await screen.findByText("Bob Member")).toBeInTheDocument();
    await userEvent.selectOptions(await screen.findByLabelText("Role uživatele bob@example.com"), "Owner");

    await waitFor(() => expect(changed).toEqual([{ roleId: 1 }]));
  });

  it("shows the server error when a member cannot be removed", async () => {
    serve();
    server.use(
      http.delete(`${API}/projects/membership/11`, () =>
        HttpResponse.json({ status: 400, detail: "Cannot remove membership: user is assigned to items in the project", errorKey: "GENERIC_BAD_REQUEST", params: [] }, { status: 400 }),
      ),
    );
    vi.spyOn(window, "confirm").mockReturnValue(true);
    renderWithProviders(<MembersEditor projectId={7} />);

    const buttons = await screen.findAllByRole("button", { name: "Odebrat" });
    await userEvent.click(buttons[1]);

    expect(await screen.findByRole("alert")).toHaveTextContent("user is assigned to items");
  });

  it("shows a created key once and offers copying; the list never shows keys", async () => {
    serve();
    server.use(
      http.post(`${API}/projects/7/invitations`, () =>
        HttpResponse.json({ id: 8, key: "SECRET-KEY-VALUE", keyPrefix: "SECRET", expiresAt: "2030-01-01T00:00:00Z", maxUses: 10 }, { status: 201 }),
      ),
    );
    const writeText = vi.fn().mockResolvedValue(undefined);
    Object.defineProperty(navigator, "clipboard", { value: { writeText }, configurable: true });
    renderWithProviders(<MembersEditor projectId={7} />);

    await userEvent.click(await screen.findByRole("button", { name: "Vytvořit pozvánku" }));

    expect(await screen.findByText("SECRET-KEY-VALUE")).toBeInTheDocument();
    await userEvent.click(screen.getByRole("button", { name: "Kopírovat odkaz" }));
    expect(writeText).toHaveBeenCalledWith(expect.stringContaining("/join?key=SECRET-KEY-VALUE"));

    await userEvent.click(screen.getByRole("button", { name: "Skrýt" }));
    expect(screen.queryByText("SECRET-KEY-VALUE")).not.toBeInTheDocument();
    expect(screen.getByText("abc123…")).toBeInTheDocument();
  });

  it("revokes an active invitation (revoked ones have no button)", async () => {
    serve();
    const revoked: string[] = [];
    server.use(
      http.delete(`${API}/projects/invitations/:id`, ({ params }) => {
        revoked.push(String(params.id));
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderWithProviders(<MembersEditor projectId={7} />);

    const buttons = await screen.findAllByRole("button", { name: "Zrušit" });
    expect(buttons).toHaveLength(1);
    await userEvent.click(buttons[0]);

    await waitFor(() => expect(revoked).toEqual(["5"]));
  });

  it("hides management controls from a plain member", async () => {
    serve({ ...allPermissions, canManageMembers: false, isOwner: false });
    renderWithProviders(<MembersEditor projectId={7} />);

    expect(await screen.findByText("Bob Member")).toBeInTheDocument();
    await waitFor(() => expect(screen.queryByLabelText("Role uživatele bob@example.com")).not.toBeInTheDocument());
    expect(screen.queryByRole("button", { name: "Vytvořit pozvánku" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Odebrat" })).not.toBeInTheDocument();
  });
});

describe("RolesEditor", () => {
  it("makes system roles read-only and custom ones editable", async () => {
    serve();
    renderWithProviders(<RolesEditor projectId={7} />);

    expect(await screen.findByLabelText("Název role Owner")).toBeDisabled();
    expect(screen.getByLabelText("Název role Tester")).toBeEnabled();
    expect(screen.getAllByRole("button", { name: "Smazat" })).toHaveLength(1);
  });

  it("creates a role", async () => {
    serve();
    const created: unknown[] = [];
    server.use(
      http.post(`${API}/projects/7/roles`, async ({ request }) => {
        created.push(await request.json());
        return HttpResponse.json({ id: 20 }, { status: 201 });
      }),
    );
    renderWithProviders(<RolesEditor projectId={7} />);

    await userEvent.type(await screen.findByLabelText("Název nové role"), "Reviewer");
    await userEvent.click(screen.getByRole("button", { name: "Vytvořit roli" }));

    await waitFor(() => expect(created).toHaveLength(1));
    expect(created[0]).toMatchObject({ title: "Reviewer", canViewProject: true, canEditItems: false });
  });
});
