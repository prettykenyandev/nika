"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useMutation } from "@tanstack/react-query";
import { checkout, type CheckoutInput } from "@/lib/client";
import { useCart } from "@/lib/cart-queries";
import { formatMoney } from "@/lib/format";

const FIELDS = [
  { name: "fullName", label: "Full name", type: "text", required: true },
  { name: "email", label: "Email", type: "email", required: true },
  { name: "phoneNumber", label: "M-Pesa phone (2547XXXXXXXX)", type: "tel", required: true },
  { name: "line1", label: "Address line 1", type: "text", required: true },
  { name: "line2", label: "Address line 2 (optional)", type: "text", required: false },
  { name: "city", label: "City", type: "text", required: true },
  { name: "postalCode", label: "Postal code (optional)", type: "text", required: false },
  { name: "country", label: "Country", type: "text", required: true },
] as const;

export default function CheckoutPage() {
  const router = useRouter();
  const { data: cart } = useCart();
  const [form, setForm] = useState<Record<string, string>>({ country: "Kenya" });

  const submit = useMutation({
    mutationFn: (input: CheckoutInput) => checkout(input),
    onSuccess: (result) => router.push(`/orders/${result.orderId}`),
  });

  function handleSubmit(event: React.FormEvent) {
    event.preventDefault();
    submit.mutate({
      fullName: form.fullName ?? "",
      email: form.email ?? "",
      phoneNumber: form.phoneNumber ?? "",
      line1: form.line1 ?? "",
      line2: form.line2,
      city: form.city ?? "",
      postalCode: form.postalCode,
      country: form.country ?? "",
    });
  }

  const isEmpty = !cart || cart.items.length === 0;

  return (
    <div className="grid gap-10 lg:grid-cols-[1fr_20rem]">
      <form onSubmit={handleSubmit} className="flex flex-col gap-4">
        <h1 className="text-2xl font-bold">Checkout</h1>

        {FIELDS.map((field) => (
          <label key={field.name} className="flex flex-col gap-1">
            <span className="text-sm text-muted">{field.label}</span>
            <input
              type={field.type}
              required={field.required}
              value={form[field.name] ?? ""}
              onChange={(e) => setForm((f) => ({ ...f, [field.name]: e.target.value }))}
              className="rounded-lg border border-border-soft bg-surface px-4 py-2 outline-none focus:border-accent"
            />
          </label>
        ))}

        {submit.isError ? (
          <p className="text-sm text-red-400">{submit.error.message}</p>
        ) : null}

        <button
          type="submit"
          disabled={isEmpty || submit.isPending}
          className="mt-2 rounded-full bg-accent-strong px-8 py-3 font-semibold text-white shadow-lg shadow-accent-strong/30 transition hover:bg-accent disabled:opacity-50"
        >
          {submit.isPending ? "Sending M-Pesa request..." : "Pay with M-Pesa"}
        </button>
        <p className="text-xs text-muted">
          You&apos;ll receive an STK push on your phone to authorise the payment.
        </p>
      </form>

      <aside className="h-fit rounded-xl border border-border-soft p-6">
        <h2 className="text-lg font-bold">Order summary</h2>
        {cart ? (
          <>
            <ul className="mt-4 flex flex-col gap-2 text-sm">
              {cart.items.map((item) => (
                <li key={item.productVariantId} className="flex justify-between gap-2">
                  <span className="text-muted">
                    {item.quantity}× {item.productName}
                  </span>
                  <span>{formatMoney(item.lineTotal, cart.currency)}</span>
                </li>
              ))}
            </ul>
            <div className="mt-4 flex justify-between border-t border-border-soft pt-4 font-semibold">
              <span>Total</span>
              <span>{formatMoney(cart.subtotal, cart.currency)}</span>
            </div>
          </>
        ) : (
          <p className="mt-4 text-sm text-muted">Your cart is empty.</p>
        )}
      </aside>
    </div>
  );
}
