import { getProducts } from "@/lib/api";
import { ProductCard } from "@/components/ProductCard";

export async function ProductGrid({
  category,
  search,
  page,
}: {
  category?: string;
  search?: string;
  page?: number;
}) {
  let items: Awaited<ReturnType<typeof getProducts>>["items"] = [];
  try {
    const result = await getProducts({ category, search, page });
    items = result.items;
  } catch {
    // API unavailable (e.g. during a build with no backend). Fall through to the empty state.
  }

  if (items.length === 0) {
    return (
      <p className="rounded-lg border border-neutral-800 p-8 text-center text-neutral-400">
        No products found. Try a different search or category.
      </p>
    );
  }

  return (
    <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
      {items.map((product) => (
        <ProductCard key={product.id} product={product} />
      ))}
    </div>
  );
}
