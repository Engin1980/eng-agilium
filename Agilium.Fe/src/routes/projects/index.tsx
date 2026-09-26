import { createFileRoute, Link, useNavigate } from "@tanstack/react-router";
import * as React from "react";
import { MiniForm } from "../../components/global/forms/area/mini-form";
import { TextInputBlock } from "../../components/global/forms/controls/text-input-block";
import { TextAreaInputBlock } from "../../components/global/forms/controls/text-area-input-block";
import { SubmitInput } from "../../components/global/forms/controls/button-input";
import { Dialog } from "../../components/global/dialogs/Dialog";
import { DialogTitle } from "@radix-ui/react-dialog";
import { useAuth } from "../../contexts/auth-context";
import { useCreateProject, useProjects } from "../../services/projects-queries";
import { ApiError } from "../../services/http-client";

export const Route = createFileRoute("/projects/")({
  component: RouteComponent,
});

function RouteComponent() {
  const { user, isRestoringSession } = useAuth();
  const navigate = useNavigate();

  React.useEffect(() => {
    if (!isRestoringSession && !user) {
      navigate({ to: "/login" });
    }
  }, [isRestoringSession, user, navigate]);

  if (isRestoringSession || !user) {
    return <div className="p-6 text-gray-500">Načítání…</div>;
  }

  return <ProjectsList />;
}

function ProjectsList() {
  const { data, isLoading, error } = useProjects();
  const createProject = useCreateProject();

  const [createProjectDialogVisible, setCreateProjectDialogVisible] =
    React.useState(false);
  const [newProjectTitle, setNewProjectTitle] = React.useState("");
  const [newProjectDescription, setNewProjectDescription] = React.useState("");
  const [createError, setCreateError] = React.useState<string | null>(null);

  async function handleCreate(e: React.FormEvent<HTMLFormElement>) {
    e.preventDefault();
    setCreateError(null);
    try {
      await createProject.mutateAsync({
        title: newProjectTitle,
        description: newProjectDescription,
      });
      setNewProjectTitle("");
      setNewProjectDescription("");
      setCreateProjectDialogVisible(false);
    } catch (err) {
      setCreateError(
        err instanceof ApiError ? err.message : "Založení projektu se nezdařilo.",
      );
    }
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
        </header>

        {isLoading && <p className="text-gray-500">Načítání projektů…</p>}
        {error && (
          <p className="text-red-600">
            {error instanceof ApiError ? error.message : "Nepodařilo se načíst projekty."}
          </p>
        )}

        <section style={{ display: "grid", gap: 12 }}>
          {data?.projects.map((p) => (
            <Link
              key={p.id}
              to="/projects/$id"
              params={{ id: String(p.id) }}
              style={{
                border: "1px solid #e5e7eb",
                borderRadius: 8,
                padding: 12,
                display: "block",
                textDecoration: "none",
                color: "inherit",
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
                      color: p.status === 1 ? "#059669" : "#6b7280",
                    }}
                  >
                    {p.status === 1 ? "Active" : "Inactive"}
                  </div>
                  <div style={{ fontSize: 12, color: "#374151" }}>
                    {p.memberCount} members
                  </div>
                </div>
              </div>
            </Link>
          ))}
        </section>
      </div>

      <Dialog
        open={createProjectDialogVisible}
        onOpenChange={setCreateProjectDialogVisible}
      >
        <DialogTitle>Nový projekt</DialogTitle>
        <MiniForm title="Nový projekt" onSubmit={handleCreate}>
          <TextInputBlock
            label="Název"
            placeholder="Project name"
            name="title"
            type="text"
            value={newProjectTitle}
            onChange={setNewProjectTitle}
          />
          <TextAreaInputBlock
            label="Popis"
            placeholder="Project description"
            name="description"
            value={newProjectDescription}
            onChange={setNewProjectDescription}
          />
          {createError && <div className="text-sm text-red-600">{createError}</div>}
          <SubmitInput label="Create new project" />
        </MiniForm>
      </Dialog>
    </>
  );
}
