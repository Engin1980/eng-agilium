import type * as React from "react";

/** Centered white card on a gray page (login-like screens). */
export function CardPage({ title, children }: { title: string; children: React.ReactNode }) {
  return (
    <div className="flex min-h-screen items-center justify-center bg-gray-50 px-4">
      <div className="w-full max-w-md rounded-lg bg-white p-8 shadow-lg">
        <h1 className="mb-6 text-2xl font-semibold text-gray-800">{title}</h1>
        {children}
      </div>
    </div>
  );
}
