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

export interface PosVariant {
  variantId: string;
  sku: string;
  variantName: string;
  productId: string;
  productName: string;
  price: number;
  currency: string;
  stockQuantity: number;
  imageUrl: string | null;
}

export interface PosSaleResult {
  orderId: string;
  orderNumber: string;
  total: number;
  currency: string;
  amountTendered: number | null;
  change: number | null;
  paidAtUtc: string;
}
