"use client";

import Link from "next/link";
import {
  useCart,
  useRemoveCartItem,
  useUpdateCartItem,
} from "@/lib/cart-queries";
import { formatMoney } from "@/lib/format";

export default function CartPage() {
  const { data: cart, isLoading, isError } = useCart();
  const updateItem = useUpdateCartItem();
  const removeItem = useRemoveCartItem();

  if (isLoading) {
    return <p className="text-muted">Loading your cart...</p>;
  }

  if (isError) {
    return <p className="text-red-400">We couldn&apos;t load your cart. Please retry.</p>;
  }

  if (!cart || cart.items.length === 0) {
    return (
      <div className="flex flex-col items-center gap-4 py-16 text-center">
        <h1 className="text-2xl font-bold">Your cart is empty</h1>
        <Link
          href="/products"
          className="rounded-full bg-accent-strong px-6 py-3 font-semibold text-white transition hover:bg-accent"
        >
          Start shopping
        </Link>
      </div>
    );
  }

  return (
    <div className="flex flex-col gap-8 lg:flex-row">
      <div className="flex flex-1 flex-col gap-4">
        <h1 className="text-2xl font-bold">Your cart</h1>
        <ul className="flex flex-col divide-y divide-border-soft rounded-xl border border-border-soft">
          {cart.items.map((item) => (
            <li key={item.productVariantId} className="flex gap-4 p-4">
              <div className="flex h-20 w-20 shrink-0 items-center justify-center overflow-hidden rounded-lg bg-surface-2 text-center text-xs text-muted">
                {item.imageUrl ? (
                  // eslint-disable-next-line @next/next/no-img-element
                  <img src={item.imageUrl} alt={item.productName} className="h-full w-full object-cover" />
                ) : (
                  item.productName
                )}
              </div>
              <div className="flex flex-1 flex-col gap-1">
                <Link href={`/products/${item.slug}`} className="font-semibold hover:text-accent">
                  {item.productName}
                </Link>
                <span className="text-sm text-muted">{item.variantName}</span>
                <span className="text-sm text-accent">
                  {formatMoney(item.unitPrice, cart.currency)}
                </span>
              </div>
              <div className="flex flex-col items-end justify-between">
                <select
                  value={item.quantity}
                  disabled={updateItem.isPending}
                  onChange={(e) =>
                    updateItem.mutate({
                      variantId: item.productVariantId,
                      quantity: Number(e.target.value),
                    })
                  }
                  className="rounded-lg border border-border-soft bg-surface px-2 py-1"
                >
                  {Array.from(
                    { length: Math.max(Math.min(item.availableStock, 10), item.quantity) },
                    (_, i) => i + 1,
                  ).map((n) => (
                    <option key={n} value={n}>
                      {n}
                    </option>
                  ))}
                </select>
                <button
                  type="button"
                  onClick={() => removeItem.mutate(item.productVariantId)}
                  disabled={removeItem.isPending}
                  className="text-sm text-muted hover:text-red-400"
                >
                  Remove
                </button>
              </div>
            </li>
          ))}
        </ul>
      </div>

      <aside className="h-fit w-full rounded-xl border border-border-soft p-6 lg:w-80">
        <h2 className="text-lg font-bold">Summary</h2>
        <div className="mt-4 flex justify-between text-sm">
          <span className="text-muted">Subtotal ({cart.itemCount} items)</span>
          <span className="font-semibold">{formatMoney(cart.subtotal, cart.currency)}</span>
        </div>
        <Link
          href="/checkout"
          className="mt-6 block rounded-full bg-accent-strong px-6 py-3 text-center font-semibold text-white transition hover:bg-accent"
        >
          Checkout
        </Link>
      </aside>
    </div>
  );
}
