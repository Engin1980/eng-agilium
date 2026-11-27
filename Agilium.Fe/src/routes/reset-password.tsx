import { createFileRoute } from "@tanstack/react-router";
import { CardPage } from "../components/global/card-page";
import { ResetPasswordForm } from "../components/specific/account-forms";

export const Route = createFileRoute("/reset-password")({
  validateSearch: (search: Record<string, unknown>): { token?: string } => ({
    token: typeof search.token === "string" ? search.token : undefined,
  }),
  component: RouteComponent,
});

function RouteComponent() {
  const { token } = Route.useSearch();
  return (
    <CardPage title="Nové heslo">
      <ResetPasswordForm token={token} />
    </CardPage>
  );
}
