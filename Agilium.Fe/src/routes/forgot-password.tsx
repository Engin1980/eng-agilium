import { createFileRoute, Link } from "@tanstack/react-router";
import { CardPage } from "../components/global/card-page";
import { ForgotPasswordForm } from "../components/specific/account-forms";

export const Route = createFileRoute("/forgot-password")({
  component: () => (
    <CardPage title="Zapomenuté heslo">
      <ForgotPasswordForm />
      <p className="mt-4 text-center text-sm text-gray-600">
        <Link to="/login" className="text-blue-600 hover:underline">
          Zpět na přihlášení
        </Link>
      </p>
    </CardPage>
  ),
});
