import Link from "next/link";
import { fetchProducts } from "@/lib/api";
import type { ProductSummaryDto } from "@/lib/types";

const STOREFRONT_URL =
  process.env.NEXT_PUBLIC_STOREFRONT_URL ?? "http://localhost:3000";

function formatPrice(amount: number, currency: string): string {
  const symbol = currency === "KES" ? "Ksh" : currency;
  return `${symbol} ${amount.toLocaleString()}`;
}

export default async function DashboardPage() {
  let products: ProductSummaryDto[] = [];
  let loadError: string | null = null;

  try {
    const result = await fetchProducts();
    products = result.items;
  } catch {
    loadError = "Could not load products. Is the API running?";
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold sm:text-3xl">Inventory</h1>
          <p className="mt-1 text-sm text-muted">
            {products.length} live product{products.length === 1 ? "" : "s"} on
            the storefront.
          </p>
        </div>
        <Link
          href="/products/new"
          className="bg-accent-strong px-4 py-2 font-semibold text-white transition hover:bg-accent"
        >
          + Add product
        </Link>
      </div>

      {loadError ? (
        <p className="border border-danger/40 bg-danger/10 p-4 text-sm text-danger">
          {loadError}
        </p>
      ) : products.length === 0 ? (
        <p className="border border-border-soft bg-surface p-6 text-sm text-muted">
          No published products yet. Use{" "}
          <Link href="/products/new" className="text-accent underline">
            Add product
          </Link>{" "}
          to create your first item.
        </p>
      ) : (
        <>
          {/* Mobile: stacked cards */}
          <ul className="flex flex-col gap-3 md:hidden">
            {products.map((p) => (
              <li
                key={p.id}
                className="flex items-center gap-3 border border-border-soft bg-surface p-3"
              >
                <Thumb url={p.primaryImageUrl} name={p.name} />
                <div className="min-w-0 flex-1">
                  <p className="truncate font-semibold">{p.name}</p>
                  <p className="text-xs text-muted">{p.categoryName}</p>
                  <p className="mt-1 text-sm text-accent">
                    From {formatPrice(p.fromPrice, p.currency)}
                  </p>
                </div>
                <StockBadge inStock={p.inStock} />
              </li>
            ))}
          </ul>

          {/* Desktop: table */}
          <div className="hidden overflow-x-auto border border-border-soft md:block">
            <table className="w-full text-left text-sm">
              <thead className="bg-surface-2 text-muted">
                <tr>
                  <th className="px-4 py-3 font-medium">Product</th>
                  <th className="px-4 py-3 font-medium">Category</th>
                  <th className="px-4 py-3 font-medium">From price</th>
                  <th className="px-4 py-3 font-medium">Stock</th>
                  <th className="px-4 py-3 font-medium">View</th>
                </tr>
              </thead>
              <tbody>
                {products.map((p) => (
                  <tr
                    key={p.id}
                    className="border-t border-border-soft bg-surface"
                  >
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-3">
                        <Thumb url={p.primaryImageUrl} name={p.name} />
                        <span className="font-medium">{p.name}</span>
                      </div>
                    </td>
                    <td className="px-4 py-3 text-muted">{p.categoryName}</td>
                    <td className="px-4 py-3 text-accent">
                      {formatPrice(p.fromPrice, p.currency)}
                    </td>
                    <td className="px-4 py-3">
                      <StockBadge inStock={p.inStock} />
                    </td>
                    <td className="px-4 py-3">
                      <a
                        href={`${STOREFRONT_URL}/products/${p.slug}`}
                        target="_blank"
                        rel="noreferrer"
                        className="text-accent underline"
                      >
                        Open
                      </a>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}
    </div>
  );
}

function Thumb({ url, name }: { url: string | null; name: string }) {
  if (!url) {
    return (
      <div className="flex h-12 w-12 shrink-0 items-center justify-center bg-surface-2 text-[10px] text-muted">
        No image
      </div>
    );
  }
  return (
    // eslint-disable-next-line @next/next/no-img-element
    <img
      src={url}
      alt={name}
      className="h-12 w-12 shrink-0 object-cover"
    />
  );
}

function StockBadge({ inStock }: { inStock: boolean }) {
  return (
    <span
      className={`inline-block px-2 py-0.5 text-xs font-medium ${
        inStock
          ? "bg-success/15 text-success"
          : "bg-danger/15 text-danger"
      }`}
    >
      {inStock ? "In stock" : "Out of stock"}
    </span>
  );
}
