import Link from "next/link";
import { fetchCategories } from "@/lib/api";
import { ProductForm } from "@/components/ProductForm";
import type { CategoryDto } from "@/lib/types";

export const metadata = {
  title: "New product — Nika Admin",
};

export default async function NewProductPage() {
  let categories: CategoryDto[] = [];
  let loadError: string | null = null;

  try {
    categories = await fetchCategories();
  } catch {
    loadError = "Could not load categories. Is the API running?";
  }

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Link href="/" className="text-sm text-muted transition hover:text-accent">
          &larr; Back to inventory
        </Link>
        <h1 className="mt-2 text-2xl font-bold sm:text-3xl">Add a new product</h1>
        <p className="mt-1 text-sm text-muted">
          Published products appear on the storefront immediately.
        </p>
      </div>

      {loadError ? (
        <p className="border border-danger/40 bg-danger/10 p-4 text-sm text-danger">
          {loadError}
        </p>
      ) : (
        <ProductForm categories={categories} />
      )}
    </div>
  );
}
