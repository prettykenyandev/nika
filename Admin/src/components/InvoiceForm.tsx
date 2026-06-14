"use client";

import { useMemo, useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { createInvoiceAction } from "@/actions/invoices";
import { formatMoney } from "@/lib/format";

const inputClass =
  "w-full border border-border-soft bg-surface-2 px-3 py-2 text-sm outline-none focus:border-accent";

interface LineRow {
  description: string;
  quantity: string;
  unitPrice: string;
  taxPercent: string;
}

function emptyLine(defaultTax: number): LineRow {
  return {
    description: "",
    quantity: "1",
    unitPrice: "",
    taxPercent: String(defaultTax),
  };
}

function todayIso(): string {
  return new Date().toISOString().slice(0, 10);
}

function addDaysIso(days: number): string {
  const d = new Date();
  d.setDate(d.getDate() + days);
  return d.toISOString().slice(0, 10);
}

export function InvoiceForm({
  defaultCurrency,
  defaultTaxPercent,
}: {
  defaultCurrency: string;
  defaultTaxPercent: number;
}) {
  const router = useRouter();
  const [customerName, setCustomerName] = useState("");
  const [customerEmail, setCustomerEmail] = useState("");
  const [issueDate, setIssueDate] = useState(todayIso());
  const [dueDate, setDueDate] = useState(addDaysIso(14));
  const [notes, setNotes] = useState("");
  const [lines, setLines] = useState<LineRow[]>([emptyLine(defaultTaxPercent)]);
  const [error, setError] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();

  function updateLine(index: number, field: keyof LineRow, value: string) {
    setLines((prev) =>
      prev.map((l, i) => (i === index ? { ...l, [field]: value } : l)),
    );
  }
  function addLine() {
    setLines((prev) => [...prev, emptyLine(defaultTaxPercent)]);
  }
  function removeLine(index: number) {
    setLines((prev) => prev.filter((_, i) => i !== index));
  }

  const totals = useMemo(() => {
    let net = 0;
    let tax = 0;
    for (const l of lines) {
      const qty = Number(l.quantity) || 0;
      const price = Number(l.unitPrice) || 0;
      const rate = l.taxPercent === "" ? 0 : Number(l.taxPercent) || 0;
      const lineNet = qty * price;
      net += lineNet;
      tax += (lineNet * rate) / 100;
    }
    return { net, tax, total: net + tax };
  }, [lines]);

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    startTransition(async () => {
      const res = await createInvoiceAction({
        customerName,
        customerEmail: customerEmail || null,
        issueDate,
        dueDate,
        currency: defaultCurrency,
        notes,
        lines: lines.map((l) => ({
          description: l.description,
          quantity: Number(l.quantity),
          unitPrice: Number(l.unitPrice),
          taxPercent: l.taxPercent === "" ? null : Number(l.taxPercent),
        })),
      });
      if (res.error || !res.id) {
        setError(res.error ?? "Could not create invoice.");
        return;
      }
      router.push(`/invoices/${res.id}`);
    });
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-6">
      {error ? (
        <p className="border border-danger/40 bg-danger/10 p-4 text-sm text-danger">
          {error}
        </p>
      ) : null}

      {/* Customer & dates */}
      <section className="flex flex-col gap-4 border border-border-soft bg-surface p-4 sm:p-6">
        <h2 className="text-lg font-semibold">Customer &amp; dates</h2>
        <div className="grid gap-4 sm:grid-cols-2">
          <label className="flex flex-col gap-1 text-sm">
            <span className="text-muted">Customer name *</span>
            <input
              value={customerName}
              onChange={(e) => setCustomerName(e.target.value)}
              placeholder="e.g. Jane Doe"
              className={inputClass}
              required
            />
          </label>
          <label className="flex flex-col gap-1 text-sm">
            <span className="text-muted">Customer email</span>
            <input
              type="email"
              value={customerEmail}
              onChange={(e) => setCustomerEmail(e.target.value)}
              placeholder="jane@example.com"
              className={inputClass}
            />
          </label>
          <label className="flex flex-col gap-1 text-sm">
            <span className="text-muted">Issue date *</span>
            <input
              type="date"
              value={issueDate}
              onChange={(e) => setIssueDate(e.target.value)}
              className={inputClass}
              required
            />
          </label>
          <label className="flex flex-col gap-1 text-sm">
            <span className="text-muted">Due date *</span>
            <input
              type="date"
              value={dueDate}
              onChange={(e) => setDueDate(e.target.value)}
              className={inputClass}
              required
            />
          </label>
        </div>
      </section>

      {/* Line items */}
      <section className="flex flex-col gap-4 border border-border-soft bg-surface p-4 sm:p-6">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold">Line items</h2>
          <button
            type="button"
            onClick={addLine}
            className="text-sm text-accent underline"
          >
            + Add line
          </button>
        </div>

        <div className="flex flex-col gap-4">
          {lines.map((l, i) => (
            <div
              key={i}
              className="grid grid-cols-1 gap-3 border border-border-soft bg-surface-2 p-3 sm:grid-cols-2 lg:grid-cols-[1.8fr_0.7fr_0.9fr_0.7fr_auto] lg:items-end"
            >
              <label className="flex flex-col gap-1 text-xs">
                <span className="text-muted">Description</span>
                <input
                  value={l.description}
                  onChange={(e) => updateLine(i, "description", e.target.value)}
                  placeholder="What is being billed"
                  className={inputClass}
                />
              </label>
              <label className="flex flex-col gap-1 text-xs">
                <span className="text-muted">Qty</span>
                <input
                  type="number"
                  min="0"
                  step="0.01"
                  inputMode="decimal"
                  value={l.quantity}
                  onChange={(e) => updateLine(i, "quantity", e.target.value)}
                  className={inputClass}
                />
              </label>
              <label className="flex flex-col gap-1 text-xs">
                <span className="text-muted">Unit price</span>
                <input
                  type="number"
                  min="0"
                  step="0.01"
                  inputMode="decimal"
                  value={l.unitPrice}
                  onChange={(e) => updateLine(i, "unitPrice", e.target.value)}
                  placeholder="0.00"
                  className={inputClass}
                />
              </label>
              <label className="flex flex-col gap-1 text-xs">
                <span className="text-muted">Tax %</span>
                <input
                  type="number"
                  min="0"
                  max="100"
                  step="0.01"
                  inputMode="decimal"
                  value={l.taxPercent}
                  onChange={(e) => updateLine(i, "taxPercent", e.target.value)}
                  className={inputClass}
                />
              </label>
              {lines.length > 1 ? (
                <button
                  type="button"
                  onClick={() => removeLine(i)}
                  className="h-fit border border-border-soft px-3 py-2 text-sm text-muted transition hover:border-danger hover:text-danger"
                >
                  Remove
                </button>
              ) : (
                <span className="hidden lg:block" />
              )}
            </div>
          ))}
        </div>

        <div className="flex flex-col items-end gap-1 border-t border-border-soft pt-3 text-sm">
          <div className="flex w-full max-w-xs justify-between text-muted">
            <span>Subtotal</span>
            <span>{formatMoney(totals.net, defaultCurrency)}</span>
          </div>
          <div className="flex w-full max-w-xs justify-between text-muted">
            <span>Tax</span>
            <span>{formatMoney(totals.tax, defaultCurrency)}</span>
          </div>
          <div className="flex w-full max-w-xs justify-between font-semibold">
            <span>Total</span>
            <span>{formatMoney(totals.total, defaultCurrency)}</span>
          </div>
        </div>
      </section>

      {/* Notes */}
      <section className="flex flex-col gap-4 border border-border-soft bg-surface p-4 sm:p-6">
        <h2 className="text-lg font-semibold">Notes</h2>
        <label className="flex flex-col gap-1 text-sm">
          <span className="text-muted">Notes (shown on the invoice)</span>
          <textarea
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            rows={2}
            className={inputClass}
          />
        </label>
      </section>

      <div className="flex justify-end">
        <button
          type="submit"
          disabled={pending}
          className="bg-accent-strong px-6 py-3 font-semibold text-white transition hover:bg-accent disabled:cursor-not-allowed disabled:opacity-50"
        >
          {pending ? "Saving…" : "Create invoice"}
        </button>
      </div>
    </form>
  );
}
