import type {
  CategoryDto,
  PagedResult,
  ProductDetailDto,
  ProductSummaryDto,
} from "@/lib/types";

export const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5087";

async function getJson<T>(path: string, revalidateSeconds: number): Promise<T> {
  const response = await fetch(`${API_URL}${path}`, {
    next: { revalidate: revalidateSeconds },
    headers: { Accept: "application/json" },
  });

  if (!response.ok) {
    throw new Error(`Request to ${path} failed with ${response.status}`);
  }

  return (await response.json()) as T;
}

export function getProducts(params: {
  category?: string;
  search?: string;
  page?: number;
}): Promise<PagedResult<ProductSummaryDto>> {
  const query = new URLSearchParams();
  if (params.category) query.set("category", params.category);
  if (params.search) query.set("search", params.search);
  if (params.page) query.set("page", String(params.page));

  const suffix = query.toString() ? `?${query.toString()}` : "";
  return getJson<PagedResult<ProductSummaryDto>>(`/api/catalog/products${suffix}`, 60);
}

export function getProductBySlug(slug: string): Promise<ProductDetailDto> {
  return getJson<ProductDetailDto>(`/api/catalog/products/${encodeURIComponent(slug)}`, 60);
}

export function getCategories(): Promise<CategoryDto[]> {
  return getJson<CategoryDto[]>("/api/catalog/categories", 300);
}
