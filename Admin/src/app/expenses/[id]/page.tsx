import Link from "next/link";
import { notFound } from "next/navigation";
import { getBill } from "@/actions/expenses";
import { BillActions } from "@/components/BillActions";
import { formatDate, formatMoney } from "@/lib/format";

export const metadata = {
  title: "Bill — Nika Admin",
};

export default async function BillDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const bill = await getBill(id);
  if (!bill) notFound();

  const statusLabel = bill.status.replace(/([A-Z])/g, " $1").trim();

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Link href="/expenses" className="text-sm text-accent underline">
          ← Back to expenses
        </Link>
        <div className="mt-2 flex flex-wrap items-center justify-between gap-3">
          <div>
            <h1 className="text-2xl font-bold sm:text-3xl">{bill.billNumber}</h1>
            <p className="mt-1 text-sm text-muted">
              {bill.vendorName}
              {bill.supplierReference ? ` · Ref ${bill.supplierReference}` : ""}
            </p>
          </div>
          <span
            className={`px-3 py-1 text-sm font-medium ${
              bill.isOverdue && bill.status !== "Paid" && bill.status !== "Cancelled"
                ? "bg-danger/15 text-danger"
                : "bg-surface-2 text-muted"
            }`}
          >
            {bill.isOverdue && bill.status !== "Paid" && bill.status !== "Cancelled"
              ? "Overdue"
              : statusLabel}
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
                    <th className="px-4 py-3 font-medium">Category</th>
                    <th className="px-4 py-3 text-right font-medium">Qty</th>
                    <th className="px-4 py-3 text-right font-medium">Unit</th>
                    <th className="px-4 py-3 text-right font-medium">Tax</th>
                    <th className="px-4 py-3 text-right font-medium">Total</th>
                  </tr>
                </thead>
                <tbody>
                  {bill.lines.map((l) => (
                    <tr key={l.id} className="border-t border-border-soft">
                      <td className="px-4 py-3">{l.description}</td>
                      <td className="px-4 py-3 text-muted">
                        {l.expenseCategoryName ?? "—"}
                      </td>
                      <td className="px-4 py-3 text-right">{l.quantity}</td>
                      <td className="px-4 py-3 text-right">
                        {formatMoney(l.unitCost, bill.currency)}
                      </td>
                      <td className="px-4 py-3 text-right text-muted">
                        {l.taxPercent}%
                      </td>
                      <td className="px-4 py-3 text-right">
                        {formatMoney(l.lineTotal, bill.currency)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
            <div className="flex flex-col items-end gap-1 border-t border-border-soft p-4 text-sm">
              <Row label="Subtotal" value={formatMoney(bill.subtotal, bill.currency)} />
              <Row label="Tax" value={formatMoney(bill.taxTotal, bill.currency)} />
              <Row label="Total" value={formatMoney(bill.total, bill.currency)} strong />
              <Row label="Paid" value={formatMoney(bill.amountPaid, bill.currency)} />
              <Row
                label="Amount due"
                value={formatMoney(bill.amountDue, bill.currency)}
                strong
              />
            </div>
          </section>

          {/* Payments */}
          {bill.payments.length > 0 ? (
            <section className="border border-border-soft bg-surface">
              <h2 className="border-b border-border-soft px-4 py-3 text-sm font-semibold">
                Payments
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
                  {bill.payments.map((p) => (
                    <tr key={p.id} className="border-t border-border-soft">
                      <td className="px-4 py-2">{formatDate(p.paidOn)}</td>
                      <td className="px-4 py-2">{p.method}</td>
                      <td className="px-4 py-2 text-muted">{p.reference ?? "—"}</td>
                      <td className="px-4 py-2 text-right">
                        {formatMoney(p.amount, bill.currency)}
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
            <Detail label="Issue date" value={formatDate(bill.issueDate)} />
            <Detail label="Due date" value={formatDate(bill.dueDate)} />
            {bill.notes ? <Detail label="Notes" value={bill.notes} /> : null}
            {bill.attachmentUrl ? (
              <a
                href={bill.attachmentUrl}
                target="_blank"
                rel="noreferrer"
                className="text-sm text-accent underline"
              >
                View receipt
              </a>
            ) : null}
          </section>

          <section className="border border-border-soft bg-surface p-4">
            <BillActions
              billId={bill.id}
              status={bill.status}
              amountDue={bill.amountDue}
              currency={bill.currency}
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
      <span className="text-sm">{value}</span>
    </div>
  );
}
