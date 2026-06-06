import Link from "next/link";
import type { ProductSummaryDto } from "@/lib/types";
import { formatMoney } from "@/lib/format";

export function ProductCard({ product }: { product: ProductSummaryDto }) {
  return (
    <Link
      href={`/products/${product.slug}`}
      className="group flex flex-col overflow-hidden rounded-xl border border-border-soft bg-surface transition hover:border-accent hover:shadow-lg hover:shadow-accent-strong/10"
    >
      <div className="relative aspect-square overflow-hidden bg-surface-2">
        {product.primaryImageUrl ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img
            src={product.primaryImageUrl}
            alt={product.name}
            className="h-full w-full object-cover transition duration-300 group-hover:scale-105"
          />
        ) : (
          <div className="flex h-full w-full items-center justify-center bg-gradient-to-br from-surface-2 to-background px-4 text-center text-lg font-bold text-muted">
            {product.name}
          </div>
        )}
        {!product.inStock ? (
          <span className="absolute left-3 top-3 rounded bg-background/80 px-2 py-1 text-xs text-muted backdrop-blur">
            Sold out
          </span>
        ) : null}
      </div>
      <div className="flex flex-1 flex-col gap-1 p-4">
        <span className="text-xs uppercase tracking-wide text-muted">
          {product.categoryName}
        </span>
        <h3 className="font-semibold leading-snug">{product.name}</h3>
        <span className="mt-auto pt-2 font-bold text-accent">
          From {formatMoney(product.fromPrice, product.currency)}
        </span>
      </div>
    </Link>
  );
}
