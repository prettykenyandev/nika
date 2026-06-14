import Link from "next/link";
import { getCompanySettings } from "@/actions/settings";
import { getExpenseCategories } from "@/actions/expenses";
import { BillForm } from "@/components/BillForm";

export const metadata = {
  title: "New bill — Nika Admin",
};

export default async function NewBillPage() {
  const [categories, settings] = await Promise.all([
    getExpenseCategories(),
    getCompanySettings(),
  ]);

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Link href="/expenses" className="text-sm text-accent underline">
          ← Back to expenses
        </Link>
        <h1 className="mt-2 text-2xl font-bold sm:text-3xl">New bill</h1>
        <p className="mt-1 text-sm text-muted">
          Record a supplier cost. Save it as a draft, then approve and pay it.
        </p>
      </div>

      <BillForm
        categories={categories}
        defaultCurrency={settings?.currency ?? "KES"}
        defaultTaxPercent={settings?.defaultTaxPercent ?? 16}
      />
    </div>
  );
}
