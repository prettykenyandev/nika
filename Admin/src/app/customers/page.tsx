import Link from "next/link";
import { getCustomers } from "@/actions/customers";
import { CustomerBackfillButton } from "@/components/CustomerActions";
import { formatMoney } from "@/lib/format";
import type { CustomerSummaryDto } from "@/lib/types";

export const metadata = {
  title: "Customers — Nika Admin",
};

function ActiveBadge({ active }: { active: boolean }) {
  return (
    <span className={`inline-block px-2 py-0.5 text-xs font-medium ${active ? "bg-success/15 text-success" : "bg-danger/15 text-danger"}`}>
      {active ? "Active" : "Inactive"}
    </span>
  );
}

export default async function CustomersPage({
  searchParams,
}: {
  searchParams: Promise<{ search?: string; activeOnly?: string }>;
}) {
  const params = await searchParams;
  const search = params.search?.trim() ?? "";
  const activeOnly = params.activeOnly === "true";
  const customers = await getCustomers({ search, activeOnly });
  const items: CustomerSummaryDto[] = customers.data?.items ?? [];

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold sm:text-3xl">Customers</h1>
          <p className="mt-1 text-sm text-muted">Manage CRM records, order history, and receivables.</p>
        </div>
        <div className="flex flex-wrap items-start gap-2">
          <CustomerBackfillButton />
          <Link href="/customers/new" className="bg-accent-strong px-4 py-2 font-semibold text-white transition hover:bg-accent">
            + New customer
          </Link>
        </div>
      </div>

      <form className="grid gap-3 border border-border-soft bg-surface p-4 sm:grid-cols-[1fr_auto_auto]">
        <input name="search" defaultValue={search} placeholder="Search customers" className="w-full border border-border-soft bg-surface-2 px-3 py-2 text-sm outline-none focus:border-accent" />
        <label className="flex items-center gap-2 text-sm text-muted">
          <input type="checkbox" name="activeOnly" value="true" defaultChecked={activeOnly} />
          Active only
        </label>
        <button type="submit" className="border border-border-soft px-4 py-2 text-sm text-muted transition hover:border-accent hover:text-accent">
          Filter
        </button>
      </form>

      {customers.error ? (
        <p className="border border-danger/40 bg-danger/10 p-4 text-sm text-danger">{customers.error}</p>
      ) : items.length === 0 ? (
        <p className="border border-border-soft bg-surface p-6 text-sm text-muted">
          No customers found. Use <Link href="/customers/new" className="text-accent underline">New customer</Link> to add your first CRM record.
        </p>
      ) : (
        <div className="overflow-x-auto border border-border-soft">
          <table className="w-full text-left text-sm">
            <thead className="bg-surface-2 text-muted">
              <tr>
                <th className="px-4 py-3 font-medium">Name</th>
                <th className="px-4 py-3 font-medium">Contact</th>
                <th className="px-4 py-3 text-right font-medium">Orders</th>
                <th className="px-4 py-3 text-right font-medium">Outstanding</th>
                <th className="px-4 py-3 font-medium">Status</th>
              </tr>
            </thead>
            <tbody>
              {items.map((customer) => (
                <tr key={customer.id} className="border-t border-border-soft bg-surface">
                  <td className="px-4 py-3">
                    <Link href={`/customers/${customer.id}`} className="font-medium text-accent underline">{customer.name}</Link>
                    {customer.city ? <p className="text-xs text-muted">{customer.city}</p> : null}
                  </td>
                  <td className="px-4 py-3 text-muted">{customer.email ?? customer.phone ?? "—"}</td>
                  <td className="px-4 py-3 text-right">{customer.orderCount}</td>
                  <td className="px-4 py-3 text-right">{formatMoney(customer.outstandingBalance, customer.currency)}</td>
                  <td className="px-4 py-3"><ActiveBadge active={customer.isActive} /></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
