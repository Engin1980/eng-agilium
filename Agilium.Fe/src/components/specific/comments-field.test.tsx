import { screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import { describe, expect, it, vi } from "vitest";
import { server } from "../../test/server";
import { renderWithProviders } from "../../test/render";
import { CommentsField } from "./comments-field";

const API = "http://localhost:5049/api/v1";

const base = {
  templateItemId: 5,
  authorId: 1,
  createdAt: "2030-01-01T10:00:00",
  editedAt: null,
  canEdit: false,
  canDelete: false,
};

function serve(comments: object[]) {
  server.use(http.get(`${API}/projects/items/9/comments`, () => HttpResponse.json({ comments })));
}

function renderField(canWrite = true) {
  return renderWithProviders(<CommentsField itemId={9} templateItemId={5} title="Diskuse" canWrite={canWrite} />);
}

describe("CommentsField", () => {
  it("renders Markdown and does not render raw HTML", async () => {
    serve([
      { ...base, id: 1, authorName: "Ada Owner", text: "Some **bold** and `code`\n\n<script>alert(1)</script><b>raw</b>" },
    ]);
    const { container } = renderField();

    expect(await screen.findByText("bold")).toBeInTheDocument();
    expect(screen.getByText("bold").tagName).toBe("STRONG");
    expect(screen.getByText("Ada Owner")).toBeInTheDocument();
    expect(container.querySelector("script")).toBeNull();
    expect(container.querySelector("b")).toBeNull();
  });

  it("opens links in a new tab without opener", async () => {
    serve([{ ...base, id: 1, authorName: "Ada", text: "[web](https://example.com)" }]);
    renderField();

    const link = await screen.findByRole("link", { name: "web" });
    expect(link).toHaveAttribute("target", "_blank");
    expect(link.getAttribute("rel")).toContain("noopener");
  });

  it("shows edit/delete only where the server allows it", async () => {
    serve([
      { ...base, id: 1, authorName: "Mine", text: "a", canEdit: true, canDelete: true },
      { ...base, id: 2, authorName: "Other", text: "b" },
    ]);
    renderField();

    await screen.findByText("Mine");
    expect(screen.getAllByRole("button", { name: "Upravit" })).toHaveLength(1);
    expect(screen.getAllByRole("button", { name: "Smazat" })).toHaveLength(1);
  });

  it("adds a comment, with a preview before sending", async () => {
    serve([]);
    const posted: unknown[] = [];
    server.use(
      http.post(`${API}/projects/items/9/comments`, async ({ request }) => {
        posted.push(await request.json());
        return HttpResponse.json({ id: 3 }, { status: 201 });
      }),
    );
    renderField();

    const submit = await screen.findByRole("button", { name: "Přidat komentář" });
    expect(submit).toBeDisabled();
    await userEvent.type(screen.getByLabelText("Nový komentář"), "hello **there**");
    await userEvent.click(screen.getByRole("button", { name: "Náhled" }));
    expect(within(screen.getByLabelText("Nový komentář (náhled)")).getByText("there").tagName).toBe("STRONG");
    await userEvent.click(screen.getByRole("button", { name: "Upravit" }));
    await userEvent.click(screen.getByRole("button", { name: "Přidat komentář" }));

    await waitFor(() => expect(posted).toEqual([{ templateItemId: 5, text: "hello **there**" }]));
  });

  it("hides the form for a reader without EditItems", async () => {
    serve([{ ...base, id: 1, authorName: "Ada", text: "x" }]);
    renderField(false);

    await screen.findByText("Ada");
    expect(screen.queryByLabelText("Nový komentář")).not.toBeInTheDocument();
  });

  it("edits an own comment", async () => {
    serve([{ ...base, id: 4, authorName: "Mine", text: "old", canEdit: true, canDelete: true }]);
    const patched: unknown[] = [];
    server.use(
      http.patch(`${API}/projects/items/comments/4`, async ({ request }) => {
        patched.push(await request.json());
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderField();

    await userEvent.click(await screen.findByRole("button", { name: "Upravit" }));
    const area = screen.getByLabelText("Upravit komentář");
    await userEvent.clear(area);
    await userEvent.type(area, "new");
    await userEvent.click(screen.getByRole("button", { name: "Uložit změnu" }));

    await waitFor(() => expect(patched).toEqual([{ text: "new" }]));
  });

  it("deletes after confirmation and shows a server error", async () => {
    serve([{ ...base, id: 6, authorName: "Mine", text: "x", canDelete: true }]);
    server.use(
      http.delete(`${API}/projects/items/comments/6`, () =>
        HttpResponse.json({ status: 403, detail: "Authorization failed.", errorKey: "AUTHORIZATION_FAILED", params: [] }, { status: 403 }),
      ),
    );
    vi.spyOn(window, "confirm").mockReturnValue(true);
    renderField();

    await userEvent.click(await screen.findByRole("button", { name: "Smazat" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("Authorization failed");
  });
});
