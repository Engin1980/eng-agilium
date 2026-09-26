import { createFileRoute, Link, useNavigate } from "@tanstack/react-router";
import * as React from "react";
import { ItemType } from "../../services/items-api";
import { useGlobalTemplate, useUpdateGlobalTemplate } from "../../services/templates-queries";
import { TemplateFieldsEditor } from "../../components/specific/template-fields-editor";
import { useAuth } from "../../contexts/auth-context";

export const Route = createFileRoute("/templates/")({
  component: RouteComponent,
});

const TYPE_TABS: { type: ItemType; label: string }[] = [
  { type: ItemType.Feature, label: "Feature" },
  { type: ItemType.UserStory, label: "User Story" },
  { type: ItemType.Task, label: "Task" },
  { type: ItemType.Bug, label: "Bug" },
];

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

  return <GlobalTemplatesPage />;
}

function GlobalTemplatesPage() {
  const [itemType, setItemType] = React.useState<ItemType>(ItemType.Feature);

  return (
    <div className="p-6">
      <Link to="/projects" className="text-sm text-blue-600 hover:underline">
        ← Zpět na projekty
      </Link>

      <h1 className="mt-4 text-2xl font-semibold">Globální výchozí šablony</h1>
      <p className="mt-1 text-sm text-gray-500">
        Nové projekty si při založení vezmou kopii těchto šablon. Úprava zde nemění šablony již
        existujících projektů.
      </p>

      <div className="mt-4 flex gap-2 border-b border-gray-200">
        {TYPE_TABS.map((tab) => (
          <button
            key={tab.type}
            onClick={() => setItemType(tab.type)}
            className={`px-3 py-2 text-sm font-medium ${
              itemType === tab.type
                ? "border-b-2 border-blue-600 text-blue-600"
                : "text-gray-500 hover:text-gray-700"
            }`}
          >
            {tab.label}
          </button>
        ))}
      </div>

      <div className="mt-4">
        <GlobalTemplateEditor itemType={itemType} />
      </div>
    </div>
  );
}

function GlobalTemplateEditor({ itemType }: { itemType: ItemType }) {
  const { data, isLoading, error } = useGlobalTemplate(itemType);
  const updateTemplate = useUpdateGlobalTemplate(itemType);

  if (isLoading) return <p className="text-gray-500">Načítání šablony…</p>;
  if (error || !data) return <p className="text-red-600">Nepodařilo se načíst šablonu.</p>;

  return (
    <TemplateFieldsEditor
      template={data}
      isSaving={updateTemplate.isPending}
      saveError={updateTemplate.error}
      onSave={(columnCount, items) => updateTemplate.mutate({ columnCount, items })}
    />
  );
}
