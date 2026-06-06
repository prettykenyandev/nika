"use client";

import { useRef, useState, useTransition } from "react";
import {
  createPosSaleAction,
  lookupSkuAction,
} from "@/actions/pos";
import type { PosSaleResult, PosVariant } from "@/lib/types";

interface SaleLine {
  variant: PosVariant;
  quantity: number;
}

function formatMoney(amount: number, currency: string): string {
  const symbol = currency === "KES" ? "Ksh" : currency;
  return `${symbol} ${amount.toLocaleString(undefined, {
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
  })}`;
}

export default function PosTerminal() {
  const [lines, setLines] = useState<SaleLine[]>([]);
  const [sku, setSku] = useState("");
  const [cash, setCash] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [notice, setNotice] = useState<string | null>(null);
  const [receipt, setReceipt] = useState<PosSaleResult | null>(null);
  const [pending, startTransition] = useTransition();
  const skuRef = useRef<HTMLInputElement>(null);

  const currency = lines[0]?.variant.currency ?? "KES";
  const total = lines.reduce((sum, l) => sum + l.variant.price * l.quantity, 0);
  const itemCount = lines.reduce((sum, l) => sum + l.quantity, 0);
  const cashValue = cash.trim() === "" ? null : Number(cash);
  const change =
    cashValue !== null && Number.isFinite(cashValue) && cashValue >= total
      ? cashValue - total
      : null;

  function focusSku() {
    requestAnimationFrame(() => skuRef.current?.focus());
  }

  function addBySku(raw: string) {
    const value = raw.trim();
    if (!value) return;
    setError(null);
    setNotice(null);

    startTransition(async () => {
      const res = await lookupSkuAction(value);
      if (res.error || !res.variant) {
        setError(res.error ?? "Not found.");
        focusSku();
        return;
      }

      const v = res.variant;
      setLines((prev) => {
        const existing = prev.find((l) => l.variant.variantId === v.variantId);
        if (existing) {
          if (existing.quantity >= v.stockQuantity) {
            setError(`Only ${v.stockQuantity} of ${v.sku} in stock.`);
            return prev;
          }
          return prev.map((l) =>
            l.variant.variantId === v.variantId
              ? { ...l, quantity: l.quantity + 1 }
              : l,
          );
        }
        if (v.stockQuantity <= 0) {
          setError(`${v.sku} is out of stock.`);
          return prev;
        }
        return [...prev, { variant: v, quantity: 1 }];
      });
      setSku("");
      setNotice(`Added ${v.productName} (${v.sku}).`);
      focusSku();
    });
  }

  function changeQty(variantId: string, delta: number) {
    setError(null);
    setLines((prev) =>
      prev.flatMap((l) => {
        if (l.variant.variantId !== variantId) return [l];
        const next = l.quantity + delta;
        if (next <= 0) return [];
        if (next > l.variant.stockQuantity) {
          setError(`Only ${l.variant.stockQuantity} of ${l.variant.sku} in stock.`);
          return [l];
        }
        return [{ ...l, quantity: next }];
      }),
    );
  }

  function removeLine(variantId: string) {
    setLines((prev) => prev.filter((l) => l.variant.variantId !== variantId));
  }

  function resetSale() {
    setLines([]);
    setSku("");
    setCash("");
    setError(null);
    setNotice(null);
    setReceipt(null);
    focusSku();
  }

  function completeSale() {
    if (!lines.length) {
      setError("Add at least one item.");
      return;
    }
    if (cashValue !== null && (!Number.isFinite(cashValue) || cashValue < total)) {
      setError(`Cash tendered must be at least ${formatMoney(total, currency)}.`);
      return;
    }
    setError(null);

    startTransition(async () => {
      const res = await createPosSaleAction({
        items: lines.map((l) => ({
          productVariantId: l.variant.variantId,
          quantity: l.quantity,
        })),
        cashTendered: cashValue,
      });
      if (res.error || !res.result) {
        setError(res.error ?? "Sale failed.");
        return;
      }
      setReceipt(res.result);
    });
  }

  // ---- Receipt view (sale completed) ----
  if (receipt) {
    return (
      <div className="mx-auto max-w-md border border-success/40 bg-success/10 p-6">
        <div className="text-center">
          <p className="text-sm font-semibold uppercase tracking-wide text-success">
            Paid
          </p>
          <h2 className="mt-1 text-2xl font-bold">Sale complete</h2>
          <p className="mt-1 text-sm text-muted">
            Order {receipt.orderNumber}
          </p>
        </div>

        <dl className="mt-6 space-y-2 text-sm">
          <div className="flex justify-between">
            <dt className="text-muted">Total</dt>
            <dd className="font-semibold">
              {formatMoney(receipt.total, receipt.currency)}
            </dd>
          </div>
          {receipt.amountTendered !== null ? (
            <div className="flex justify-between">
              <dt className="text-muted">Cash tendered</dt>
              <dd>{formatMoney(receipt.amountTendered, receipt.currency)}</dd>
            </div>
          ) : null}
          {receipt.change !== null ? (
            <div className="flex justify-between border-t border-border-soft pt-2 text-base">
              <dt className="font-semibold">Change due</dt>
              <dd className="font-bold text-success">
                {formatMoney(receipt.change, receipt.currency)}
              </dd>
            </div>
          ) : null}
        </dl>

        <button
          type="button"
          onClick={resetSale}
          className="mt-6 w-full bg-accent-strong px-4 py-3 font-semibold text-white transition hover:bg-accent"
        >
          New sale
        </button>
      </div>
    );
  }

  // ---- Register view ----
  return (
    <div className="grid gap-6 lg:grid-cols-[1fr_22rem]">
      {/* Left: scan + cart */}
      <div className="flex flex-col gap-4">
        <form
          onSubmit={(e) => {
            e.preventDefault();
            addBySku(sku);
          }}
          className="flex flex-col gap-2 sm:flex-row"
        >
          <input
            ref={skuRef}
            autoFocus
            value={sku}
            onChange={(e) => setSku(e.target.value)}
            placeholder="Scan or type a SKU, then Enter"
            className="flex-1 border border-border-soft bg-surface px-4 py-3 text-base outline-none focus:border-accent"
            aria-label="SKU"
          />
          <button
            type="submit"
            disabled={pending}
            className="bg-accent-strong px-5 py-3 font-semibold text-white transition hover:bg-accent disabled:opacity-60"
          >
            {pending ? "…" : "Add"}
          </button>
        </form>

        {error ? (
          <p className="border border-danger/40 bg-danger/10 px-4 py-2 text-sm text-danger">
            {error}
          </p>
        ) : notice ? (
          <p className="text-sm text-muted">{notice}</p>
        ) : null}

        {lines.length === 0 ? (
          <div className="border border-dashed border-border-soft bg-surface p-10 text-center text-sm text-muted">
            No items yet. Scan a product SKU to start a sale.
          </div>
        ) : (
          <ul className="flex flex-col gap-2">
            {lines.map((l) => (
              <li
                key={l.variant.variantId}
                className="flex items-center gap-3 border border-border-soft bg-surface p-3"
              >
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img
                  src={l.variant.imageUrl ?? "/placeholder.png"}
                  alt=""
                  className="h-14 w-14 flex-shrink-0 object-cover"
                  onError={(e) => {
                    (e.currentTarget as HTMLImageElement).style.visibility =
                      "hidden";
                  }}
                />
                <div className="min-w-0 flex-1">
                  <p className="truncate font-semibold">
                    {l.variant.productName}
                  </p>
                  <p className="truncate text-xs text-muted">
                    {l.variant.variantName} · {l.variant.sku}
                  </p>
                  <p className="text-sm">
                    {formatMoney(l.variant.price, l.variant.currency)}
                  </p>
                </div>
                <div className="flex items-center gap-1">
                  <button
                    type="button"
                    onClick={() => changeQty(l.variant.variantId, -1)}
                    className="h-8 w-8 border border-border-soft text-lg leading-none transition hover:border-accent"
                    aria-label="Decrease quantity"
                  >
                    −
                  </button>
                  <span className="w-8 text-center font-semibold">
                    {l.quantity}
                  </span>
                  <button
                    type="button"
                    onClick={() => changeQty(l.variant.variantId, 1)}
                    className="h-8 w-8 border border-border-soft text-lg leading-none transition hover:border-accent"
                    aria-label="Increase quantity"
                  >
                    +
                  </button>
                </div>
                <div className="w-24 text-right font-semibold">
                  {formatMoney(l.variant.price * l.quantity, l.variant.currency)}
                </div>
                <button
                  type="button"
                  onClick={() => removeLine(l.variant.variantId)}
                  className="text-muted transition hover:text-danger"
                  aria-label="Remove item"
                >
                  ✕
                </button>
              </li>
            ))}
          </ul>
        )}
      </div>

      {/* Right: totals + payment */}
      <aside className="flex h-fit flex-col gap-4 border border-border-soft bg-surface p-5 lg:sticky lg:top-6">
        <div className="flex justify-between text-sm text-muted">
          <span>Items</span>
          <span>{itemCount}</span>
        </div>
        <div className="flex items-baseline justify-between border-b border-border-soft pb-4">
          <span className="text-sm text-muted">Total</span>
          <span className="text-3xl font-bold">
            {formatMoney(total, currency)}
          </span>
        </div>

        <label className="flex flex-col gap-1 text-sm">
          <span className="text-muted">Cash tendered (optional)</span>
          <input
            inputMode="decimal"
            value={cash}
            onChange={(e) => setCash(e.target.value)}
            placeholder="e.g. 1000"
            className="border border-border-soft bg-background px-3 py-2 text-base outline-none focus:border-accent"
          />
        </label>

        {change !== null ? (
          <div className="flex justify-between text-lg">
            <span className="text-muted">Change</span>
            <span className="font-bold text-success">
              {formatMoney(change, currency)}
            </span>
          </div>
        ) : null}

        <button
          type="button"
          onClick={completeSale}
          disabled={pending || lines.length === 0}
          className="bg-success px-4 py-3 font-bold text-black transition hover:opacity-90 disabled:opacity-50"
        >
          {pending ? "Processing…" : "Complete cash sale"}
        </button>
        {lines.length > 0 ? (
          <button
            type="button"
            onClick={resetSale}
            className="border border-border-soft px-4 py-2 text-sm text-muted transition hover:border-danger hover:text-danger"
          >
            Clear sale
          </button>
        ) : null}
      </aside>
    </div>
  );
}
