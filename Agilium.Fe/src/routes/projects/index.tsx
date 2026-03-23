import { createFileRoute } from "@tanstack/react-router";
import * as React from "react";
import { MiniForm } from "../../components/global/forms/area/mini-form";
import { TextInputBlock } from "../../components/global/forms/controls/text-input-block";
import { SubmitInput } from "../../components/global/forms/controls/button-input";
import { Dialog } from "../../components/global/dialogs/Dialog";
import { DialogTitle } from "@radix-ui/react-dialog";

export const Route = createFileRoute("/projects/")({
  component: RouteComponent,
});

type Project = {
  id: number;
  title: string;
  description?: string;
  status: "Active" | "Inactive";
  members: number;
};

const mockProjects: Project[] = [
  {
    id: 1,
    title: "Alpha",
    description: "Project Alpha description.",
    status: "Active",
    members: 4,
  },
  {
    id: 2,
    title: "Beta",
    description: "Project Beta description.",
    status: "Active",
    members: 2,
  },
  {
    id: 3,
    title: "Gamma",
    description: "Project Gamma description.",
    status: "Inactive",
    members: 0,
  },
];

function RouteComponent() {
  const [projects, setProjects] = React.useState<Project[]>(mockProjects);
  const [newProjectTitle, setNewProjectTitle] = React.useState("");
  const [createProjectDialogVisible, setCreateProjectDialogVisible] =
    React.useState(true);

  async function handleCreate(e: any) {
    e.preventDefault();
    const nextId = projects.reduce((m, p) => Math.max(m, p.id), 0) + 1;
    const newProject: Project = {
      id: nextId,
      title: newProjectTitle,
      description: "",
      status: "Active",
      members: 0,
    };

    setProjects((s) => [newProject, ...s]);
    setNewProjectTitle("");
    setCreateProjectDialogVisible(false);
  }

  return (
    <>
      <div style={{ padding: 24 }}>
        <header
          style={{
            display: "flex",
            justifyContent: "space-between",
            alignItems: "center",
            marginBottom: 16,
          }}
        >
          <h1 style={{ margin: 0 }}>Projects</h1>
          <div style={{ display: "flex", gap: 8, alignItems: "center" }}>
            <input
              placeholder="Search projects..."
              style={{ padding: 8, minWidth: 220 }}
            />
            <button
              onClick={() => setCreateProjectDialogVisible(true)}
              style={{
                padding: "8px 12px",
                background: "#2563eb",
                color: "white",
                border: "none",
                borderRadius: 6,
                cursor: "pointer",
                fontWeight: 600,
              }}
            >
              Nový projekt
            </button>
          </div>
        </header>

        <section style={{ display: "grid", gap: 12 }}>
          {projects.map((p) => (
            <article
              key={p.id}
              style={{
                border: "1px solid #e5e7eb",
                borderRadius: 8,
                padding: 12,
              }}
            >
              <div
                style={{
                  display: "flex",
                  justifyContent: "space-between",
                  alignItems: "center",
                }}
              >
                <div>
                  <h2 style={{ margin: 0 }}>{p.title}</h2>
                  <p style={{ margin: "4px 0", color: "#6b7280" }}>
                    {p.description}
                  </p>
                </div>
                <div style={{ textAlign: "right" }}>
                  <div
                    style={{
                      fontSize: 12,
                      color: p.status === "Active" ? "#059669" : "#6b7280",
                    }}
                  >
                    {p.status}
                  </div>
                  <div style={{ fontSize: 12, color: "#374151" }}>
                    {p.members} members
                  </div>
                </div>
              </div>
            </article>
          ))}
        </section>
      </div>

      <Dialog
        open={createProjectDialogVisible}
        onOpenChange={setCreateProjectDialogVisible}
      >
        <DialogTitle>Nový projekt</DialogTitle>
        <MiniForm title="Nový projekt" onSubmit={handleCreate}>
          <div>
            <TextInputBlock
              label="title"
              placeholder="Project name"
              name="title"
              type="text"
              value={newProjectTitle}
              onChange={setNewProjectTitle}
            />
            <SubmitInput label="Create new project" />
          </div>
        </MiniForm>
      </Dialog>
    </>
  );
}
