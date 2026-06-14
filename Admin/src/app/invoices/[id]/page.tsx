import Link from "next/link";
import { notFound } from "next/navigation";
import { getInvoice } from "@/actions/invoices";
import { InvoiceActions } from "@/components/InvoiceActions";
import { formatDate, formatMoney } from "@/lib/format";

export const metadata = {
  title: "Invoice — Nika Admin",
};

export default async function InvoiceDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const invoice = await getInvoice(id);
  if (!invoice) notFound();

  const statusLabel = invoice.status.replace(/([A-Z])/g, " $1").trim();
  const showOverdue =
    invoice.isOverdue && invoice.status !== "Paid" && invoice.status !== "Void";

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Link href="/invoices" className="text-sm text-accent underline">
          ← Back to invoices
        </Link>
        <div className="mt-2 flex flex-wrap items-center justify-between gap-3">
          <div>
            <h1 className="text-2xl font-bold sm:text-3xl">{invoice.invoiceNumber}</h1>
            <p className="mt-1 text-sm text-muted">
              {invoice.customerName}
              {invoice.customerEmail ? ` · ${invoice.customerEmail}` : ""}
            </p>
          </div>
          <span
            className={`px-3 py-1 text-sm font-medium ${
              showOverdue ? "bg-danger/15 text-danger" : "bg-surface-2 text-muted"
            }`}
          >
            {showOverdue ? "Overdue" : statusLabel}
          </span>
        </div>
      </div>

      <div className="grid gap-6 lg:grid-cols-[1.6fr_1fr]">
        <div className="flex flex-col gap-6">
          {/* Lines */}
          <section className="border border-border-soft bg-surface">
            <div className="overflow-x-auto">
              <table className="w-full text-left text-sm">
                <thead className="bg-surface-2 text-muted">
                  <tr>
                    <th className="px-4 py-3 font-medium">Description</th>
                    <th className="px-4 py-3 text-right font-medium">Qty</th>
                    <th className="px-4 py-3 text-right font-medium">Unit</th>
                    <th className="px-4 py-3 text-right font-medium">Tax</th>
                    <th className="px-4 py-3 text-right font-medium">Total</th>
                  </tr>
                </thead>
                <tbody>
                  {invoice.lines.map((l) => (
                    <tr key={l.id} className="border-t border-border-soft">
                      <td className="px-4 py-3">{l.description}</td>
                      <td className="px-4 py-3 text-right">{l.quantity}</td>
                      <td className="px-4 py-3 text-right">
                        {formatMoney(l.unitPrice, invoice.currency)}
                      </td>
                      <td className="px-4 py-3 text-right text-muted">
                        {l.taxPercent}%
                      </td>
                      <td className="px-4 py-3 text-right">
                        {formatMoney(l.lineTotal, invoice.currency)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <div className="flex flex-col items-end gap-1 border-t border-border-soft p-4 text-sm">
              <Row label="Subtotal" value={formatMoney(invoice.subtotal, invoice.currency)} />
              <Row label="Tax" value={formatMoney(invoice.taxTotal, invoice.currency)} />
              <Row label="Total" value={formatMoney(invoice.total, invoice.currency)} strong />
              <Row label="Received" value={formatMoney(invoice.amountPaid, invoice.currency)} />
              <Row
                label="Amount due"
                value={formatMoney(invoice.amountDue, invoice.currency)}
                strong
              />
            </div>
          </section>

          {/* Receipts */}
          {invoice.payments.length > 0 ? (
            <section className="border border-border-soft bg-surface">
              <h2 className="border-b border-border-soft px-4 py-3 text-sm font-semibold">
                Receipts
              </h2>
              <table className="w-full text-left text-sm">
                <thead className="bg-surface-2 text-muted">
                  <tr>
                    <th className="px-4 py-2 font-medium">Date</th>
                    <th className="px-4 py-2 font-medium">Method</th>
                    <th className="px-4 py-2 font-medium">Reference</th>
                    <th className="px-4 py-2 text-right font-medium">Amount</th>
                  </tr>
                </thead>
                <tbody>
                  {invoice.payments.map((p) => (
                    <tr key={p.id} className="border-t border-border-soft">
                      <td className="px-4 py-2">{formatDate(p.receivedOn)}</td>
                      <td className="px-4 py-2">{p.method}</td>
                      <td className="px-4 py-2 text-muted">{p.reference ?? "—"}</td>
                      <td className="px-4 py-2 text-right">
                        {formatMoney(p.amount, invoice.currency)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </section>
          ) : null}
        </div>

        {/* Side panel */}
        <div className="flex flex-col gap-6">
          <section className="flex flex-col gap-3 border border-border-soft bg-surface p-4">
            <h2 className="text-sm font-semibold">Details</h2>
            <Detail label="Issue date" value={formatDate(invoice.issueDate)} />
            <Detail label="Due date" value={formatDate(invoice.dueDate)} />
            {invoice.orderId ? <Detail label="From order" value={invoice.orderId} /> : null}
            {invoice.notes ? <Detail label="Notes" value={invoice.notes} /> : null}
          </section>

          <section className="border border-border-soft bg-surface p-4">
            <InvoiceActions
              invoiceId={invoice.id}
              status={invoice.status}
              amountDue={invoice.amountDue}
              currency={invoice.currency}
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
