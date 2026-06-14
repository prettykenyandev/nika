import Link from "next/link";
import { getApSummary, getBills } from "@/actions/expenses";
import { formatDate, formatMoney } from "@/lib/format";
import type { BillSummaryDto } from "@/lib/types";

export const metadata = {
  title: "Expenses — Nika Admin",
};

const STATUS_STYLES: Record<string, string> = {
  Draft: "bg-surface-2 text-muted",
  AwaitingPayment: "bg-warning/15 text-warning",
  PartiallyPaid: "bg-accent/15 text-accent",
  Paid: "bg-success/15 text-success",
  Cancelled: "bg-danger/15 text-danger",
};

function StatusBadge({ status, isOverdue }: { status: string; isOverdue: boolean }) {
  if (isOverdue && status !== "Paid" && status !== "Cancelled") {
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

export default async function ExpensesPage() {
  const [summary, bills] = await Promise.all([getApSummary(), getBills()]);
  const items: BillSummaryDto[] = bills.data?.items ?? [];
  const loadError = bills.error ?? null;
  const currency = summary?.currency ?? "KES";

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold sm:text-3xl">Expenses &amp; bills</h1>
          <p className="mt-1 text-sm text-muted">
            Track supplier costs, approvals and payments (accounts payable).
          </p>
        </div>
        <div className="flex gap-2">
          <Link
            href="/expenses/categories"
            className="border border-border-soft px-4 py-2 text-sm text-muted transition hover:border-accent hover:text-accent"
          >
            Categories
          </Link>
          <Link
            href="/expenses/new"
            className="bg-accent-strong px-4 py-2 font-semibold text-white transition hover:bg-accent"
          >
            + New bill
          </Link>
        </div>
      </div>

      {summary ? (
        <div className="grid gap-3 sm:grid-cols-3">
          <SummaryCard
            label="Outstanding"
            value={formatMoney(summary.outstanding, currency)}
            hint={`${summary.openBillCount} open bill${summary.openBillCount === 1 ? "" : "s"}`}
          />
          <SummaryCard
            label="Overdue"
            value={formatMoney(summary.overdue, currency)}
            hint={`${summary.overdueBillCount} bill${summary.overdueBillCount === 1 ? "" : "s"} past due`}
            danger={summary.overdue > 0}
          />
          <SummaryCard
            label="Bills on file"
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
          No bills yet. Use{" "}
          <Link href="/expenses/new" className="text-accent underline">
            New bill
          </Link>{" "}
          to record your first expense.
        </p>
      ) : (
        <div className="overflow-x-auto border border-border-soft">
          <table className="w-full text-left text-sm">
            <thead className="bg-surface-2 text-muted">
              <tr>
                <th className="px-4 py-3 font-medium">Bill</th>
                <th className="px-4 py-3 font-medium">Vendor</th>
                <th className="px-4 py-3 font-medium">Due</th>
                <th className="px-4 py-3 font-medium">Status</th>
                <th className="px-4 py-3 text-right font-medium">Total</th>
                <th className="px-4 py-3 text-right font-medium">Owing</th>
              </tr>
            </thead>
            <tbody>
              {items.map((b) => (
                <tr key={b.id} className="border-t border-border-soft bg-surface">
                  <td className="px-4 py-3">
                    <Link
                      href={`/expenses/${b.id}`}
                      className="font-medium text-accent underline"
                    >
                      {b.billNumber}
                    </Link>
                  </td>
                  <td className="px-4 py-3">{b.vendorName}</td>
                  <td className="px-4 py-3 text-muted">{formatDate(b.dueDate)}</td>
                  <td className="px-4 py-3">
                    <StatusBadge status={b.status} isOverdue={b.isOverdue} />
                  </td>
                  <td className="px-4 py-3 text-right">
                    {formatMoney(b.total, b.currency)}
                  </td>
                  <td className="px-4 py-3 text-right text-muted">
                    {formatMoney(b.amountDue, b.currency)}
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
