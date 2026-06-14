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

export interface VendorSummaryDto {
  id: string;
  name: string;
  contactName: string | null;
  email: string | null;
  phone: string | null;
  paymentTermDays: number;
  isActive: boolean;
}

export interface VendorDetailDto {
  id: string;
  name: string;
  contactName: string | null;
  email: string | null;
  phone: string | null;
  addressLine1: string | null;
  city: string | null;
  country: string | null;
  taxIdentifier: string | null;
  paymentTermDays: number;
  notes: string | null;
  isActive: boolean;
}

export interface VendorInput {
  name: string;
  contactName: string | null;
  email: string | null;
  phone: string | null;
  addressLine1: string | null;
  city: string | null;
  country: string | null;
  taxIdentifier: string | null;
  paymentTermDays: number;
  notes: string | null;
}

export interface VariantOptionDto {
  variantId: string;
  sku: string;
  label: string;
  stockQuantity: number;
  price: number;
  currency: string;
}

export interface PurchaseOrderSummaryDto {
  id: string;
  poNumber: string;
  vendorId: string;
  vendorName: string;
  orderDate: string;
  expectedDate: string | null;
  currency: string;
  status: string;
  total: number;
}

export interface PurchaseOrderLineDto {
  id: string;
  productVariantId: string | null;
  description: string;
  sku: string | null;
  quantity: number;
  quantityReceived: number;
  quantityOutstanding: number;
  unitCost: number;
  taxPercent: number;
  lineNet: number;
  lineTax: number;
  lineTotal: number;
}

export interface PurchaseOrderDetailDto {
  id: string;
  poNumber: string;
  vendorId: string;
  vendorName: string;
  orderDate: string;
  expectedDate: string | null;
  currency: string;
  status: string;
  notes: string | null;
  generatedBillId: string | null;
  subtotal: number;
  taxTotal: number;
  total: number;
  lines: PurchaseOrderLineDto[];
}

export interface PurchaseOrderLineInput {
  productVariantId: string | null;
  description: string;
  sku: string | null;
  quantity: number;
  unitCost: number;
  taxPercent: number | null;
}

export interface CreatePurchaseOrderInput {
  vendorId: string;
  orderDate: string;
  expectedDate: string | null;
  currency: string | null;
  notes: string | null;
  lines: PurchaseOrderLineInput[];
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
  customerId?: string | null;
  customerName: string;
  customerEmail: string | null;
  issueDate: string;
  dueDate: string;
  currency: string | null;
  notes: string | null;
  lines: InvoiceLineInput[];
}

export interface CustomerSummaryDto {
  id: string;
  name: string;
  email: string | null;
  phone: string | null;
  city: string | null;
  isActive: boolean;
  orderCount: number;
  outstandingBalance: number;
  currency: string;
}

export interface CustomerOrderDto {
  id: string;
  orderNumber: string;
  createdAtUtc: string;
  status: string;
  total: number;
  currency: string;
}

export interface CustomerInvoiceDto {
  id: string;
  invoiceNumber: string;
  issueDate: string;
  dueDate: string;
  status: string;
  isOverdue: boolean;
  total: number;
  amountDue: number;
  currency: string;
}

export interface CustomerDetailDto {
  id: string;
  name: string;
  email: string | null;
  phone: string | null;
  addressLine1: string | null;
  city: string | null;
  country: string | null;
  notes: string | null;
  isActive: boolean;
  currency: string;
  totalInvoiced: number;
  outstandingBalance: number;
  lifetimeOrderValue: number;
  orders: CustomerOrderDto[];
  invoices: CustomerInvoiceDto[];
}

export interface CustomerInput {
  name: string;
  email: string | null;
  phone: string | null;
  addressLine1: string | null;
  city: string | null;
  country: string | null;
  notes: string | null;
}

export interface DashboardSummaryDto {
  currency: string;
  revenueThisMonth: number;
  expensesThisMonth: number;
  netThisMonth: number;
  outstandingReceivables: number;
  overdueReceivables: number;
  outstandingPayables: number;
  overduePayables: number;
  lowStockCount: number;
  inventoryValue: number;
  openPurchaseOrders: number;
}

export interface ExpenseBreakdownDto {
  category: string;
  amount: number;
}

export interface ProfitAndLossDto {
  from: string;
  to: string;
  currency: string;
  orderRevenue: number;
  invoicedRevenue: number;
  totalRevenue: number;
  totalExpenses: number;
  netProfit: number;
  expensesByCategory: ExpenseBreakdownDto[];
}

export interface AgingBucketDto {
  label: string;
  amount: number;
  count: number;
}

export interface AgingReportDto {
  currency: string;
  total: number;
  buckets: AgingBucketDto[];
}

export interface TopProductDto {
  productId: string;
  productName: string;
  quantitySold: number;
  revenue: number;
}

export interface SalesTrendPointDto {
  period: string;
  revenue: number;
  orderCount: number;
}

export interface SalesAnalyticsDto {
  from: string;
  to: string;
  currency: string;
  totalSales: number;
  orderCount: number;
  topProducts: TopProductDto[];
  monthlyTrend: SalesTrendPointDto[];
}

export interface LowStockItemDto {
  productId: string;
  productName: string;
  variantId: string;
  sku: string;
  variantName: string;
  stockQuantity: number;
}

export interface InventoryValuationDto {
  currency: string;
  totalValue: number;
  variantCount: number;
  totalUnits: number;
  lowStockThreshold: number;
  lowStock: LowStockItemDto[];
}

export interface VatSummaryDto {
  from: string;
  to: string;
  currency: string;
  outputTax: number;
  inputTax: number;
  netVatDue: number;
}

export interface StaffDto {
  id: string;
  email: string;
  fullName: string;
  roles: string[];
  isActive: boolean;
}

export interface CreateStaffInput {
  email: string;
  fullName: string;
  password: string;
  roles: string[];
}

export interface AuditLogEntryDto {
  id: string;
  occurredAtUtc: string;
  actorId: string | null;
  actorEmail: string;
  action: string;
  summary: string | null;
}
