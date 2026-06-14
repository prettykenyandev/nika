import Link from "next/link";
import { notFound } from "next/navigation";
import { getCustomer } from "@/actions/customers";
import { CustomerActiveButton } from "@/components/CustomerActions";
import { CustomerForm } from "@/components/CustomerForm";
import { formatDate, formatMoney } from "@/lib/format";

export const metadata = {
  title: "Customer — Nika Admin",
};

export default async function CustomerDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const customer = await getCustomer(id);
  if (!customer) notFound();

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Link href="/customers" className="text-sm text-accent underline">← Back to customers</Link>
        <div className="mt-2 flex flex-wrap items-center justify-between gap-3">
          <div>
            <h1 className="text-2xl font-bold sm:text-3xl">{customer.name}</h1>
            <p className="mt-1 text-sm text-muted">{customer.email ?? "No email"}{customer.phone ? ` · ${customer.phone}` : ""}</p>
          </div>
          <div className="flex flex-wrap gap-2">
            <Link
              href={`/invoices/new?customerId=${customer.id}`}
              className="bg-accent-strong px-4 py-2 text-sm font-semibold text-white transition hover:bg-accent"
            >
              New invoice for customer
            </Link>
            <CustomerActiveButton customerId={customer.id} isActive={customer.isActive} />
          </div>
        </div>
      </div>

      <div className="grid gap-3 sm:grid-cols-3">
        <SummaryCard label="Outstanding" value={formatMoney(customer.outstandingBalance, customer.currency)} danger={customer.outstandingBalance > 0} />
        <SummaryCard label="Total invoiced" value={formatMoney(customer.totalInvoiced, customer.currency)} />
        <SummaryCard label="Lifetime orders" value={formatMoney(customer.lifetimeOrderValue, customer.currency)} />
      </div>

      <div className="grid gap-6 lg:grid-cols-2">
        <HistoryTable
          title="Order history"
          empty="No orders yet."
          headers={["Order", "Date", "Status", "Total"]}
          rows={customer.orders.map((order) => [
            order.orderNumber,
            formatDate(order.createdAtUtc),
            order.status,
            formatMoney(order.total, order.currency),
          ])}
        />
        <section className="border border-border-soft bg-surface">
          <h2 className="border-b border-border-soft px-4 py-3 text-sm font-semibold">Invoice history</h2>
          {customer.invoices.length === 0 ? (
            <p className="p-4 text-sm text-muted">No invoices yet.</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-left text-sm">
                <thead className="bg-surface-2 text-muted">
                  <tr>
                    <th className="px-4 py-2 font-medium">Invoice</th>
                    <th className="px-4 py-2 font-medium">Due</th>
                    <th className="px-4 py-2 font-medium">Status</th>
                    <th className="px-4 py-2 text-right font-medium">Due</th>
                  </tr>
                </thead>
                <tbody>
                  {customer.invoices.map((invoice) => (
                    <tr key={invoice.id} className="border-t border-border-soft">
                      <td className="px-4 py-2"><Link href={`/invoices/${invoice.id}`} className="text-accent underline">{invoice.invoiceNumber}</Link></td>
                      <td className="px-4 py-2 text-muted">{formatDate(invoice.dueDate)}</td>
                      <td className="px-4 py-2">{invoice.isOverdue ? "Overdue" : invoice.status}</td>
                      <td className="px-4 py-2 text-right">{formatMoney(invoice.amountDue, invoice.currency)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
      </div>

      <CustomerForm customer={customer} />
    </div>
  );
}

function SummaryCard({ label, value, danger }: { label: string; value: string; danger?: boolean }) {
  return (
    <div className="border border-border-soft bg-surface p-4">
      <p className="text-xs uppercase tracking-wide text-muted">{label}</p>
      <p className={`mt-1 text-xl font-bold ${danger ? "text-danger" : ""}`}>{value}</p>
    </div>
  );
}

function HistoryTable({
  title,
  empty,
  headers,
  rows,
}: {
  title: string;
  empty: string;
  headers: string[];
  rows: string[][];
}) {
  return (
    <section className="border border-border-soft bg-surface">
      <h2 className="border-b border-border-soft px-4 py-3 text-sm font-semibold">{title}</h2>
      {rows.length === 0 ? (
        <p className="p-4 text-sm text-muted">{empty}</p>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead className="bg-surface-2 text-muted">
              <tr>{headers.map((h) => <th key={h} className="px-4 py-2 font-medium">{h}</th>)}</tr>
            </thead>
            <tbody>
              {rows.map((row, i) => (
                <tr key={i} className="border-t border-border-soft">
                  {row.map((cell, j) => <td key={j} className="px-4 py-2">{cell}</td>)}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  );
}
