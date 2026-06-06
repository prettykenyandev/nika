"use client";

import Link from "next/link";
import { useCart } from "@/lib/cart-queries";

export function CartLink() {
  const { data: cart } = useCart();
  const count = cart?.itemCount ?? 0;

  return (
    <Link
      href="/cart"
      className="relative rounded-full border border-border-soft px-4 py-2 text-sm font-medium text-muted transition hover:border-accent hover:text-accent"
    >
      Cart
      {count > 0 ? (
        <span className="ml-2 rounded-full bg-accent px-2 py-0.5 text-xs font-semibold text-white">
          {count}
        </span>
      ) : null}
    </Link>
  );
}
