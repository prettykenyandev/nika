import Link from "next/link";
import { getDashboardSummary } from "@/actions/reports";
import { formatMoney } from "@/lib/format";

export default async function DashboardPage() {
  const summary = await getDashboardSummary();
  const data = summary.data;
  const currency = data?.currency ?? "KES";

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold sm:text-3xl">Dashboard</h1>
          <p className="mt-1 text-sm text-muted">This month’s performance and operational alerts.</p>
        </div>
        <Link href="/reports" className="border border-border-soft px-4 py-2 text-sm text-muted transition hover:border-accent hover:text-accent">
          View reports
        </Link>
      </div>

      {summary.error ? (
        <p className="border border-danger/40 bg-danger/10 p-4 text-sm text-danger">{summary.error}</p>
      ) : data ? (
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4">
          <Kpi label="Revenue this month" value={formatMoney(data.revenueThisMonth, currency)} />
          <Kpi label="Expenses this month" value={formatMoney(data.expensesThisMonth, currency)} />
          <Kpi label="Net this month" value={formatMoney(data.netThisMonth, currency)} danger={data.netThisMonth < 0} />
          <Kpi label="Outstanding AR" value={formatMoney(data.outstandingReceivables, currency)} hint={`${formatMoney(data.overdueReceivables, currency)} overdue`} danger={data.overdueReceivables > 0} />
          <Kpi label="Outstanding AP" value={formatMoney(data.outstandingPayables, currency)} hint={`${formatMoney(data.overduePayables, currency)} overdue`} danger={data.overduePayables > 0} />
          <Kpi label="Inventory value" value={formatMoney(data.inventoryValue, currency)} />
          <Kpi label="Low stock" value={String(data.lowStockCount)} danger={data.lowStockCount > 0} />
          <Kpi label="Open purchase orders" value={String(data.openPurchaseOrders)} />
        </div>
      ) : (
        <p className="border border-border-soft bg-surface p-6 text-sm text-muted">No dashboard data available.</p>
      )}
    </div>
  );
}

function Kpi({
  label,
  value,
  hint,
  danger,
}: {
  label: string;
  value: string;
  hint?: string;
  danger?: boolean;
}) {
  return (
    <div className="border border-border-soft bg-surface p-4">
      <p className="text-xs uppercase tracking-wide text-muted">{label}</p>
      <p className={`mt-1 text-xl font-bold ${danger ? "text-danger" : ""}`}>{value}</p>
      {hint ? <p className="mt-1 text-xs text-muted">{hint}</p> : null}
    </div>
  );
}
