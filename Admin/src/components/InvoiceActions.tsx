"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import {
  recordInvoicePaymentAction,
  sendInvoiceAction,
  voidInvoiceAction,
} from "@/actions/invoices";

const inputClass =
  "w-full border border-border-soft bg-surface-2 px-3 py-2 text-sm outline-none focus:border-accent";

function todayIso(): string {
  return new Date().toISOString().slice(0, 10);
}

export function InvoiceActions({
  invoiceId,
  status,
  amountDue,
  currency,
}: {
  invoiceId: string;
  status: string;
  amountDue: number;
  currency: string;
}) {
  const router = useRouter();
  const [error, setError] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();

  const [amount, setAmount] = useState(amountDue > 0 ? String(amountDue) : "");
  const [receivedOn, setReceivedOn] = useState(todayIso());
  const [method, setMethod] = useState("Mpesa");
  const [reference, setReference] = useState("");

  const canSend = status === "Draft";
  const canPay = status === "Sent" || status === "PartiallyPaid";
  const canVoid = status !== "Paid" && status !== "Void";

  function send() {
    setError(null);
    startTransition(async () => {
      const res = await sendInvoiceAction(invoiceId);
      if (res.error) setError(res.error);
      else router.refresh();
    });
  }

  function voidInvoice() {
    setError(null);
    startTransition(async () => {
      const res = await voidInvoiceAction(invoiceId);
      if (res.error) setError(res.error);
      else router.refresh();
    });
  }

  function recordPayment(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    startTransition(async () => {
      const res = await recordInvoicePaymentAction(invoiceId, {
        amount: Number(amount),
        receivedOn,
        method,
        reference: reference || null,
      });
      if (res.error) {
        setError(res.error);
        return;
      }
      setReference("");
      router.refresh();
    });
  }

  return (
    <div className="flex flex-col gap-4">
      {error ? (
        <p className="border border-danger/40 bg-danger/10 p-3 text-sm text-danger">
          {error}
        </p>
      ) : null}

      <div className="flex flex-wrap gap-2">
        {canSend ? (
          <button
            type="button"
            onClick={send}
            disabled={pending}
            className="bg-accent-strong px-4 py-2 text-sm font-semibold text-white transition hover:bg-accent disabled:opacity-50"
          >
            {pending ? "Working…" : "Send to customer"}
          </button>
        ) : null}
        {canVoid ? (
          <button
            type="button"
            onClick={voidInvoice}
            disabled={pending}
            className="border border-border-soft px-4 py-2 text-sm text-muted transition hover:border-danger hover:text-danger disabled:opacity-50"
          >
            Void invoice
          </button>
        ) : null}
      </div>

      {canPay ? (
        <form
          onSubmit={recordPayment}
          className="flex flex-col gap-3 border border-border-soft bg-surface-2 p-4"
        >
          <h3 className="text-sm font-semibold">Record a receipt</h3>
          <div className="grid gap-3 sm:grid-cols-2">
            <label className="flex flex-col gap-1 text-xs">
              <span className="text-muted">Amount ({currency})</span>
              <input
                type="number"
                min="0"
                step="0.01"
                inputMode="decimal"
                value={amount}
                onChange={(e) => setAmount(e.target.value)}
                className={inputClass}
                required
              />
            </label>
            <label className="flex flex-col gap-1 text-xs">
              <span className="text-muted">Received on</span>
              <input
                type="date"
                value={receivedOn}
                onChange={(e) => setReceivedOn(e.target.value)}
                className={inputClass}
                required
              />
            </label>
            <label className="flex flex-col gap-1 text-xs">
              <span className="text-muted">Method</span>
              <select
                value={method}
                onChange={(e) => setMethod(e.target.value)}
                className={inputClass}
              >
                <option>Mpesa</option>
                <option>Bank transfer</option>
                <option>Cash</option>
                <option>Card</option>
                <option>Cheque</option>
              </select>
            </label>
            <label className="flex flex-col gap-1 text-xs">
              <span className="text-muted">Reference</span>
              <input
                value={reference}
                onChange={(e) => setReference(e.target.value)}
                placeholder="Transaction code"
                className={inputClass}
              />
            </label>
          </div>
          <div>
            <button
              type="submit"
              disabled={pending}
              className="bg-accent-strong px-4 py-2 text-sm font-semibold text-white transition hover:bg-accent disabled:opacity-50"
            >
              {pending ? "Saving…" : "Record receipt"}
            </button>
          </div>
        </form>
      ) : null}
    </div>
  );
}
