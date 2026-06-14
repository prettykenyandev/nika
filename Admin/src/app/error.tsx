"use client";

import { useEffect } from "react";
import Link from "next/link";

export default function AppError({
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
    <div className="mx-auto flex max-w-md flex-col items-center gap-4 py-16 text-center">
      <h1 className="text-2xl font-bold">Something went wrong</h1>
      <p className="text-sm text-muted">
        An unexpected error occurred while loading this page. This is usually
        temporary — try again, or head back to the dashboard.
      </p>
      {error.digest ? (
        <p className="text-xs text-muted">Reference: {error.digest}</p>
      ) : null}
      <div className="flex gap-2">
        <button
          type="button"
          onClick={reset}
          className="bg-accent-strong px-4 py-2 text-sm font-semibold text-white transition hover:bg-accent"
        >
          Try again
        </button>
        <Link
          href="/"
          className="border border-border-soft px-4 py-2 text-sm text-muted transition hover:border-accent hover:text-accent"
        >
          Go to dashboard
        </Link>
      </div>
    </div>
  );
}
