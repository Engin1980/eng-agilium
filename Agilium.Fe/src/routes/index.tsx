import { createFileRoute, Link } from "@tanstack/react-router";

export const Route = createFileRoute("/")({
  component: RouteComponent,
});

function RouteComponent() {
  return (
    <div className="min-h-screen bg-gray-50">
      <div className="mx-auto flex min-h-screen max-w-3xl flex-col items-center justify-center px-6 text-center">
        <span className="mb-4 rounded-full bg-blue-100 px-3 py-1 text-sm font-medium text-blue-700">
          Agilium
        </span>
        <h1 className="text-4xl font-bold tracking-tight text-gray-900 sm:text-5xl">
          Plánujte a sledujte svou práci na jednom místě
        </h1>
        <p className="mt-4 max-w-xl text-lg text-gray-600">
          Agilium je jednoduchý nástroj pro agilní řízení projektů — features,
          user-stories a úkoly organizované do sprintů a přehledné kanban
          nástěnky.
        </p>

        <div className="mt-8 flex flex-wrap items-center justify-center gap-4">
          <Link
            to="/projects"
            className="rounded-lg bg-blue-600 px-6 py-3 font-semibold text-white shadow-sm transition hover:bg-blue-700"
          >
            Zobrazit projekty
          </Link>
        </div>

        <div className="mt-16 grid w-full grid-cols-1 gap-6 text-left sm:grid-cols-3">
          <FeatureCard
            title="Projekty a týmy"
            description="Spravujte projekty a členy, kteří na nich spolupracují."
          />
          <FeatureCard
            title="Kanban nástěnka"
            description="Sledujte stav features, user-stories a úkolů podle sprintů."
          />
          <FeatureCard
            title="Vlastní šablony"
            description="Přizpůsobte si pole a strukturu položek podle potřeb projektu."
          />
        </div>
      </div>
    </div>
  );
}

function FeatureCard({
  title,
  description,
}: {
  title: string;
  description: string;
}) {
  return (
    <div className="rounded-lg border border-gray-200 bg-white p-5 shadow-sm">
      <h3 className="font-semibold text-gray-900">{title}</h3>
      <p className="mt-1 text-sm text-gray-600">{description}</p>
    </div>
  );
}
