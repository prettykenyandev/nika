"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { createVendorAction, updateVendorAction } from "@/actions/vendors";
import type { VendorDetailDto, VendorInput } from "@/lib/types";

const inputClass =
  "w-full border border-border-soft bg-surface-2 px-3 py-2 text-sm outline-none focus:border-accent";

export function VendorForm({ vendor }: { vendor?: VendorDetailDto }) {
  const router = useRouter();
  const [name, setName] = useState(vendor?.name ?? "");
  const [contactName, setContactName] = useState(vendor?.contactName ?? "");
  const [email, setEmail] = useState(vendor?.email ?? "");
  const [phone, setPhone] = useState(vendor?.phone ?? "");
  const [addressLine1, setAddressLine1] = useState(vendor?.addressLine1 ?? "");
  const [city, setCity] = useState(vendor?.city ?? "");
  const [country, setCountry] = useState(vendor?.country ?? "");
  const [taxIdentifier, setTaxIdentifier] = useState(vendor?.taxIdentifier ?? "");
  const [paymentTermDays, setPaymentTermDays] = useState(
    String(vendor?.paymentTermDays ?? 30),
  );
  const [notes, setNotes] = useState(vendor?.notes ?? "");
  const [error, setError] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();

  const editing = Boolean(vendor);

  function payload(): VendorInput {
    return {
      name,
      contactName,
      email,
      phone,
      addressLine1,
      city,
      country,
      taxIdentifier,
      paymentTermDays: Number(paymentTermDays),
      notes,
    };
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    startTransition(async () => {
      const res = vendor
        ? await updateVendorAction(vendor.id, payload())
        : await createVendorAction(payload());
      if (res.error) {
        setError(res.error);
        return;
      }
      if (vendor) {
        router.refresh();
      } else if (res.id) {
        router.push(`/vendors/${res.id}`);
      } else {
        setError("Could not save vendor.");
      }
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
        <h2 className="text-lg font-semibold">Vendor details</h2>
        <div className="grid gap-4 sm:grid-cols-2">
          <label className="flex flex-col gap-1 text-sm">
            <span className="text-muted">Name *</span>
            <input
              value={name}
              onChange={(e) => setName(e.target.value)}
              placeholder="e.g. Acme Supplies Ltd"
              className={inputClass}
              required
            />
          </label>
          <label className="flex flex-col gap-1 text-sm">
            <span className="text-muted">Contact name</span>
            <input
              value={contactName}
              onChange={(e) => setContactName(e.target.value)}
              className={inputClass}
            />
          </label>
          <label className="flex flex-col gap-1 text-sm">
            <span className="text-muted">Email</span>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="vendor@example.com"
              className={inputClass}
            />
          </label>
          <label className="flex flex-col gap-1 text-sm">
            <span className="text-muted">Phone</span>
            <input
              value={phone}
              onChange={(e) => setPhone(e.target.value)}
              className={inputClass}
            />
          </label>
          <label className="flex flex-col gap-1 text-sm">
            <span className="text-muted">Payment terms (days)</span>
            <input
              type="number"
              min="0"
              step="1"
              value={paymentTermDays}
              onChange={(e) => setPaymentTermDays(e.target.value)}
              className={inputClass}
            />
          </label>
          <label className="flex flex-col gap-1 text-sm">
            <span className="text-muted">Tax identifier</span>
            <input
              value={taxIdentifier}
              onChange={(e) => setTaxIdentifier(e.target.value)}
              className={inputClass}
            />
          </label>
        </div>
      </section>

      <section className="flex flex-col gap-4 border border-border-soft bg-surface p-4 sm:p-6">
        <h2 className="text-lg font-semibold">Address &amp; notes</h2>
        <div className="grid gap-4 sm:grid-cols-2">
          <label className="flex flex-col gap-1 text-sm sm:col-span-2">
            <span className="text-muted">Address line</span>
            <input
              value={addressLine1}
              onChange={(e) => setAddressLine1(e.target.value)}
              className={inputClass}
            />
          </label>
          <label className="flex flex-col gap-1 text-sm">
            <span className="text-muted">City</span>
            <input value={city} onChange={(e) => setCity(e.target.value)} className={inputClass} />
          </label>
          <label className="flex flex-col gap-1 text-sm">
            <span className="text-muted">Country</span>
            <input
              value={country}
              onChange={(e) => setCountry(e.target.value)}
              className={inputClass}
            />
          </label>
        </div>
        <label className="flex flex-col gap-1 text-sm">
          <span className="text-muted">Notes</span>
          <textarea
            value={notes}
            onChange={(e) => setNotes(e.target.value)}
            rows={3}
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
          {pending ? "Saving…" : editing ? "Save vendor" : "Create vendor"}
        </button>
      </div>
    </form>
  );
}
