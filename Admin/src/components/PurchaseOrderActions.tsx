"use client";

import Link from "next/link";
import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import {
  cancelPurchaseOrderAction,
  closePurchaseOrderAction,
  receivePurchaseOrderAction,
  sendPurchaseOrderAction,
} from "@/actions/purchaseOrders";
import type { PurchaseOrderLineDto } from "@/lib/types";

const inputClass =
  "w-full border border-border-soft bg-surface-2 px-3 py-2 text-sm outline-none focus:border-accent";

export function PurchaseOrderActions({
  orderId,
  status,
  lines,
}: {
  orderId: string;
  status: string;
  currency: string;
  lines: PurchaseOrderLineDto[];
}) {
  const router = useRouter();
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [billId, setBillId] = useState<string | null>(null);
  const [generateBill, setGenerateBill] = useState(true);
  const [receipts, setReceipts] = useState<Record<string, string>>(() =>
    Object.fromEntries(
      lines
        .filter((l) => l.quantityOutstanding > 0)
        .map((l) => [l.id, String(l.quantityOutstanding)]),
    ),
  );
  const [pending, startTransition] = useTransition();

  const canSend = status === "Draft";
  const canCancel = status === "Draft";
  const canReceive = status === "Sent" || status === "PartiallyReceived";
  const canClose = status === "Received" || status === "PartiallyReceived";
  const receivableLines = lines.filter((l) => l.quantityOutstanding > 0);

  function run(action: () => Promise<{ error?: string }>) {
    setError(null);
    setNotice(null);
    setBillId(null);
    startTransition(async () => {
      const res = await action();
      if (res.error) setError(res.error);
      else router.refresh();
    });
  }

  function receive(e: React.FormEvent) {
    e.preventDefault();
    run(() =>
      receivePurchaseOrderAction(
        orderId,
        receivableLines
          .map((line) => ({
            lineId: line.id,
            quantity: Number(receipts[line.id]) || 0,
          }))
          .filter((r) => r.quantity > 0),
      ),
    );
  }

  function close(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    setNotice(null);
    setBillId(null);
    startTransition(async () => {
      const res = await closePurchaseOrderAction(orderId, generateBill);
      if (res.error) {
        setError(res.error);
        return;
      }
      if (res.billId) {
        setBillId(res.billId);
      } else {
        setNotice("Purchase order closed.");
      }
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
      {notice ? (
        <p className="border border-accent/40 bg-accent/10 p-3 text-sm text-accent">
          {notice}
        </p>
      ) : null}
      {billId ? (
        <p className="border border-accent/40 bg-accent/10 p-3 text-sm text-accent">
          Purchase order closed.{" "}
          <Link href={`/expenses/${billId}`} className="underline">
            View generated bill
          </Link>
          .
        </p>
      ) : null}

      <div className="flex flex-wrap gap-2">
        {canSend ? (
          <button
            type="button"
            onClick={() => run(() => sendPurchaseOrderAction(orderId))}
            disabled={pending}
            className="bg-accent-strong px-4 py-2 text-sm font-semibold text-white transition hover:bg-accent disabled:opacity-50"
          >
            {pending ? "Working…" : "Send to vendor"}
          </button>
        ) : null}
        {canCancel ? (
          <button
            type="button"
            onClick={() => run(() => cancelPurchaseOrderAction(orderId))}
            disabled={pending}
            className="border border-border-soft px-4 py-2 text-sm text-muted transition hover:border-danger hover:text-danger disabled:opacity-50"
          >
            Cancel
          </button>
        ) : null}
      </div>

      {canReceive && receivableLines.length > 0 ? (
        <form
          onSubmit={receive}
          className="flex flex-col gap-3 border border-border-soft bg-surface-2 p-4"
        >
          <h3 className="text-sm font-semibold">Receive goods</h3>
          {receivableLines.map((line) => (
            <label key={line.id} className="grid gap-2 text-xs sm:grid-cols-[1fr_7rem] sm:items-center">
              <span>
                {line.description}
                <span className="block text-muted">
                  Outstanding: {line.quantityOutstanding}
                </span>
              </span>
              <input
                type="number"
                min="0"
                max={line.quantityOutstanding}
                step="0.01"
                inputMode="decimal"
                value={receipts[line.id] ?? ""}
                onChange={(e) =>
                  setReceipts((prev) => ({ ...prev, [line.id]: e.target.value }))
                }
                className={inputClass}
              />
            </label>
          ))}
          <div>
            <button
              type="submit"
              disabled={pending}
              className="bg-accent-strong px-4 py-2 text-sm font-semibold text-white transition hover:bg-accent disabled:opacity-50"
            >
              {pending ? "Saving…" : "Receive goods"}
            </button>
          </div>
        </form>
      ) : null}

      {canClose ? (
        <form
          onSubmit={close}
          className="flex flex-col gap-3 border border-border-soft bg-surface-2 p-4"
        >
          <h3 className="text-sm font-semibold">Close purchase order</h3>
          <label className="flex items-center gap-2 text-sm">
            <input
              type="checkbox"
              checked={generateBill}
              onChange={(e) => setGenerateBill(e.target.checked)}
            />
            <span>Generate a bill for received goods</span>
          </label>
          <div>
            <button
              type="submit"
              disabled={pending}
              className="bg-accent-strong px-4 py-2 text-sm font-semibold text-white transition hover:bg-accent disabled:opacity-50"
            >
              {pending ? "Closing…" : "Close purchase order"}
            </button>
          </div>
        </form>
      ) : null}
    </div>
  );
}
