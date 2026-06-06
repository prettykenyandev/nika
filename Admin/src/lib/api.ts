import "server-only";
import type { CategoryDto, PagedResult, ProductSummaryDto } from "@/lib/types";

// Server-side only. All API calls happen from the Next.js server, so the
// .NET API never receives cross-origin browser requests (no CORS needed).
export const API_URL = process.env.API_URL ?? "http://localhost:5087";

export async function fetchCategories(): Promise<CategoryDto[]> {
  const res = await fetch(`${API_URL}/api/catalog/categories`, {
    cache: "no-store",
    headers: { Accept: "application/json" },
  });
  if (!res.ok) throw new Error(`Failed to load categories (${res.status})`);
  return (await res.json()) as CategoryDto[];
}

export async function fetchProducts(): Promise<PagedResult<ProductSummaryDto>> {
  const res = await fetch(`${API_URL}/api/catalog/products?pageSize=100`, {
    cache: "no-store",
    headers: { Accept: "application/json" },
  });
  if (!res.ok) throw new Error(`Failed to load products (${res.status})`);
  return (await res.json()) as PagedResult<ProductSummaryDto>;
}
