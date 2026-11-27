import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import { describe, expect, it } from "vitest";
import { server } from "../../test/server";
import { renderWithProviders } from "../../test/render";
import {
  ChangePasswordForm,
  CheckYourEmail,
  ForgotPasswordForm,
  ResetPasswordForm,
  VerifyEmailResult,
} from "./account-forms";

const API = "http://localhost:5049/api/v1";

function problem(status: number, errorKey: string, detail = "x") {
  return HttpResponse.json({ status, detail, errorKey, params: [] }, { status });
}

describe("CheckYourEmail", () => {
  it("offers to resend the verification e-mail", async () => {
    const sent: unknown[] = [];
    server.use(
      http.post(`${API}/auth/verify-email/resend`, async ({ request }) => {
        sent.push(await request.json());
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderWithProviders(<CheckYourEmail email="a@example.com" />);

    await userEvent.click(await screen.findByRole("button", { name: "Poslat znovu" }));

    await waitFor(() => expect(sent).toEqual([{ email: "a@example.com" }]));
    expect(await screen.findByRole("status")).toBeInTheDocument();
  });
});

describe("VerifyEmailResult", () => {
  it("verifies exactly once and offers the login", async () => {
    const calls: unknown[] = [];
    server.use(
      http.post(`${API}/auth/verify-email`, async ({ request }) => {
        calls.push(await request.json());
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderWithProviders(<VerifyEmailResult token="abc" pendingJoin={true} />);

    expect(await screen.findByText("E-mail byl ověřen. Můžete se přihlásit.")).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /pokračovat k pozvánce/ })).toBeInTheDocument();
    expect(calls).toEqual([{ token: "abc" }]);
  });

  it("shows a Czech message for an invalid token", async () => {
    server.use(http.post(`${API}/auth/verify-email`, () => problem(400, "INVALID_TOKEN")));
    renderWithProviders(<VerifyEmailResult token="bad" pendingJoin={false} />);

    expect(await screen.findByRole("alert")).toHaveTextContent("neplatný");
  });

  it("does not call the API without a token", async () => {
    renderWithProviders(<VerifyEmailResult token={undefined} pendingJoin={false} />);

    expect(await screen.findByRole("alert")).toHaveTextContent("neplatný");
  });
});

describe("ForgotPasswordForm", () => {
  it("shows the same neutral confirmation", async () => {
    const sent: unknown[] = [];
    server.use(
      http.post(`${API}/auth/password-reset/request`, async ({ request }) => {
        sent.push(await request.json());
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderWithProviders(<ForgotPasswordForm />);

    await userEvent.type(await screen.findByLabelText("E-mail"), "who@example.com");
    await userEvent.click(screen.getByRole("button", { name: "Poslat odkaz" }));

    expect(await screen.findByRole("status")).toHaveTextContent("Pokud účet");
    expect(sent[0]).toMatchObject({ email: "who@example.com" });
  });
});

describe("ResetPasswordForm", () => {
  it("rejects different passwords without calling the API", async () => {
    renderWithProviders(<ResetPasswordForm token="t" />);

    await userEvent.type(await screen.findByLabelText("Nové heslo"), "Password-123");
    await userEvent.type(screen.getByLabelText("Nové heslo znovu"), "Password-124");
    await userEvent.click(screen.getByRole("button", { name: "Nastavit heslo" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("neshodují");
  });

  it("sets the new password with the token", async () => {
    const calls: unknown[] = [];
    server.use(
      http.post(`${API}/auth/password-reset/confirm`, async ({ request }) => {
        calls.push(await request.json());
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderWithProviders(<ResetPasswordForm token="tok" />);

    await userEvent.type(await screen.findByLabelText("Nové heslo"), "Password-123");
    await userEvent.type(screen.getByLabelText("Nové heslo znovu"), "Password-123");
    await userEvent.click(screen.getByRole("button", { name: "Nastavit heslo" }));

    expect(await screen.findByRole("status")).toHaveTextContent("Heslo bylo změněno");
    expect(calls).toEqual([{ token: "tok", newPassword: "Password-123" }]);
  });

  it("shows the invalid-token error", async () => {
    server.use(http.post(`${API}/auth/password-reset/confirm`, () => problem(400, "INVALID_TOKEN")));
    renderWithProviders(<ResetPasswordForm token="old" />);

    await userEvent.type(await screen.findByLabelText("Nové heslo"), "Password-123");
    await userEvent.type(screen.getByLabelText("Nové heslo znovu"), "Password-123");
    await userEvent.click(screen.getByRole("button", { name: "Nastavit heslo" }));

    expect(await screen.findByRole("alert")).toHaveTextContent("vypršel");
  });
});

describe("ChangePasswordForm", () => {
  async function fill(current: string) {
    await userEvent.type(await screen.findByLabelText("Současné heslo"), current);
    await userEvent.type(screen.getByLabelText("Nové heslo"), "Password-123");
    await userEvent.type(screen.getByLabelText("Nové heslo znovu"), "Password-123");
    await userEvent.click(screen.getByRole("button", { name: "Změnit heslo" }));
  }

  it("changes the password", async () => {
    const calls: unknown[] = [];
    server.use(
      http.post(`${API}/auth/password`, async ({ request }) => {
        calls.push(await request.json());
        return new HttpResponse(null, { status: 204 });
      }),
    );
    renderWithProviders(<ChangePasswordForm />);

    await fill("Old-password-1");

    expect(await screen.findByRole("status")).toHaveTextContent("Heslo bylo změněno");
    expect(calls).toEqual([{ currentPassword: "Old-password-1", newPassword: "Password-123" }]);
  });

  it("reports a wrong current password", async () => {
    server.use(http.post(`${API}/auth/password`, () => problem(400, "GENERIC_BAD_REQUEST")));
    renderWithProviders(<ChangePasswordForm />);

    await fill("wrong");

    expect(await screen.findByRole("alert")).toHaveTextContent("Současné heslo není správné");
  });
});
