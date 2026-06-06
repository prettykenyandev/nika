import { notFound } from "next/navigation";
import type { Metadata } from "next";
import { getProductBySlug } from "@/lib/api";
import { AddToCartPanel } from "@/components/AddToCartPanel";

interface PageProps {
  params: Promise<{ slug: string }>;
}

export async function generateMetadata({ params }: PageProps): Promise<Metadata> {
  const { slug } = await params;
  try {
    const product = await getProductBySlug(slug);
    return {
      title: product.name,
      description: product.description,
    };
  } catch {
    return { title: "Product not found" };
  }
}

export default async function ProductDetailPage({ params }: PageProps) {
  const { slug } = await params;

  let product;
  try {
    product = await getProductBySlug(slug);
  } catch {
    notFound();
  }

  return (
    <div className="grid gap-10 lg:grid-cols-2">
      <div className="flex flex-col gap-3">
        <div className="aspect-square overflow-hidden rounded-2xl border border-border-soft bg-surface-2">
          {product.imageUrls.length > 0 ? (
            // eslint-disable-next-line @next/next/no-img-element
            <img
              src={product.imageUrls[0]}
              alt={product.name}
              className="h-full w-full object-cover"
            />
          ) : (
            <div className="flex h-full w-full items-center justify-center bg-gradient-to-br from-surface-2 to-background p-8 text-center text-2xl font-bold text-muted">
              {product.name}
            </div>
          )}
        </div>
        {product.imageUrls.length > 1 ? (
          <div className="grid grid-cols-4 gap-3">
            {product.imageUrls.slice(0, 4).map((url) => (
              // eslint-disable-next-line @next/next/no-img-element
              <img
                key={url}
                src={url}
                alt={product.name}
                className="aspect-square w-full rounded-lg border border-border-soft object-cover"
              />
            ))}
          </div>
        ) : null}
      </div>

      <div className="flex flex-col gap-6">
        <div className="flex flex-col gap-2">
          <span className="text-xs uppercase tracking-wide text-accent">
            {product.categoryName}
          </span>
          <h1 className="text-3xl font-bold">{product.name}</h1>
          <p className="text-muted">{product.description}</p>
        </div>

        <AddToCartPanel variants={product.variants} />
      </div>
    </div>
  );
}
