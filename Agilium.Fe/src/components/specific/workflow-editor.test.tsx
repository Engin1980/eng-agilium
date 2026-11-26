import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import { describe, expect, it } from "vitest";
import { server } from "../../test/server";
import { renderWithProviders } from "../../test/render";
import { WorkflowEditor } from "./workflow-editor";

const API = "http://localhost:5049/api/v1";

const states = [
  { id: 1, title: "To do", type: 1, itemCount: 2 },
  { id: 2, title: "Doing", type: 2, itemCount: 1 },
  { id: 3, title: "Done", type: 3, itemCount: 0 },
];

function serveWorkflow() {
  const saved: unknown[] = [];
  let current = states;
  server.use(
    http.get(`${API}/projects/7/workflow`, () => HttpResponse.json({ states: current })),
    http.put(`${API}/projects/7/workflow`, async ({ request }) => {
      const body = (await request.json()) as { states: { id: number; title: string; type: number }[] };
      saved.push(body);
      current = body.states.map((s) => ({ ...s, itemCount: 0 })); // what the server would return afterwards
      return HttpResponse.json({ movedItems: 1 });
    }),
  );
  return saved;
}

describe("WorkflowEditor", () => {
  it("shows the columns and keeps Save disabled until something changes", async () => {
    serveWorkflow();
    renderWithProviders(<WorkflowEditor projectId={7} />);

    expect(await screen.findByLabelText("Název sloupce 1")).toHaveValue("To do");
    expect(screen.getByRole("button", { name: "Uložit" })).toBeDisabled();
  });

  it("blocks saving when the title is empty and explains why", async () => {
    serveWorkflow();
    renderWithProviders(<WorkflowEditor projectId={7} />);

    await userEvent.clear(await screen.findByLabelText("Název sloupce 2"));

    expect(screen.getByRole("alert")).toHaveTextContent("Každý sloupec musí mít název.");
    expect(screen.getByRole("button", { name: "Uložit" })).toBeDisabled();
  });

  it("blocks saving when no column has the Done type", async () => {
    serveWorkflow();
    renderWithProviders(<WorkflowEditor projectId={7} />);

    await userEvent.selectOptions(await screen.findByLabelText("Stav sloupce 3"), "2");

    expect(screen.getByRole("alert")).toHaveTextContent("Chybí sloupec se stavem Done.");
    expect(screen.getByRole("button", { name: "Uložit" })).toBeDisabled();
  });

  it("does not allow removing a column below the minimum of two", async () => {
    server.use(
      http.get(`${API}/projects/7/workflow`, () => HttpResponse.json({ states: [states[0], states[2]] })),
    );
    renderWithProviders(<WorkflowEditor projectId={7} />);

    expect(await screen.findByRole("button", { name: "Odebrat sloupec 1" })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Odebrat sloupec 2" })).toBeDisabled();
  });

  it("asks for confirmation before removing a column and then sends the new workflow", async () => {
    const saved = serveWorkflow();
    renderWithProviders(<WorkflowEditor projectId={7} />);

    await userEvent.click(await screen.findByRole("button", { name: "Odebrat sloupec 2" }));
    await userEvent.click(screen.getByRole("button", { name: "Uložit" }));

    expect(await screen.findByText(/Rušíte 1 sloupců \(Doing\)/)).toBeInTheDocument();
    expect(saved).toHaveLength(0);

    await userEvent.click(screen.getAllByRole("button", { name: "Uložit" }).at(-1)!);

    await screen.findByText(/Přesunuté položky: 1/);
    expect(saved).toEqual([
      {
        states: [
          { id: 1, title: "To do", type: 1 },
          { id: 3, title: "Done", type: 3 },
        ],
      },
    ]);
  });
});
