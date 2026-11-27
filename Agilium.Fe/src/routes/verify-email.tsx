import { createFileRoute } from "@tanstack/react-router";
import { CardPage } from "../components/global/card-page";
import { VerifyEmailResult } from "../components/specific/account-forms";

export const Route = createFileRoute("/verify-email")({
  validateSearch: (search: Record<string, unknown>): { token?: string } => ({
    token: typeof search.token === "string" ? search.token : undefined,
  }),
  component: RouteComponent,
});

function RouteComponent() {
  const { token } = Route.useSearch();
  return (
    <CardPage title="Ověření e-mailu">
      <VerifyEmailResult token={token} pendingJoin={!!sessionStorage.getItem("agilium.pendingJoinKey")} />
    </CardPage>
  );
}
