"use client";

import { use } from "react";
import Link from "next/link";
import { useQuery } from "@tanstack/react-query";
import { fetchOrder } from "@/lib/client";
import { formatMoney } from "@/lib/format";
import type { OrderStatus } from "@/lib/types";

const STATUS_COPY: Record<OrderStatus, { title: string; tone: string; body: string }> = {
  PendingPayment: {
    title: "Awaiting M-Pesa confirmation",
    tone: "text-amber-400",
    body: "Check your phone and enter your M-Pesa PIN to authorise the payment.",
  },
  Paid: {
    title: "Payment received",
    tone: "text-green-400",
    body: "Thank you! Your order is confirmed and being prepared.",
  },
  Fulfilled: {
    title: "Order fulfilled",
    tone: "text-green-400",
    body: "Your order is on its way.",
  },
  PaymentFailed: {
    title: "Payment failed",
    tone: "text-red-400",
    body: "We couldn't confirm your M-Pesa payment. Please try checking out again.",
  },
  Cancelled: {
    title: "Order cancelled",
    tone: "text-muted",
    body: "This order was cancelled.",
  },
};

export default function OrderPage({ params }: { params: Promise<{ id: string }> }) {
  const { id } = use(params);

  const { data: order, isLoading } = useQuery({
    queryKey: ["order", id],
    queryFn: () => fetchOrder(id),
    // Poll while we wait for the webhook to confirm the payment.
    refetchInterval: (query) =>
      query.state.data?.status === "PendingPayment" ? 4000 : false,
  });

  if (isLoading || !order) {
    return <p className="text-muted">Loading your order...</p>;
  }

  const status = STATUS_COPY[order.status];

  return (
    <div className="mx-auto flex max-w-xl flex-col gap-6">
      <div className="rounded-xl border border-border-soft p-6">
        <span className="text-sm text-muted">Order {order.orderNumber}</span>
        <h1 className={`mt-1 text-2xl font-bold ${status.tone}`}>{status.title}</h1>
        <p className="mt-2 text-muted">{status.body}</p>
        {order.status === "PendingPayment" ? (
          <p className="mt-3 flex items-center gap-2 text-sm text-muted">
            <span className="h-2 w-2 animate-pulse rounded-full bg-amber-400" />
            Waiting for confirmation...
          </p>
        ) : null}
      </div>

      <div className="rounded-xl border border-border-soft p-6">
        <h2 className="font-bold">Items</h2>
        <ul className="mt-3 flex flex-col gap-2 text-sm">
          {order.items.map((item) => (
            <li key={item.sku} className="flex justify-between gap-2">
              <span className="text-muted">
                {item.quantity}× {item.productName}
              </span>
              <span>{formatMoney(item.lineTotal, order.currency)}</span>
            </li>
          ))}
        </ul>
        <div className="mt-4 flex justify-between border-t border-border-soft pt-4 font-semibold">
          <span>Total</span>
          <span>{formatMoney(order.total, order.currency)}</span>
        </div>
      </div>

      <Link href="/products" className="text-center text-sm text-muted hover:text-accent">
        Continue shopping
      </Link>
    </div>
  );
}
