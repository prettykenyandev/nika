import Link from "next/link";
import { notFound } from "next/navigation";
import { getPurchaseOrder } from "@/actions/purchaseOrders";
import { PurchaseOrderActions } from "@/components/PurchaseOrderActions";
import { formatDate, formatMoney } from "@/lib/format";

export const metadata = {
  title: "Purchase order — Nika Admin",
};

export default async function PurchaseOrderDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const po = await getPurchaseOrder(id);
  if (!po) notFound();

  const statusLabel = po.status.replace(/([A-Z])/g, " $1").trim();

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Link href="/purchase-orders" className="text-sm text-accent underline">
          ← Back to purchasing
        </Link>
        <div className="mt-2 flex flex-wrap items-center justify-between gap-3">
          <div>
            <h1 className="text-2xl font-bold sm:text-3xl">{po.poNumber}</h1>
            <p className="mt-1 text-sm text-muted">{po.vendorName}</p>
          </div>
          <span className="bg-surface-2 px-3 py-1 text-sm font-medium text-muted">
            {statusLabel}
          </span>
        </div>
      </div>

      <div className="grid gap-6 lg:grid-cols-[1.6fr_1fr]">
        <div className="flex flex-col gap-6">
          <section className="border border-border-soft bg-surface">
            <div className="overflow-x-auto">
              <table className="w-full text-left text-sm">
                <thead className="bg-surface-2 text-muted">
                  <tr>
                    <th className="px-4 py-3 font-medium">Description</th>
                    <th className="px-4 py-3 font-medium">SKU</th>
                    <th className="px-4 py-3 text-right font-medium">Ordered</th>
                    <th className="px-4 py-3 text-right font-medium">Received</th>
                    <th className="px-4 py-3 text-right font-medium">Outstanding</th>
                    <th className="px-4 py-3 text-right font-medium">Unit</th>
                    <th className="px-4 py-3 text-right font-medium">Tax</th>
                    <th className="px-4 py-3 text-right font-medium">Total</th>
                  </tr>
                </thead>
                <tbody>
                  {po.lines.map((l) => (
                    <tr key={l.id} className="border-t border-border-soft">
                      <td className="px-4 py-3">{l.description}</td>
                      <td className="px-4 py-3 text-muted">{l.sku ?? "—"}</td>
                      <td className="px-4 py-3 text-right">{l.quantity}</td>
                      <td className="px-4 py-3 text-right">{l.quantityReceived}</td>
                      <td className="px-4 py-3 text-right">
                        {l.quantityOutstanding}
                      </td>
                      <td className="px-4 py-3 text-right">
                        {formatMoney(l.unitCost, po.currency)}
                      </td>
                      <td className="px-4 py-3 text-right text-muted">
                        {l.taxPercent}%
                      </td>
                      <td className="px-4 py-3 text-right">
                        {formatMoney(l.lineTotal, po.currency)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <div className="flex flex-col items-end gap-1 border-t border-border-soft p-4 text-sm">
              <Row label="Subtotal" value={formatMoney(po.subtotal, po.currency)} />
              <Row label="Tax" value={formatMoney(po.taxTotal, po.currency)} />
              <Row label="Total" value={formatMoney(po.total, po.currency)} strong />
            </div>
          </section>
        </div>

        <div className="flex flex-col gap-6">
          <section className="flex flex-col gap-3 border border-border-soft bg-surface p-4">
            <h2 className="text-sm font-semibold">Details</h2>
            <Detail label="Vendor" value={po.vendorName} />
            <Detail label="Order date" value={formatDate(po.orderDate)} />
            {po.expectedDate ? (
              <Detail label="Expected date" value={formatDate(po.expectedDate)} />
            ) : null}
            {po.notes ? <Detail label="Notes" value={po.notes} /> : null}
            {po.generatedBillId ? (
              <Link
                href={`/expenses/${po.generatedBillId}`}
                className="text-sm text-accent underline"
              >
                View generated bill
              </Link>
            ) : null}
          </section>

          <section className="border border-border-soft bg-surface p-4">
            <PurchaseOrderActions
              orderId={po.id}
              status={po.status}
              currency={po.currency}
              lines={po.lines}
            />
          </section>
        </div>
      </div>
    </div>
  );
}

function Row({
  label,
  value,
  strong,
}: {
  label: string;
  value: string;
  strong?: boolean;
}) {
  return (
    <div
      className={`flex w-full max-w-xs justify-between ${
        strong ? "font-semibold" : "text-muted"
      }`}
    >
      <span>{label}</span>
      <span>{value}</span>
    </div>
  );
}

function Detail({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex flex-col gap-0.5">
      <span className="text-xs uppercase tracking-wide text-muted">{label}</span>
      <span className="break-words text-sm">{value}</span>
    </div>
  );
}
