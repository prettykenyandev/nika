import { Suspense } from "react";
import Link from "next/link";
import { ProductGrid } from "@/components/ProductGrid";
import { ProductGridSkeleton } from "@/components/ProductGridSkeleton";

export default function HomePage() {
  return (
    <div className="flex flex-col gap-12">
      <section className="relative overflow-hidden rounded-2xl border border-border-soft bg-gradient-to-br from-surface-2 via-surface to-background p-10 text-center sm:p-16">
        <span className="inline-block rounded-full border border-accent/30 bg-accent/10 px-3 py-1 text-xs font-semibold uppercase tracking-wide text-accent">
          Gym clothes &amp; accessories
        </span>
        <h1 className="mt-4 text-4xl font-black tracking-tight sm:text-6xl">
          Train like you mean it.
        </h1>
        <p className="mx-auto mt-4 max-w-xl text-muted">
          Premium gym apparel and accessories engineered for results. Pay fast
          with M-Pesa.
        </p>
        <Link
          href="/products"
          className="mt-8 inline-block rounded-full bg-accent-strong px-8 py-3 font-semibold text-white shadow-lg shadow-accent-strong/30 transition hover:bg-accent"
        >
          Shop the collection
        </Link>
      </section>

      <section className="flex flex-col gap-6">
        <h2 className="text-2xl font-bold">Latest drops</h2>
        <Suspense fallback={<ProductGridSkeleton />}>
          <ProductGrid />
        </Suspense>
      </section>
    </div>
  );
}
