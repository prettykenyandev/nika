import {
  getInventoryValuation,
  getPayablesAging,
  getProfitAndLoss,
  getReceivablesAging,
  getSalesAnalytics,
  getVatSummary,
} from "@/actions/reports";
import { formatDate, formatMoney } from "@/lib/format";

export const metadata = {
  title: "Reports — Nika Admin",
};

export default async function ReportsPage({
  searchParams,
}: {
  searchParams: Promise<{ from?: string; to?: string }>;
}) {
  const params = await searchParams;
  const range = { from: params.from, to: params.to };
  const [pl, ar, ap, sales, inventory, vat] = await Promise.all([
    getProfitAndLoss(range),
    getReceivablesAging(),
    getPayablesAging(),
    getSalesAnalytics(range),
    getInventoryValuation(),
    getVatSummary(range),
  ]);

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold sm:text-3xl">Reports</h1>
        <p className="mt-1 text-sm text-muted">Financial, sales, inventory, and tax reporting.</p>
      </div>

      <form className="grid gap-3 border border-border-soft bg-surface p-4 sm:grid-cols-[1fr_1fr_auto]">
        <label className="flex flex-col gap-1 text-sm">
          <span className="text-muted">From</span>
          <input type="date" name="from" defaultValue={params.from ?? ""} className="border border-border-soft bg-surface-2 px-3 py-2 outline-none focus:border-accent" />
        </label>
        <label className="flex flex-col gap-1 text-sm">
          <span className="text-muted">To</span>
          <input type="date" name="to" defaultValue={params.to ?? ""} className="border border-border-soft bg-surface-2 px-3 py-2 outline-none focus:border-accent" />
        </label>
        <button type="submit" className="self-end border border-border-soft px-4 py-2 text-sm text-muted transition hover:border-accent hover:text-accent">
          Apply
        </button>
      </form>

      <ErrorList errors={[pl.error, ar.error, ap.error, sales.error, inventory.error, vat.error]} />

      {pl.data ? (
        <section className="border border-border-soft bg-surface p-4">
          <h2 className="text-lg font-semibold">Profit &amp; loss</h2>
          <p className="text-sm text-muted">{formatDate(pl.data.from)} – {formatDate(pl.data.to)}</p>
          <div className="mt-4 grid gap-3 sm:grid-cols-4">
            <Kpi label="Order revenue" value={formatMoney(pl.data.orderRevenue, pl.data.currency)} />
            <Kpi label="Invoiced revenue" value={formatMoney(pl.data.invoicedRevenue, pl.data.currency)} />
            <Kpi label="Expenses" value={formatMoney(pl.data.totalExpenses, pl.data.currency)} />
            <Kpi label="Net profit" value={formatMoney(pl.data.netProfit, pl.data.currency)} danger={pl.data.netProfit < 0} />
          </div>
          <MiniTable
            title="Expenses by category"
            headers={["Category", "Amount"]}
            rows={pl.data.expensesByCategory.map((x) => [x.category, formatMoney(x.amount, pl.data!.currency)])}
          />
        </section>
      ) : null}

      <div className="grid gap-6 lg:grid-cols-2">
        {ar.data ? <Aging title="Receivables aging" report={ar.data} /> : null}
        {ap.data ? <Aging title="Payables aging" report={ap.data} /> : null}
      </div>

      {sales.data ? (
        <section className="border border-border-soft bg-surface p-4">
          <h2 className="text-lg font-semibold">Sales analytics</h2>
          <div className="mt-4 grid gap-3 sm:grid-cols-2">
            <Kpi label="Total sales" value={formatMoney(sales.data.totalSales, sales.data.currency)} />
            <Kpi label="Orders" value={String(sales.data.orderCount)} />
          </div>
          <div className="grid gap-4 lg:grid-cols-2">
            <MiniTable
              title="Top products"
              headers={["Product", "Qty", "Revenue"]}
              rows={sales.data.topProducts.map((x) => [x.productName, String(x.quantitySold), formatMoney(x.revenue, sales.data!.currency)])}
            />
            <MiniTable
              title="Monthly trend"
              headers={["Period", "Orders", "Revenue"]}
              rows={sales.data.monthlyTrend.map((x) => [x.period, String(x.orderCount), formatMoney(x.revenue, sales.data!.currency)])}
            />
          </div>
        </section>
      ) : null}

      {inventory.data ? (
        <section className="border border-border-soft bg-surface p-4">
          <h2 className="text-lg font-semibold">Inventory valuation</h2>
          <div className="mt-4 grid gap-3 sm:grid-cols-4">
            <Kpi label="Value" value={formatMoney(inventory.data.totalValue, inventory.data.currency)} />
            <Kpi label="Variants" value={String(inventory.data.variantCount)} />
            <Kpi label="Units" value={String(inventory.data.totalUnits)} />
            <Kpi label="Low-stock threshold" value={String(inventory.data.lowStockThreshold)} />
          </div>
          <MiniTable
            title="Low stock"
            headers={["Product", "SKU", "Variant", "Qty"]}
            rows={inventory.data.lowStock.map((x) => [x.productName, x.sku, x.variantName, String(x.stockQuantity)])}
          />
        </section>
      ) : null}

      {vat.data ? (
        <section className="border border-border-soft bg-surface p-4">
          <h2 className="text-lg font-semibold">VAT summary</h2>
          <p className="text-sm text-muted">{formatDate(vat.data.from)} – {formatDate(vat.data.to)}</p>
          <div className="mt-4 grid gap-3 sm:grid-cols-3">
            <Kpi label="Output tax" value={formatMoney(vat.data.outputTax, vat.data.currency)} />
            <Kpi label="Input tax" value={formatMoney(vat.data.inputTax, vat.data.currency)} />
            <Kpi label="Net VAT due" value={formatMoney(vat.data.netVatDue, vat.data.currency)} danger={vat.data.netVatDue > 0} />
          </div>
        </section>
      ) : null}
    </div>
  );
}

