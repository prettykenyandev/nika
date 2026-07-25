import type { CreateProductInput, VariantInput } from "@/lib/types";

/** Convert free text to a URL-friendly slug (matches the API's slug rules). */
export function toSlug(value: string): string {
  return value
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
}

/** Trim image URLs and drop the blank ones. */
export function normalizeImageUrls(imageUrls: string[]): string[] {
  return imageUrls.map((url) => url.trim()).filter(Boolean);
}

export interface NormalizedVariant {
  sku: string;
  name: string;
  price: number;
  stockQuantity: number;
}

/**
 * Trim/parse each variant and drop rows the operator left entirely blank
 * (no SKU and no name), so trailing empty form rows are ignored.
 */
export function normalizeVariants(variants: VariantInput[]): NormalizedVariant[] {
  return variants
    .map((variant) => ({
      sku: variant.sku.trim(),
      name: variant.name.trim(),
      price: Number(variant.price),
      stockQuantity: Math.trunc(Number(variant.stockQuantity)),
    }))
    .filter((variant) => variant.sku || variant.name);
}

export interface CreateProductPayload {
  name: string;
  description: string;
  categoryId: string;
  imageUrls: string[];
  variants: NormalizedVariant[];
  publish: boolean;
}

export type BuildPayloadResult =
  | { ok: true; payload: CreateProductPayload }
  | { ok: false; error: string };

/**
 * Validate and normalise the product form into the request payload. Returns the
 * first validation error (in field order) instead of throwing, so the server
 * action can surface it directly to the form.
 */
export function buildCreateProductPayload(input: CreateProductInput): BuildPayloadResult {
  if (!input.name.trim()) return { ok: false, error: "Product name is required." };
  if (!input.description.trim()) return { ok: false, error: "Description is required." };
  if (!input.categoryId) return { ok: false, error: "Please choose a category." };

  const imageUrls = normalizeImageUrls(input.imageUrls);
  const variants = normalizeVariants(input.variants);

  if (variants.length === 0) {
    return { ok: false, error: "Add at least one variant (SKU, name, price, stock)." };
  }

  for (const variant of variants) {
    if (!variant.sku) return { ok: false, error: "Every variant needs a SKU." };
    if (!variant.name) return { ok: false, error: `Variant ${variant.sku} needs a name.` };
    if (!Number.isFinite(variant.price) || variant.price <= 0) {
      return { ok: false, error: `Variant ${variant.sku} needs a price greater than 0.` };
    }
    if (!Number.isFinite(variant.stockQuantity) || variant.stockQuantity < 0) {
      return { ok: false, error: `Variant ${variant.sku} needs a stock quantity of 0 or more.` };
    }
  }

  return {
    ok: true,
    payload: {
      name: input.name.trim(),
      description: input.description.trim(),
      categoryId: input.categoryId,
      imageUrls,
      variants,
      publish: input.publish,
    },
  };
}
