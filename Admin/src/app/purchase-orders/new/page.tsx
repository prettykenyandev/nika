import Link from "next/link";
import { getCompanySettings } from "@/actions/settings";
import { getVariantOptions } from "@/actions/purchaseOrders";
import { getVendors } from "@/actions/vendors";
import { PurchaseOrderForm } from "@/components/PurchaseOrderForm";

export const metadata = {
  title: "New purchase order — Nika Admin",
};

export default async function NewPurchaseOrderPage() {
  const [vendors, variants, settings] = await Promise.all([
    getVendors({ activeOnly: true }),
    getVariantOptions(),
    getCompanySettings(),
  ]);

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Link href="/purchase-orders" className="text-sm text-accent underline">
          ← Back to purchasing
        </Link>
        <h1 className="mt-2 text-2xl font-bold sm:text-3xl">New purchase order</h1>
        <p className="mt-1 text-sm text-muted">
          Order inventory from a vendor, then receive goods and close the PO.
        </p>
      </div>

      {vendors.error ? (
        <p className="border border-danger/40 bg-danger/10 p-4 text-sm text-danger">
          {vendors.error}
        </p>
      ) : null}

      <PurchaseOrderForm
        vendors={vendors.data?.items ?? []}
        variants={variants}
        defaultCurrency={settings?.currency ?? "KES"}
        defaultTaxPercent={settings?.defaultTaxPercent ?? 16}
      />
    </div>
  );
}
