"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import {
  approveBillAction,
  cancelBillAction,
  getBillPdfAction,
  recordBillPaymentAction,
} from "@/actions/expenses";

const inputClass =
  "w-full border border-border-soft bg-surface-2 px-3 py-2 text-sm outline-none focus:border-accent";

function todayIso(): string {
  return new Date().toISOString().slice(0, 10);
}

function downloadBase64Pdf(fileName: string, base64: string) {
  const bytes = Uint8Array.from(atob(base64), (c) => c.charCodeAt(0));
  const blob = new Blob([bytes], { type: "application/pdf" });
  const url = URL.createObjectURL(blob);
  const link = document.createElement("a");
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}

export function BillActions({
  billId,
  status,
  amountDue,
  currency,
}: {
  billId: string;
  status: string;
  amountDue: number;
  currency: string;
}) {
  const router = useRouter();
  const [error, setError] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();

  // Payment form
  const [amount, setAmount] = useState(amountDue > 0 ? String(amountDue) : "");
  const [paidOn, setPaidOn] = useState(todayIso());
  const [method, setMethod] = useState("Mpesa");
  const [reference, setReference] = useState("");

  const canApprove = status === "Draft";
  const canPay = status === "AwaitingPayment" || status === "PartiallyPaid";
  const canCancel = status !== "Paid" && status !== "Cancelled";

  function approve() {
    setError(null);
    startTransition(async () => {
      const res = await approveBillAction(billId);
      if (res.error) setError(res.error);
      else router.refresh();
    });
  }

  function cancel() {
    setError(null);
    startTransition(async () => {
      const res = await cancelBillAction(billId);
      if (res.error) setError(res.error);
      else router.refresh();
    });
  }

  function downloadPdf() {
    setError(null);
    startTransition(async () => {
      const res = await getBillPdfAction(billId);
      if (res.error || !res.base64 || !res.fileName) {
        setError(res.error ?? "Could not generate the PDF.");
        return;
      }
      downloadBase64Pdf(res.fileName, res.base64);
    });
  }

  function recordPayment(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    startTransition(async () => {
      const res = await recordBillPaymentAction(billId, {
        amount: Number(amount),
        paidOn,
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
        {canApprove ? (
          <button
            type="button"
            onClick={approve}
            disabled={pending}
            className="bg-accent-strong px-4 py-2 text-sm font-semibold text-white transition hover:bg-accent disabled:opacity-50"
          >
            {pending ? "Working…" : "Approve for payment"}
          </button>
        ) : null}
        <button
          type="button"
          onClick={downloadPdf}
          disabled={pending}
          className="border border-border-soft px-4 py-2 text-sm text-muted transition hover:border-accent hover:text-accent disabled:opacity-50"
        >
          Download PDF
        </button>
        {canCancel ? (
          <button
            type="button"
            onClick={cancel}
            disabled={pending}
            className="border border-border-soft px-4 py-2 text-sm text-muted transition hover:border-danger hover:text-danger disabled:opacity-50"
          >
            Cancel bill
          </button>
        ) : null}
      </div>

      {canPay ? (
        <form
          onSubmit={recordPayment}
          className="flex flex-col gap-3 border border-border-soft bg-surface-2 p-4"
        >
          <h3 className="text-sm font-semibold">Record a payment</h3>
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
              <span className="text-muted">Paid on</span>
              <input
                type="date"
                value={paidOn}
                onChange={(e) => setPaidOn(e.target.value)}
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
              {pending ? "Saving…" : "Record payment"}
            </button>
          </div>
        </form>
      ) : null}
    </div>
  );
}
