"use client";

import { useState } from "react";
import Link from "next/link";
import type { ProductVariantDto } from "@/lib/types";
import { formatMoney } from "@/lib/format";
import { useAddToCart } from "@/lib/cart-queries";

export function AddToCartPanel({ variants }: { variants: ProductVariantDto[] }) {
  const inStockVariants = variants.filter((v) => v.inStock);
  const [selectedId, setSelectedId] = useState(
    inStockVariants[0]?.id ?? variants[0]?.id ?? "",
  );
  const [quantity, setQuantity] = useState(1);
  const [added, setAdded] = useState(false);

  const addToCart = useAddToCart();
  const selected = variants.find((v) => v.id === selectedId);
  const maxQty = Math.min(selected?.stockQuantity ?? 1, 10);

  function handleAdd() {
    if (!selected) return;
    setAdded(false);
    addToCart.mutate(
      { variantId: selected.id, quantity },
      { onSuccess: () => setAdded(true) },
    );
  }

  return (
    <div className="flex flex-col gap-5">
      <div className="text-3xl font-bold text-accent">
        {selected ? formatMoney(selected.price, selected.currency) : "—"}
      </div>

      <div className="flex flex-col gap-2">
        <label className="text-sm font-medium text-muted">Option</label>
        <div className="flex flex-wrap gap-2">
          {variants.map((variant) => (
            <button
              key={variant.id}
              type="button"
              disabled={!variant.inStock}
              onClick={() => {
                setSelectedId(variant.id);
                setQuantity(1);
              }}
              className={`rounded-lg border px-4 py-2 text-sm transition ${
                variant.id === selectedId
                  ? "border-accent bg-accent-strong text-white"
                  : "border-border-soft hover:border-accent"
              } ${variant.inStock ? "" : "cursor-not-allowed opacity-40"}`}
            >
              {variant.name}
            </button>
          ))}
        </div>
      </div>

      <div className="flex items-center gap-3">
        <label className="text-sm font-medium text-muted">Qty</label>
        <select
          value={quantity}
          onChange={(e) => setQuantity(Number(e.target.value))}
          disabled={!selected?.inStock}
          className="rounded-lg border border-border-soft bg-surface px-3 py-2"
        >
          {Array.from({ length: Math.max(maxQty, 1) }, (_, i) => i + 1).map((n) => (
            <option key={n} value={n}>
              {n}
            </option>
          ))}
        </select>
      </div>

      <button
        type="button"
        onClick={handleAdd}
        disabled={!selected?.inStock || addToCart.isPending}
        className="rounded-full bg-accent-strong px-8 py-3 font-semibold text-white shadow-lg shadow-accent-strong/30 transition hover:bg-accent disabled:cursor-not-allowed disabled:opacity-50"
      >
        {addToCart.isPending
          ? "Adding..."
          : selected?.inStock
            ? "Add to cart"
            : "Sold out"}
      </button>

      {addToCart.isError ? (
        <p className="text-sm text-red-400">{addToCart.error.message}</p>
      ) : null}

      {added ? (
        <p className="text-sm text-green-400">
          Added to cart.{" "}
          <Link href="/cart" className="underline">
            View cart
          </Link>
        </p>
      ) : null}
    </div>
  );
}
