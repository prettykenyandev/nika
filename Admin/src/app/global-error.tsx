"use client";

import { useEffect } from "react";

/**
 * Catches errors thrown in the root layout itself (where the normal error
 * boundary cannot render). Must include its own <html>/<body>.
 */
export default function GlobalError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    console.error(error);
  }, [error]);

  return (
    <html lang="en">
      <body className="min-h-screen">
        <div className="mx-auto flex max-w-md flex-col items-center gap-4 py-16 text-center">
          <h1 className="text-2xl font-bold">Something went wrong</h1>
          <p className="text-sm text-muted">
            The application hit an unexpected error. Please try again.
          </p>
          <button
            type="button"
            onClick={reset}
            className="bg-accent-strong px-4 py-2 text-sm font-semibold text-white transition hover:bg-accent"
          >
            Try again
          </button>
        </div>
      </body>
    </html>
  );
}
