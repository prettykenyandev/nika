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
  method: string;
  status: string;
  message: string;
  paidAtUtc: string | null;
}

export interface CompanySettings {
  legalName: string;
  tradingName: string | null;
  email: string | null;
  phone: string | null;
  taxIdentifier: string | null;
  addressLine1: string | null;
  addressLine2: string | null;
  city: string | null;
  country: string | null;
  logoUrl: string | null;
  currency: string;
  defaultTaxPercent: number;
  invoiceNumberPrefix: string;
  billNumberPrefix: string;
  purchaseOrderNumberPrefix: string;
  invoiceFooter: string | null;
  paymentInstructions: string | null;
}

export interface ExpenseCategoryDto {
  id: string;
  name: string;
  slug: string;
  description: string | null;
}

export interface BillSummaryDto {
  id: string;
  billNumber: string;
  vendorName: string;
  issueDate: string;
  dueDate: string;
  currency: string;
  status: string;
  isOverdue: boolean;
  total: number;
  amountPaid: number;
  amountDue: number;
}

export interface BillLineDto {
  id: string;
  description: string;
  expenseCategoryId: string | null;
  expenseCategoryName: string | null;
  quantity: number;
  unitCost: number;
  taxPercent: number;
  lineNet: number;
  lineTax: number;
  lineTotal: number;
}

export interface BillPaymentDto {
  id: string;
  amount: number;
  paidOn: string;
  method: string;
  reference: string | null;
}

export interface BillDetailDto {
  id: string;
  billNumber: string;
  vendorName: string;
  vendorId: string | null;
  supplierReference: string | null;
  issueDate: string;
  dueDate: string;
  currency: string;
  status: string;
  isOverdue: boolean;
  notes: string | null;
  attachmentUrl: string | null;
  subtotal: number;
  taxTotal: number;
  total: number;
  amountPaid: number;
  amountDue: number;
  lines: BillLineDto[];
  payments: BillPaymentDto[];
}

export interface ApSummaryDto {
  currency: string;
  outstanding: number;
  overdue: number;
  openBillCount: number;
  overdueBillCount: number;
}

export interface BillLineInput {
  description: string;
  expenseCategoryId: string | null;
  quantity: number;
  unitCost: number;
  taxPercent: number | null;
}

export interface CreateBillInput {
  vendorName: string;
  issueDate: string;
  dueDate: string;
  currency: string | null;
  supplierReference: string | null;
  notes: string | null;
  attachmentUrl: string | null;
  lines: BillLineInput[];
}

export interface InvoiceSummaryDto {
  id: string;
  invoiceNumber: string;
  customerName: string;
  issueDate: string;
  dueDate: string;
  currency: string;
  status: string;
  isOverdue: boolean;
  total: number;
  amountPaid: number;
  amountDue: number;
}

export interface InvoiceLineDto {
  id: string;
  description: string;
  quantity: number;
  unitPrice: number;
  taxPercent: number;
  lineNet: number;
  lineTax: number;
  lineTotal: number;
}

export interface InvoicePaymentDto {
  id: string;
  amount: number;
  receivedOn: string;
  method: string;
  reference: string | null;
}

export interface InvoiceDetailDto {
  id: string;
  invoiceNumber: string;
  customerName: string;
  customerId: string | null;
  customerEmail: string | null;
  orderId: string | null;
  issueDate: string;
  dueDate: string;
  currency: string;
  status: string;
  isOverdue: boolean;
  notes: string | null;
  subtotal: number;
  taxTotal: number;
  total: number;
  amountPaid: number;
  amountDue: number;
  lines: InvoiceLineDto[];
  payments: InvoicePaymentDto[];
}

export interface ArSummaryDto {
  currency: string;
  outstanding: number;
  overdue: number;
  openInvoiceCount: number;
  overdueInvoiceCount: number;
}

export interface InvoiceLineInput {
  description: string;
  quantity: number;
  unitPrice: number;
  taxPercent: number | null;
}

export interface CreateInvoiceInput {
  customerName: string;
  customerEmail: string | null;
  issueDate: string;
  dueDate: string;
  currency: string | null;
  notes: string | null;
  lines: InvoiceLineInput[];
}
