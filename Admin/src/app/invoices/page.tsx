import Link from "next/link";
import { getArSummary, getInvoices } from "@/actions/invoices";
import { formatDate, formatMoney } from "@/lib/format";
import type { InvoiceSummaryDto } from "@/lib/types";

export const metadata = {
  title: "Invoices — Nika Admin",
};

const STATUS_STYLES: Record<string, string> = {
  Draft: "bg-surface-2 text-muted",
  Sent: "bg-warning/15 text-warning",
  PartiallyPaid: "bg-accent/15 text-accent",
  Paid: "bg-success/15 text-success",
  Void: "bg-danger/15 text-danger",
};

function StatusBadge({ status, isOverdue }: { status: string; isOverdue: boolean }) {
  if (isOverdue && status !== "Paid" && status !== "Void") {
    return (
      <span className="inline-block bg-danger/15 px-2 py-0.5 text-xs font-medium text-danger">
        Overdue
      </span>
    );
  }
  const label = status.replace(/([A-Z])/g, " $1").trim();
  return (
    <span
      className={`inline-block px-2 py-0.5 text-xs font-medium ${
        STATUS_STYLES[status] ?? "bg-surface-2 text-muted"
      }`}
    >
      {label}
    </span>
  );
}

export default async function InvoicesPage() {
  const [summary, invoices] = await Promise.all([getArSummary(), getInvoices()]);
  const items: InvoiceSummaryDto[] = invoices.data?.items ?? [];
  const loadError = invoices.error ?? null;
  const currency = summary?.currency ?? "KES";

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold sm:text-3xl">Invoices</h1>
          <p className="mt-1 text-sm text-muted">
            Bill customers and track receipts (accounts receivable).
          </p>
        </div>
        <Link
          href="/invoices/new"
          className="bg-accent-strong px-4 py-2 font-semibold text-white transition hover:bg-accent"
        >
          + New invoice
        </Link>
      </div>

      {summary ? (
        <div className="grid gap-3 sm:grid-cols-3">
          <SummaryCard
            label="Outstanding"
            value={formatMoney(summary.outstanding, currency)}
            hint={`${summary.openInvoiceCount} open invoice${summary.openInvoiceCount === 1 ? "" : "s"}`}
          />
          <SummaryCard
            label="Overdue"
            value={formatMoney(summary.overdue, currency)}
            hint={`${summary.overdueInvoiceCount} invoice${summary.overdueInvoiceCount === 1 ? "" : "s"} past due`}
            danger={summary.overdue > 0}
          />
          <SummaryCard
            label="Invoices on file"
            value={String(items.length)}
            hint="Showing latest 100"
          />
        </div>
      ) : null}

      {loadError ? (
        <p className="border border-danger/40 bg-danger/10 p-4 text-sm text-danger">
          {loadError}
        </p>
      ) : items.length === 0 ? (
        <p className="border border-border-soft bg-surface p-6 text-sm text-muted">
          No invoices yet. Use{" "}
          <Link href="/invoices/new" className="text-accent underline">
            New invoice
          </Link>{" "}
          to bill your first customer.
        </p>
      ) : (
        <div className="overflow-x-auto border border-border-soft">
          <table className="w-full text-left text-sm">
            <thead className="bg-surface-2 text-muted">
              <tr>
                <th className="px-4 py-3 font-medium">Invoice</th>
                <th className="px-4 py-3 font-medium">Customer</th>
                <th className="px-4 py-3 font-medium">Due</th>
                <th className="px-4 py-3 font-medium">Status</th>
                <th className="px-4 py-3 text-right font-medium">Total</th>
                <th className="px-4 py-3 text-right font-medium">Owing</th>
              </tr>
            </thead>
            <tbody>
              {items.map((inv) => (
                <tr key={inv.id} className="border-t border-border-soft bg-surface">
                  <td className="px-4 py-3">
                    <Link
                      href={`/invoices/${inv.id}`}
                      className="font-medium text-accent underline"
                    >
                      {inv.invoiceNumber}
                    </Link>
                  </td>
                  <td className="px-4 py-3">{inv.customerName}</td>
                  <td className="px-4 py-3 text-muted">{formatDate(inv.dueDate)}</td>
                  <td className="px-4 py-3">
                    <StatusBadge status={inv.status} isOverdue={inv.isOverdue} />
                  </td>
                  <td className="px-4 py-3 text-right">
                    {formatMoney(inv.total, inv.currency)}
                  </td>
                  <td className="px-4 py-3 text-right text-muted">
                    {formatMoney(inv.amountDue, inv.currency)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}

function SummaryCard({
  label,
  value,
  hint,
  danger,
}: {
  label: string;
  value: string;
  hint: string;
  danger?: boolean;
}) {
  return (
    <div className="border border-border-soft bg-surface p-4">
      <p className="text-xs uppercase tracking-wide text-muted">{label}</p>
      <p className={`mt-1 text-xl font-bold ${danger ? "text-danger" : ""}`}>
        {value}
      </p>
      <p className="mt-1 text-xs text-muted">{hint}</p>
    </div>
  );
}
