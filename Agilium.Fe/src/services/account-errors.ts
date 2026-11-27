import { ApiError } from "./http-client";

/** Czech texts for the errors of the account endpoints (the server messages are English). */
export function accountErrorText(error: unknown, fallback = "Operace se nezdařila."): string {
  if (!(error instanceof ApiError)) return fallback;
  switch (error.errorKey) {
    case "EMAIL_NOT_VERIFIED":
      return "E-mail zatím nebyl ověřen. Klikněte na odkaz v e-mailu, který jsme vám poslali.";
    case "INVALID_TOKEN":
      return "Odkaz je neplatný, už byl použit, nebo vypršel.";
    case "TOO_MANY_REQUESTS":
      return "Příliš mnoho požadavků, zkuste to prosím za chvíli.";
    default:
      return error.message || fallback;
  }
}
