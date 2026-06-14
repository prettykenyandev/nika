"use client";

import { useMemo, useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { createPurchaseOrderAction } from "@/actions/purchaseOrders";
import { formatMoney } from "@/lib/format";
import type { VariantOptionDto, VendorSummaryDto } from "@/lib/types";

const inputClass =
  "w-full border border-border-soft bg-surface-2 px-3 py-2 text-sm outline-none focus:border-accent";

interface LineRow {
  productVariantId: string;
  description: string;
  sku: string;
  quantity: string;
  unitCost: string;
  taxPercent: string;
}

function emptyLine(defaultTax: number): LineRow {
  return {
    productVariantId: "",
    description: "",
    sku: "",
    quantity: "1",
    unitCost: "",
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

export function PurchaseOrderForm({
  vendors,
  variants,
  defaultCurrency,
  defaultTaxPercent,
}: {
  vendors: VendorSummaryDto[];
  variants: VariantOptionDto[];
  defaultCurrency: string;
  defaultTaxPercent: number;
}) {
  const router = useRouter();
  const [vendorId, setVendorId] = useState("");
  const [orderDate, setOrderDate] = useState(todayIso());
  const [expectedDate, setExpectedDate] = useState(addDaysIso(7));
  const [currency, setCurrency] = useState(defaultCurrency);
  const [notes, setNotes] = useState("");
  const [lines, setLines] = useState<LineRow[]>([emptyLine(defaultTaxPercent)]);
  const [error, setError] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();

  function updateLine(index: number, field: keyof LineRow, value: string) {
    setLines((prev) =>
      prev.map((l, i) => (i === index ? { ...l, [field]: value } : l)),
    );
  }

  function chooseVariant(index: number, variantId: string) {
    const variant = variants.find((v) => v.variantId === variantId);
    setLines((prev) =>
      prev.map((l, i) =>
        i === index
          ? {
              ...l,
              productVariantId: variantId,
              description: variant ? variant.label : l.description,
              sku: variant ? variant.sku : "",
              unitCost: variant ? String(variant.price) : l.unitCost,
            }
          : l,
      ),
    );
    if (variant?.currency) setCurrency(variant.currency);
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
      const cost = Number(l.unitCost) || 0;
      const rate = l.taxPercent === "" ? 0 : Number(l.taxPercent) || 0;
      const lineNet = qty * cost;
      net += lineNet;
      tax += (lineNet * rate) / 100;
    }
    return { net, tax, total: net + tax };
  }, [lines]);

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    startTransition(async () => {
      const res = await createPurchaseOrderAction({
        vendorId,
        orderDate,
        expectedDate: expectedDate || null,
        currency,
        notes,
        lines: lines.map((l) => ({
          productVariantId: l.productVariantId || null,
          description: l.description,
          sku: l.sku || null,
          quantity: Number(l.quantity),
          unitCost: Number(l.unitCost),
          taxPercent: l.taxPercent === "" ? null : Number(l.taxPercent),
        })),
      });
      if (res.error || !res.id) {
        setError(res.error ?? "Could not create purchase order.");
        return;
      }
      router.push(`/purchase-orders/${res.id}`);
    });
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-6">
      {error ? (
        <p className="border border-danger/40 bg-danger/10 p-4 text-sm text-danger">
          {error}
        </p>
      ) : null}

      <section className="flex flex-col gap-4 border border-border-soft bg-surface p-4 sm:p-6">
        <h2 className="text-lg font-semibold">Vendor &amp; dates</h2>
        <div className="grid gap-4 sm:grid-cols-2">
          <label className="flex flex-col gap-1 text-sm">
            <span className="text-muted">Vendor *</span>
            <select
              value={vendorId}
              onChange={(e) => setVendorId(e.target.value)}
              className={inputClass}
              required
            >
              <option value="">Choose a vendor</option>
              {vendors.map((v) => (
                <option key={v.id} value={v.id}>
                  {v.name}
                </option>
              ))}
            </select>
          </label>
          <label className="flex flex-col gap-1 text-sm">
            <span className="text-muted">Currency</span>
            <input
              value={currency}
              onChange={(e) => setCurrency(e.target.value.toUpperCase())}
              className={inputClass}
            />
          </label>
          <label className="flex flex-col gap-1 text-sm">
            <span className="text-muted">Order date *</span>
            <input
              type="date"
              value={orderDate}
              onChange={(e) => setOrderDate(e.target.value)}
              className={inputClass}
              required
            />
          </label>
          <label className="flex flex-col gap-1 text-sm">
            <span className="text-muted">Expected date</span>
            <input
              type="date"
              value={expectedDate}
              onChange={(e) => setExpectedDate(e.target.value)}
              className={inputClass}
            />
          </label>
        </div>
      </section>

      <section className="flex flex-col gap-4 border border-border-soft bg-surface p-4 sm:p-6">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold">Line items</h2>
          <button type="button" onClick={addLine} className="text-sm text-accent underline">
            + Add line
          </button>
        </div>

        <div className="flex flex-col gap-4">
          {lines.map((l, i) => (
            <div
              key={i}
              className="grid grid-cols-1 gap-3 border border-border-soft bg-surface-2 p-3 sm:grid-cols-2 lg:grid-cols-[1.2fr_1.4fr_0.8fr_0.7fr_0.9fr_0.7fr_auto] lg:items-end"
            >
              <label className="flex flex-col gap-1 text-xs">
                <span className="text-muted">Product</span>
                <select
                  value={l.productVariantId}
                  onChange={(e) => chooseVariant(i, e.target.value)}
                  className={inputClass}
                >
                  <option value="">Custom line</option>
                  {variants.map((v) => (
                    <option key={v.variantId} value={v.variantId}>
                      {v.sku} — {v.label} ({v.stockQuantity} on hand)
                    </option>
                  ))}
                </select>
              </label>
              <label className="flex flex-col gap-1 text-xs">
                <span className="text-muted">Description</span>
                <input
                  value={l.description}
                  onChange={(e) => updateLine(i, "description", e.target.value)}
                  placeholder="What is being ordered"
                  className={inputClass}
                />
              </label>
              <label className="flex flex-col gap-1 text-xs">
                <span className="text-muted">SKU</span>
                <input
                  value={l.sku}
                  onChange={(e) => updateLine(i, "sku", e.target.value)}
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
                <span className="text-muted">Unit cost</span>
                <input
                  type="number"
                  min="0"
                  step="0.01"
                  inputMode="decimal"
                  value={l.unitCost}
                  onChange={(e) => updateLine(i, "unitCost", e.target.value)}
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
          <Row label="Subtotal" value={formatMoney(totals.net, currency)} />
          <Row label="Tax" value={formatMoney(totals.tax, currency)} />
          <Row label="Total" value={formatMoney(totals.total, currency)} strong />
        </div>
      </section>

      <section className="flex flex-col gap-4 border border-border-soft bg-surface p-4 sm:p-6">
        <h2 className="text-lg font-semibold">Notes</h2>
        <label className="flex flex-col gap-1 text-sm">
          <span className="text-muted">Internal notes</span>
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
          {pending ? "Saving…" : "Create purchase order"}
        </button>
      </div>
    </form>
  );
}

function Row({ label, value, strong }: { label: string; value: string; strong?: boolean }) {
  return (
    <div className={`flex w-full max-w-xs justify-between ${strong ? "font-semibold" : "text-muted"}`}>
      <span>{label}</span>
      <span>{value}</span>
    </div>
  );
}
