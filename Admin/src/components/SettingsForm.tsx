"use client";

import { useState, useTransition } from "react";
import { updateCompanySettingsAction } from "@/actions/settings";
import type { CompanySettings } from "@/lib/types";

const inputClass =
  "border border-border-soft bg-surface-2 px-3 py-2 outline-none focus:border-accent";

function Field({
  label,
  children,
}: {
  label: string;
  children: React.ReactNode;
}) {
  return (
    <label className="flex flex-col gap-1 text-sm">
      <span className="text-muted">{label}</span>
      {children}
    </label>
  );
}

export function SettingsForm({ initial }: { initial: CompanySettings }) {
  const [form, setForm] = useState<CompanySettings>(initial);
  const [error, setError] = useState<string | null>(null);
  const [saved, setSaved] = useState(false);
  const [pending, startTransition] = useTransition();

  function set<K extends keyof CompanySettings>(key: K, value: CompanySettings[K]) {
    setForm((f) => ({ ...f, [key]: value }));
    setSaved(false);
  }

  function text(key: keyof CompanySettings) {
    return {
      value: (form[key] as string | null) ?? "",
      onChange: (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>) =>
        set(key, e.target.value as CompanySettings[typeof key]),
    };
  }

  function onSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    startTransition(async () => {
      const result = await updateCompanySettingsAction(form);
      if (result.error) {
        setError(result.error);
        setSaved(false);
      } else {
        setSaved(true);
      }
    });
  }

  return (
    <form onSubmit={onSubmit} className="flex flex-col gap-8">
      <section className="flex flex-col gap-4">
        <h2 className="text-sm font-semibold uppercase tracking-wide text-muted">
          Company profile
        </h2>
        <div className="grid gap-4 sm:grid-cols-2">
          <Field label="Legal name *">
            <input className={inputClass} required {...text("legalName")} />
          </Field>
          <Field label="Trading name">
            <input className={inputClass} {...text("tradingName")} />
          </Field>
          <Field label="Email">
            <input className={inputClass} type="email" {...text("email")} />
          </Field>
          <Field label="Phone">
            <input className={inputClass} {...text("phone")} />
          </Field>
          <Field label="Tax PIN / identifier">
            <input className={inputClass} {...text("taxIdentifier")} />
          </Field>
          <Field label="Logo URL">
            <input className={inputClass} {...text("logoUrl")} />
          </Field>
        </div>
      </section>

      <section className="flex flex-col gap-4">
        <h2 className="text-sm font-semibold uppercase tracking-wide text-muted">
          Address
        </h2>
        <div className="grid gap-4 sm:grid-cols-2">
          <Field label="Address line 1">
            <input className={inputClass} {...text("addressLine1")} />
          </Field>
          <Field label="Address line 2">
            <input className={inputClass} {...text("addressLine2")} />
          </Field>
          <Field label="City">
            <input className={inputClass} {...text("city")} />
          </Field>
          <Field label="Country">
            <input className={inputClass} {...text("country")} />
          </Field>
        </div>
      </section>

      <section className="flex flex-col gap-4">
        <h2 className="text-sm font-semibold uppercase tracking-wide text-muted">
          Finance defaults
        </h2>
        <div className="grid gap-4 sm:grid-cols-3">
          <Field label="Currency (3-letter)">
            <input
              className={inputClass}
              maxLength={3}
              value={form.currency}
              onChange={(e) => set("currency", e.target.value.toUpperCase())}
            />
          </Field>
          <Field label="Default tax %">
            <input
              className={inputClass}
              type="number"
              min={0}
              max={100}
              step="0.01"
              value={form.defaultTaxPercent}
              onChange={(e) => set("defaultTaxPercent", Number(e.target.value))}
            />
          </Field>
          <div />
          <Field label="Invoice prefix">
            <input className={inputClass} {...text("invoiceNumberPrefix")} />
          </Field>
          <Field label="Bill prefix">
            <input className={inputClass} {...text("billNumberPrefix")} />
          </Field>
          <Field label="Purchase order prefix">
            <input className={inputClass} {...text("purchaseOrderNumberPrefix")} />
          </Field>
        </div>
      </section>

      <section className="flex flex-col gap-4">
        <h2 className="text-sm font-semibold uppercase tracking-wide text-muted">
          Document text
        </h2>
        <Field label="Invoice footer">
          <textarea
            className={inputClass}
            rows={2}
            value={form.invoiceFooter ?? ""}
            onChange={(e) => set("invoiceFooter", e.target.value)}
          />
        </Field>
        <Field label="Payment instructions">
          <textarea
            className={inputClass}
            rows={2}
            value={form.paymentInstructions ?? ""}
            onChange={(e) => set("paymentInstructions", e.target.value)}
          />
        </Field>
      </section>

      {error ? <p className="text-sm text-danger">{error}</p> : null}
      {saved ? <p className="text-sm text-success">Settings saved.</p> : null}

      <div>
        <button
          type="submit"
          disabled={pending}
          className="bg-accent-strong px-5 py-2.5 font-semibold text-white transition hover:bg-accent disabled:cursor-not-allowed disabled:opacity-50"
        >
          {pending ? "Saving…" : "Save settings"}
        </button>
      </div>
    </form>
  );
}
