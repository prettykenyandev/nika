import { Suspense } from "react";
import Link from "next/link";
import { getCategories } from "@/lib/api";
import { ProductGrid } from "@/components/ProductGrid";
import { ProductGridSkeleton } from "@/components/ProductGridSkeleton";

export const metadata = {
  title: "Shop",
};

interface SearchParams {
  category?: string;
  search?: string;
  page?: string;
}

export default async function ProductsPage({
  searchParams,
}: {
  searchParams: Promise<SearchParams>;
}) {
  const { category, search, page } = await searchParams;
  const categories = await getCategories().catch(() => []);
  const pageNumber = page ? Number(page) : 1;

  // Key the Suspense boundary on the active filters so it re-suspends on change.
  const gridKey = `${category ?? "all"}-${search ?? ""}-${pageNumber}`;

  return (
    <div className="flex flex-col gap-8">
      <div className="flex flex-col gap-4">
        <h1 className="text-3xl font-bold">Shop</h1>

        <form action="/products" className="flex gap-2">
          {category ? <input type="hidden" name="category" value={category} /> : null}
          <input
            type="search"
            name="search"
            defaultValue={search ?? ""}
            placeholder="Search products..."
            className="flex-1 rounded-lg border border-border-soft bg-surface px-4 py-2 outline-none focus:border-accent"
          />
          <button
            type="submit"
            className="rounded-lg bg-accent-strong px-5 py-2 font-medium text-white transition hover:bg-accent"
          >
            Search
          </button>
        </form>

        <nav className="flex flex-wrap gap-2">
          <FilterPill href="/products" label="All" active={!category} />
          {categories.map((c) => (
            <FilterPill
              key={c.id}
              href={`/products?category=${c.slug}`}
              label={c.name}
              active={category === c.slug}
            />
          ))}
        </nav>
      </div>

      <Suspense key={gridKey} fallback={<ProductGridSkeleton />}>
        <ProductGrid category={category} search={search} page={pageNumber} />
      </Suspense>
    </div>
  );
}

function FilterPill({
  href,
  label,
  active,
}: {
  href: string;
  label: string;
  active: boolean;
}) {
  return (
    <Link
      href={href}
      className={`rounded-full border px-4 py-1.5 text-sm transition ${
        active
          ? "border-accent bg-accent-strong text-white"
          : "border-border-soft text-muted hover:border-accent hover:text-accent"
      }`}
    >
      {label}
    </Link>
  );
}
