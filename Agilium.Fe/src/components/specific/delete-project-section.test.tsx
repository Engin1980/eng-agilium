import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import { describe, expect, it, vi } from "vitest";
import { server } from "../../test/server";
import { renderWithProviders } from "../../test/render";
import { DeleteProjectSection } from "./delete-project-section";

const API = "http://localhost:5049/api/v1";

function serve(isOwner: boolean) {
  server.use(
    http.get(`${API}/projects/7`, () =>
      HttpResponse.json({
        id: 7,
        title: "Moje",
        description: "",
        status: 1,
        memberCount: 1,
        roleId: 1,
        roleTitle: "Owner",
        permissions: {
          canViewProject: true,
          canManageProject: true,
          canViewMembers: true,
          canManageMembers: true,
          canManageSprints: true,
          canEditItems: true,
          isOwner,
        },
      }),
    ),
  );
}

describe("DeleteProjectSection", () => {
  it("is hidden from non-owners", async () => {
    serve(false);
    renderWithProviders(<DeleteProjectSection projectId={7} onDeleted={() => {}} />);
    await waitFor(() => expect(screen.queryByRole("button", { name: "Smazat projekt" })).not.toBeInTheDocument());
  });

  it("keeps the confirm button disabled until the title is typed, then deletes", async () => {
    serve(true);
    const deleted: string[] = [];
    server.use(
      http.delete(`${API}/projects/7`, () => {
        deleted.push("7");
        return new HttpResponse(null, { status: 204 });
      }),
    );
    const onDeleted = vi.fn();
    renderWithProviders(<DeleteProjectSection projectId={7} onDeleted={onDeleted} />);

    await userEvent.click(await screen.findByRole("button", { name: "Smazat projekt" }));
    const confirm = screen.getByRole("button", { name: "Trvale smazat" });
    expect(confirm).toBeDisabled();

    await userEvent.type(screen.getByLabelText("Název projektu pro potvrzení"), "Moj");
    expect(confirm).toBeDisabled();
    await userEvent.type(screen.getByLabelText("Název projektu pro potvrzení"), "e");
    expect(confirm).toBeEnabled();
    await userEvent.click(confirm);

    await waitFor(() => expect(onDeleted).toHaveBeenCalled());
    expect(deleted).toEqual(["7"]);
  });
});
