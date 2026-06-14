"use client";

import { useMemo, useRef, useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import {
  createBillAction,
  createExpenseCategoryAction,
  uploadReceiptAction,
} from "@/actions/expenses";
import { formatMoney } from "@/lib/format";
import { MAX_UPLOAD_BYTES, MAX_UPLOAD_LABEL } from "@/lib/constants";
import type { ExpenseCategoryDto } from "@/lib/types";

const inputClass =
  "w-full border border-border-soft bg-surface-2 px-3 py-2 text-sm outline-none focus:border-accent";

interface LineRow {
  description: string;
  expenseCategoryId: string;
  quantity: string;
  unitCost: string;
  taxPercent: string;
}

function emptyLine(defaultTax: number): LineRow {
  return {
    description: "",
    expenseCategoryId: "",
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

export function BillForm({
  categories: initialCategories,
  defaultCurrency,
  defaultTaxPercent,
}: {
  categories: ExpenseCategoryDto[];
  defaultCurrency: string;
  defaultTaxPercent: number;
}) {
  const router = useRouter();
  const [categories, setCategories] = useState(initialCategories);
  const [vendorName, setVendorName] = useState("");
  const [supplierReference, setSupplierReference] = useState("");
  const [issueDate, setIssueDate] = useState(todayIso());
  const [dueDate, setDueDate] = useState(addDaysIso(30));
  const [notes, setNotes] = useState("");
  const [lines, setLines] = useState<LineRow[]>([emptyLine(defaultTaxPercent)]);
  const [attachmentUrl, setAttachmentUrl] = useState<string | null>(null);
  const [uploadError, setUploadError] = useState<string | null>(null);
  const [uploading, setUploading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();
  const fileRef = useRef<HTMLInputElement>(null);

  // Inline "add category"
  const [showNewCategory, setShowNewCategory] = useState(false);
  const [newCategory, setNewCategory] = useState("");
  const [catError, setCatError] = useState<string | null>(null);
  const [catPending, startCatTransition] = useTransition();

  function updateLine(index: number, field: keyof LineRow, value: string) {
    setLines((prev) =>
      prev.map((l, i) => (i === index ? { ...l, [field]: value } : l)),
    );
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

  function handleUpload(file: File | undefined) {
    if (!file) return;
    setUploadError(null);
    if (file.size > MAX_UPLOAD_BYTES) {
      setUploadError(
        `“${file.name}” is too large. Receipts must be ${MAX_UPLOAD_LABEL} or smaller.`,
      );
      if (fileRef.current) fileRef.current.value = "";
      return;
    }
    setUploading(true);
    const fd = new FormData();
    fd.append("file", file);
    startTransition(async () => {
      const res = await uploadReceiptAction(fd);
      setUploading(false);
      if (res.error || !res.url) {
        setUploadError(res.error ?? "Upload failed.");
        if (fileRef.current) fileRef.current.value = "";
        return;
      }
      setAttachmentUrl(res.url);
    });
  }

  function handleAddCategory() {
    const trimmed = newCategory.trim();
    if (!trimmed) return;
    setCatError(null);
    startCatTransition(async () => {
      const res = await createExpenseCategoryAction(trimmed, null);
      if (res.error || !res.id) {
        setCatError(res.error ?? "Could not create category.");
        return;
      }
      const created: ExpenseCategoryDto = {
        id: res.id,
        name: trimmed,
        slug: trimmed.toLowerCase().replace(/[^a-z0-9]+/g, "-"),
        description: null,
      };
      setCategories((prev) => [...prev, created]);
      setNewCategory("");
      setShowNewCategory(false);
    });
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    startTransition(async () => {
      const res = await createBillAction({
        vendorName,
        issueDate,
        dueDate,
        currency: defaultCurrency,
        supplierReference,
        notes,
        attachmentUrl,
        lines: lines.map((l) => ({
          description: l.description,
          expenseCategoryId: l.expenseCategoryId || null,
          quantity: Number(l.quantity),
          unitCost: Number(l.unitCost),
          taxPercent: l.taxPercent === "" ? null : Number(l.taxPercent),
        })),
      });
      if (res.error || !res.id) {
        setError(res.error ?? "Could not create bill.");
        return;
      }
      router.push(`/expenses/${res.id}`);
    });
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-6">
      {error ? (
        <p className="border border-danger/40 bg-danger/10 p-4 text-sm text-danger">
          {error}
        </p>
      ) : null}

      {/* Vendor & dates */}
      <section className="flex flex-col gap-4 border border-border-soft bg-surface p-4 sm:p-6">
        <h2 className="text-lg font-semibold">Vendor &amp; dates</h2>
        <div className="grid gap-4 sm:grid-cols-2">
          <label className="flex flex-col gap-1 text-sm">
            <span className="text-muted">Vendor name *</span>
            <input
              value={vendorName}
              onChange={(e) => setVendorName(e.target.value)}
              placeholder="e.g. Acme Properties Ltd"
              className={inputClass}
              required
            />
          </label>
          <label className="flex flex-col gap-1 text-sm">
            <span className="text-muted">Supplier invoice / reference</span>
            <input
              value={supplierReference}
              onChange={(e) => setSupplierReference(e.target.value)}
              placeholder="Their invoice number"
              className={inputClass}
            />
          </label>
          <label className="flex flex-col gap-1 text-sm">
            <span className="text-muted">Issue date *</span>
            <input
              type="date"
              value={issueDate}
              onChange={(e) => setIssueDate(e.target.value)}
              className={inputClass}
              required
            />
          </label>
          <label className="flex flex-col gap-1 text-sm">
            <span className="text-muted">Due date *</span>
            <input
              type="date"
              value={dueDate}
              onChange={(e) => setDueDate(e.target.value)}
              className={inputClass}
              required
            />
          </label>
        </div>
      </section>

      {/* Line items */}
      <section className="flex flex-col gap-4 border border-border-soft bg-surface p-4 sm:p-6">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold">Line items</h2>
          <button
            type="button"
            onClick={addLine}
            className="text-sm text-accent underline"
          >
            + Add line
          </button>
        </div>

        <div className="flex flex-col gap-2">
          <button
            type="button"
            onClick={() => setShowNewCategory((v) => !v)}
            className="self-start text-xs text-muted underline transition hover:text-accent"
          >
            {showNewCategory ? "Cancel new category" : "+ New expense category"}
          </button>
          {showNewCategory ? (
            <div className="flex flex-col gap-2 border border-border-soft bg-surface-2 p-3 sm:flex-row">
              <input
                value={newCategory}
                onChange={(e) => setNewCategory(e.target.value)}
                placeholder="e.g. Rent, Utilities, Salaries"
                className={`${inputClass} sm:flex-1`}
              />
              <button
                type="button"
                onClick={handleAddCategory}
                disabled={catPending}
                className="bg-accent-strong px-4 py-2 text-sm font-semibold text-white transition hover:bg-accent disabled:opacity-50"
              >
                {catPending ? "Adding…" : "Add"}
              </button>
            </div>
          ) : null}
          {catError ? <p className="text-xs text-danger">{catError}</p> : null}
        </div>

        <div className="flex flex-col gap-4">
          {lines.map((l, i) => (
            <div
              key={i}
              className="grid grid-cols-1 gap-3 border border-border-soft bg-surface-2 p-3 sm:grid-cols-2 lg:grid-cols-[1.6fr_1.1fr_0.7fr_0.9fr_0.7fr_auto] lg:items-end"
            >
              <label className="flex flex-col gap-1 text-xs">
                <span className="text-muted">Description</span>
                <input
                  value={l.description}
                  onChange={(e) => updateLine(i, "description", e.target.value)}
                  placeholder="What was purchased"
                  className={inputClass}
                />
              </label>
              <label className="flex flex-col gap-1 text-xs">
                <span className="text-muted">Category</span>
                <select
                  value={l.expenseCategoryId}
                  onChange={(e) => updateLine(i, "expenseCategoryId", e.target.value)}
                  className={inputClass}
                >
                  <option value="">Uncategorised</option>
                  {categories.map((c) => (
                    <option key={c.id} value={c.id}>
                      {c.name}
                    </option>
                  ))}
                </select>
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
          <div className="flex w-full max-w-xs justify-between text-muted">
            <span>Subtotal</span>
            <span>{formatMoney(totals.net, defaultCurrency)}</span>
          </div>
          <div className="flex w-full max-w-xs justify-between text-muted">
            <span>Tax</span>
            <span>{formatMoney(totals.tax, defaultCurrency)}</span>
          </div>
          <div className="flex w-full max-w-xs justify-between font-semibold">
            <span>Total</span>
            <span>{formatMoney(totals.total, defaultCurrency)}</span>
          </div>
        </div>
      </section>

      {/* Receipt & notes */}
      <section className="flex flex-col gap-4 border border-border-soft bg-surface p-4 sm:p-6">
        <h2 className="text-lg font-semibold">Receipt &amp; notes</h2>
        <div className="flex flex-col gap-2">
          <span className="text-sm text-muted">Receipt / attachment</span>
          {attachmentUrl ? (
            <div className="flex items-center gap-3 text-sm">
              <a
                href={attachmentUrl}
                target="_blank"
                rel="noreferrer"
                className="text-accent underline"
              >
                View uploaded receipt
              </a>
              <button
                type="button"
                onClick={() => {
                  setAttachmentUrl(null);
                  if (fileRef.current) fileRef.current.value = "";
                }}
                className="text-muted underline hover:text-danger"
              >
                Remove
              </button>
            </div>
          ) : (
            <input
              ref={fileRef}
              type="file"
              accept="image/*,application/pdf"
              onChange={(e) => handleUpload(e.target.files?.[0])}
              disabled={uploading}
              className="text-sm text-muted file:mr-3 file:border file:border-border-soft file:bg-surface-2 file:px-3 file:py-1.5 file:text-sm"
            />
          )}
          <p className="text-xs text-muted">
            Images or PDF, up to {MAX_UPLOAD_LABEL}.
          </p>
          {uploading ? <p className="text-xs text-muted">Uploading…</p> : null}
          {uploadError ? <p className="text-xs text-danger">{uploadError}</p> : null}
        </div>
        <label className="flex flex-col gap-1 text-sm">
          <span className="text-muted">Notes</span>
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
          disabled={pending || uploading}
          className="bg-accent-strong px-6 py-3 font-semibold text-white transition hover:bg-accent disabled:cursor-not-allowed disabled:opacity-50"
        >
          {pending ? "Saving…" : "Create bill"}
        </button>
      </div>
    </form>
  );
}
