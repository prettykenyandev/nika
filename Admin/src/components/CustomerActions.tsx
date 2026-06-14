"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { backfillCustomersAction, setCustomerActiveAction } from "@/actions/customers";

export function CustomerActiveButton({
  customerId,
  isActive,
}: {
  customerId: string;
  isActive: boolean;
}) {
  const router = useRouter();
  const [error, setError] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();

  function toggle() {
    setError(null);
    startTransition(async () => {
      const res = await setCustomerActiveAction(customerId, !isActive);
      if (res.error) setError(res.error);
      else router.refresh();
    });
  }

  return (
    <div className="flex flex-col gap-2">
      {error ? <p className="text-sm text-danger">{error}</p> : null}
      <button
        type="button"
        onClick={toggle}
        disabled={pending}
        className="border border-border-soft px-4 py-2 text-sm text-muted transition hover:border-accent hover:text-accent disabled:opacity-50"
      >
        {pending ? "Working…" : isActive ? "Deactivate" : "Activate"}
      </button>
    </div>
  );
}

export function CustomerBackfillButton() {
  const router = useRouter();
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();

  function backfill() {
    setMessage(null);
    setError(null);
    startTransition(async () => {
      const res = await backfillCustomersAction();
      if (res.error) {
        setError(res.error);
        return;
      }
      setMessage(`Backfill complete${res.count !== undefined ? `: ${res.count} customer(s)` : ""}.`);
      router.refresh();
    });
  }

  return (
    <div className="flex flex-col items-start gap-2">
      <button
        type="button"
        onClick={backfill}
        disabled={pending}
        className="border border-border-soft px-4 py-2 text-sm text-muted transition hover:border-accent hover:text-accent disabled:opacity-50"
      >
        {pending ? "Backfilling…" : "Backfill from orders"}
      </button>
      {message ? <p className="text-xs text-success">{message}</p> : null}
      {error ? <p className="text-xs text-danger">{error}</p> : null}
    </div>
  );
}
