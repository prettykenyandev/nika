import Link from "next/link";
import { getPurchaseOrders } from "@/actions/purchaseOrders";
import { formatDate, formatMoney } from "@/lib/format";
import type { PurchaseOrderSummaryDto } from "@/lib/types";

export const metadata = {
  title: "Purchasing — Nika Admin",
};

const STATUS_STYLES: Record<string, string> = {
  Draft: "bg-surface-2 text-muted",
  Sent: "bg-warning/15 text-warning",
  PartiallyReceived: "bg-accent/15 text-accent",
  Received: "bg-success/15 text-success",
  Closed: "bg-success/15 text-success",
  Cancelled: "bg-danger/15 text-danger",
};

function StatusBadge({ status }: { status: string }) {
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

export default async function PurchaseOrdersPage({
  searchParams,
}: {
  searchParams: Promise<{ status?: string; search?: string }>;
}) {
  const params = await searchParams;
  const status = params.status?.trim() ?? "";
  const search = params.search?.trim() ?? "";
  const purchaseOrders = await getPurchaseOrders({ status, search });
  const items: PurchaseOrderSummaryDto[] = purchaseOrders.data?.items ?? [];
  const loadError = purchaseOrders.error ?? null;

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold sm:text-3xl">Purchasing</h1>
          <p className="mt-1 text-sm text-muted">
            Create purchase orders, receive stock and generate supplier bills.
          </p>
        </div>
        <Link
          href="/purchase-orders/new"
          className="bg-accent-strong px-4 py-2 font-semibold text-white transition hover:bg-accent"
        >
          + New purchase order
        </Link>
      </div>

      <form className="grid gap-3 border border-border-soft bg-surface p-4 sm:grid-cols-[1fr_12rem_auto]">
        <input
          name="search"
          defaultValue={search}
          placeholder="Search purchase orders"
          className="w-full border border-border-soft bg-surface-2 px-3 py-2 text-sm outline-none focus:border-accent"
        />
        <select
          name="status"
          defaultValue={status}
          className="w-full border border-border-soft bg-surface-2 px-3 py-2 text-sm outline-none focus:border-accent"
        >
          <option value="">All statuses</option>
          <option value="Draft">Draft</option>
          <option value="Sent">Sent</option>
          <option value="PartiallyReceived">Partially received</option>
          <option value="Received">Received</option>
          <option value="Closed">Closed</option>
          <option value="Cancelled">Cancelled</option>
        </select>
        <button
          type="submit"
          className="border border-border-soft px-4 py-2 text-sm text-muted transition hover:border-accent hover:text-accent"
        >
          Filter
        </button>
      </form>

      {loadError ? (
        <p className="border border-danger/40 bg-danger/10 p-4 text-sm text-danger">
          {loadError}
        </p>
      ) : items.length === 0 ? (
        <p className="border border-border-soft bg-surface p-6 text-sm text-muted">
          No purchase orders found. Use{" "}
          <Link href="/purchase-orders/new" className="text-accent underline">
            New purchase order
          </Link>{" "}
          to order stock.
        </p>
      ) : (
        <div className="overflow-x-auto border border-border-soft">
          <table className="w-full text-left text-sm">
            <thead className="bg-surface-2 text-muted">
              <tr>
                <th className="px-4 py-3 font-medium">PO</th>
                <th className="px-4 py-3 font-medium">Vendor</th>
                <th className="px-4 py-3 font-medium">Order date</th>
                <th className="px-4 py-3 font-medium">Status</th>
                <th className="px-4 py-3 text-right font-medium">Total</th>
              </tr>
            </thead>
            <tbody>
              {items.map((po) => (
                <tr key={po.id} className="border-t border-border-soft bg-surface">
                  <td className="px-4 py-3">
                    <Link
                      href={`/purchase-orders/${po.id}`}
                      className="font-medium text-accent underline"
                    >
                      {po.poNumber}
                    </Link>
                  </td>
                  <td className="px-4 py-3">{po.vendorName}</td>
                  <td className="px-4 py-3 text-muted">{formatDate(po.orderDate)}</td>
                  <td className="px-4 py-3">
                    <StatusBadge status={po.status} />
                  </td>
                  <td className="px-4 py-3 text-right">
                    {formatMoney(po.total, po.currency)}
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
