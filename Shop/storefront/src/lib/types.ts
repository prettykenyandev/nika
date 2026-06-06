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

export interface ProductDetailDto {
  id: string;
  name: string;
  slug: string;
  description: string;
  categoryId: string;
  categoryName: string;
  imageUrls: string[];
  variants: ProductVariantDto[];
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasNextPage: boolean;
}

export interface CartItemDto {
  productId: string;
  productVariantId: string;
  productName: string;
  variantName: string;
  slug: string;
  sku: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
  imageUrl: string | null;
  availableStock: number;
}

export interface CartDto {
  cartId: string;
  items: CartItemDto[];
  subtotal: number;
  currency: string;
  itemCount: number;
}

export type OrderStatus =
  | "PendingPayment"
  | "Paid"
  | "Fulfilled"
  | "Cancelled"
  | "PaymentFailed";

export interface OrderItemDto {
  productName: string;
  sku: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

export interface OrderDto {
  id: string;
  orderNumber: string;
  customerEmail: string;
  status: OrderStatus;
  total: number;
  currency: string;
  createdAtUtc: string;
  paidAtUtc: string | null;
  items: OrderItemDto[];
}

export interface CheckoutResult {
  orderId: string;
  orderNumber: string;
  paymentId: string;
  paymentInitiated: boolean;
  message: string;
}
