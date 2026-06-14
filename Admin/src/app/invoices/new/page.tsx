import Link from "next/link";
import { getCustomer } from "@/actions/customers";
import { getCompanySettings } from "@/actions/settings";
import { InvoiceForm } from "@/components/InvoiceForm";

export const metadata = {
  title: "New invoice — Nika Admin",
};

export default async function NewInvoicePage({
  searchParams,
}: {
  searchParams: Promise<{ customerId?: string }>;
}) {
  const params = await searchParams;
  const [settings, customer] = await Promise.all([
    getCompanySettings(),
    params.customerId ? getCustomer(params.customerId) : Promise.resolve(null),
  ]);

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Link href="/invoices" className="text-sm text-accent underline">
          ← Back to invoices
        </Link>
        <h1 className="mt-2 text-2xl font-bold sm:text-3xl">New invoice</h1>
        <p className="mt-1 text-sm text-muted">
          Bill a customer. Save it as a draft, then send it and record receipts.
        </p>
      </div>

      <InvoiceForm
        defaultCurrency={settings?.currency ?? "KES"}
        defaultTaxPercent={settings?.defaultTaxPercent ?? 16}
        customerId={customer?.id ?? null}
        defaultCustomerName={customer?.name ?? ""}
        defaultCustomerEmail={customer?.email ?? ""}
      />
    </div>
  );
}
