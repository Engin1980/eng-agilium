import { createFileRoute, useParams } from "@tanstack/react-router";
import React from "react";

export const Route = createFileRoute("/projects/$id/")({
  component: RouteComponent,
});

function RouteComponent() {
  const { id } = useParams({ from: Route.id });

  return (
    <div className="p-6">
      <header>
        <h1 className="text-2xl font-semibold">{id}</h1>
      </header>
      <main className="mt-4">
        {/* Přehled projektu - obsah doplním později */}
      </main>
    </div>
  );
}