function ErrorList({ errors }: { errors: Array<string | undefined> }) {
  const visible = errors.filter((e): e is string => Boolean(e));
  if (!visible.length) return null;
  return (
    <div className="border border-danger/40 bg-danger/10 p-4 text-sm text-danger">
      {visible.map((error) => <p key={error}>{error}</p>)}
    </div>
  );
}

function Kpi({ label, value, danger }: { label: string; value: string; danger?: boolean }) {
  return (
    <div className="border border-border-soft bg-surface-2 p-3">
      <p className="text-xs uppercase tracking-wide text-muted">{label}</p>
      <p className={`mt-1 text-lg font-bold ${danger ? "text-danger" : ""}`}>{value}</p>
    </div>
  );
}

function Aging({ title, report }: { title: string; report: { currency: string; total: number; buckets: { label: string; amount: number; count: number }[] } }) {
  return (
    <section className="border border-border-soft bg-surface p-4">
      <h2 className="text-lg font-semibold">{title}</h2>
      <p className="mt-1 text-sm text-muted">Total {formatMoney(report.total, report.currency)}</p>
      <MiniTable
        title="Buckets"
        headers={["Age", "Count", "Amount"]}
        rows={report.buckets.map((x) => [x.label, String(x.count), formatMoney(x.amount, report.currency)])}
      />
    </section>
  );
}

function MiniTable({ title, headers, rows }: { title: string; headers: string[]; rows: string[][] }) {
  return (
    <div className="mt-4 overflow-x-auto border border-border-soft">
      <h3 className="border-b border-border-soft px-3 py-2 text-sm font-semibold">{title}</h3>
      {rows.length === 0 ? (
        <p className="p-3 text-sm text-muted">No data.</p>
      ) : (
        <table className="w-full text-left text-sm">
          <thead className="bg-surface-2 text-muted">
            <tr>{headers.map((h) => <th key={h} className="px-3 py-2 font-medium">{h}</th>)}</tr>
          </thead>
          <tbody>
            {rows.map((row, i) => (
              <tr key={i} className="border-t border-border-soft">
                {row.map((cell, j) => <td key={j} className="px-3 py-2">{cell}</td>)}
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
