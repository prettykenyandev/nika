export interface CategoryDto {
  id: string;
  name: string;
  slug: string;
  description: string | null;
}

export interface ProductVariantDto {
  id: string;
  sku: string;
  name: string;
  price: number;
  currency: string;
  stockQuantity: number;
  inStock: boolean;
}

export interface ProductSummaryDto {
  id: string;
  name: string;
  slug: string;
  categoryName: string;
  fromPrice: number;
  currency: string;
  primaryImageUrl: string | null;
  inStock: boolean;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
}

export interface AuthResponse {
  token: string;
  expiresAtUtc: string;
  email: string;
  fullName: string;
  roles: string[];
}

export interface VariantInput {
  sku: string;
  name: string;
  price: number;
  stockQuantity: number;
}

export interface CreateProductInput {
  name: string;
  description: string;
  categoryId: string;
  imageUrls: string[];
  variants: VariantInput[];
  publish: boolean;
}
