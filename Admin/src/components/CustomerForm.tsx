"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { createCustomerAction, updateCustomerAction } from "@/actions/customers";
import type { CustomerDetailDto, CustomerInput } from "@/lib/types";

const inputClass =
  "w-full border border-border-soft bg-surface-2 px-3 py-2 text-sm outline-none focus:border-accent";

export function CustomerForm({ customer }: { customer?: CustomerDetailDto }) {
  const router = useRouter();
  const [name, setName] = useState(customer?.name ?? "");
  const [email, setEmail] = useState(customer?.email ?? "");
  const [phone, setPhone] = useState(customer?.phone ?? "");
  const [addressLine1, setAddressLine1] = useState(customer?.addressLine1 ?? "");
  const [city, setCity] = useState(customer?.city ?? "");
  const [country, setCountry] = useState(customer?.country ?? "");
  const [notes, setNotes] = useState(customer?.notes ?? "");
  const [error, setError] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();
  const editing = Boolean(customer);

  function payload(): CustomerInput {
    return { name, email, phone, addressLine1, city, country, notes };
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    startTransition(async () => {
      const res = customer
        ? await updateCustomerAction(customer.id, payload())
        : await createCustomerAction(payload());
      if (res.error) {
        setError(res.error);
        return;
      }
      if (customer) router.refresh();
      else if (res.id) router.push(`/customers/${res.id}`);
      else setError("Could not save customer.");
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
        <h2 className="text-lg font-semibold">Customer details</h2>
        <div className="grid gap-4 sm:grid-cols-2">
          <label className="flex flex-col gap-1 text-sm">
            <span className="text-muted">Name *</span>
            <input value={name} onChange={(e) => setName(e.target.value)} className={inputClass} required />
          </label>
          <label className="flex flex-col gap-1 text-sm">
            <span className="text-muted">Email</span>
            <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} className={inputClass} />
          </label>
          <label className="flex flex-col gap-1 text-sm">
            <span className="text-muted">Phone</span>
            <input value={phone} onChange={(e) => setPhone(e.target.value)} className={inputClass} />
          </label>
          <label className="flex flex-col gap-1 text-sm">
            <span className="text-muted">City</span>
            <input value={city} onChange={(e) => setCity(e.target.value)} className={inputClass} />
          </label>
        </div>
      </section>

      <section className="flex flex-col gap-4 border border-border-soft bg-surface p-4 sm:p-6">
        <h2 className="text-lg font-semibold">Billing address &amp; notes</h2>
        <label className="flex flex-col gap-1 text-sm">
          <span className="text-muted">Address line</span>
          <input
            value={addressLine1}
            onChange={(e) => setAddressLine1(e.target.value)}
            className={inputClass}
          />
        </label>
        <label className="flex flex-col gap-1 text-sm">
          <span className="text-muted">Country</span>
          <input value={country} onChange={(e) => setCountry(e.target.value)} className={inputClass} />
        </label>
        <label className="flex flex-col gap-1 text-sm">
          <span className="text-muted">Notes</span>
          <textarea value={notes} onChange={(e) => setNotes(e.target.value)} rows={3} className={inputClass} />
        </label>
      </section>

      <div className="flex justify-end">
        <button
          type="submit"
          disabled={pending}
          className="bg-accent-strong px-6 py-3 font-semibold text-white transition hover:bg-accent disabled:cursor-not-allowed disabled:opacity-50"
        >
          {pending ? "Saving…" : editing ? "Save customer" : "Create customer"}
        </button>
      </div>
    </form>
  );
}
