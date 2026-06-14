import Link from "next/link";
import { getExpenseCategories } from "@/actions/expenses";
import { ExpenseCategoryManager } from "@/components/ExpenseCategoryManager";

export const metadata = {
  title: "Expense categories — Nika Admin",
};

export default async function ExpenseCategoriesPage() {
  const categories = await getExpenseCategories();

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Link href="/expenses" className="text-sm text-accent underline">
          ← Back to expenses
        </Link>
        <h1 className="mt-2 text-2xl font-bold sm:text-3xl">Expense categories</h1>
        <p className="mt-1 text-sm text-muted">
          Classify bills for reporting (e.g. Rent, Utilities, Salaries, Stock).
        </p>
      </div>

      <ExpenseCategoryManager initial={categories} />
    </div>
  );
}
