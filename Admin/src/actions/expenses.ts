"use server";

import { revalidatePath } from "next/cache";
import { getToken } from "@/lib/auth";
import { MAX_UPLOAD_BYTES, MAX_UPLOAD_LABEL } from "@/lib/constants";
import {
  NETWORK_ERROR,
  SESSION_EXPIRED,
  readProblem,
  safeJson,
  tryFetch,
} from "@/lib/http";
import type {
  ApSummaryDto,
  BillDetailDto,
  BillSummaryDto,
  CreateBillInput,
  ExpenseCategoryDto,
  PagedResult,
} from "@/lib/types";

async function authHeader(): Promise<Record<string, string> | null> {
  const token = await getToken();
  if (!token) return null;
  return { Authorization: `Bearer ${token}`, Accept: "application/json" };
}

export async function getExpenseCategories(): Promise<ExpenseCategoryDto[]> {
  const headers = await authHeader();
  if (!headers) return [];

  const res = await tryFetch(`/api/admin/expenses/categories`, { headers });
  if (!res || !res.ok) return [];
  return (await safeJson<ExpenseCategoryDto[]>(res)) ?? [];
}

export async function getApSummary(): Promise<ApSummaryDto | null> {
  const headers = await authHeader();
  if (!headers) return null;

  const res = await tryFetch(`/api/admin/expenses/summary`, { headers });
  if (!res || !res.ok) return null;
  return await safeJson<ApSummaryDto>(res);
}

export interface BillsResult {
  data: PagedResult<BillSummaryDto> | null;
  error?: string;
}

export async function getBills(params?: {
  status?: string;
  search?: string;
  overdueOnly?: boolean;
}): Promise<BillsResult> {
  const headers = await authHeader();
  if (!headers) return { data: null, error: SESSION_EXPIRED };

  const query = new URLSearchParams({ pageSize: "100" });
  if (params?.status) query.set("status", params.status);
  if (params?.search) query.set("search", params.search);
  if (params?.overdueOnly) query.set("overdueOnly", "true");

  const res = await tryFetch(`/api/admin/bills?${query.toString()}`, { headers });
  if (!res) return { data: null, error: NETWORK_ERROR };
  if (res.status === 401) return { data: null, error: SESSION_EXPIRED };
  if (!res.ok) return { data: null, error: await readProblem(res, "Failed to load bills") };

  return { data: await safeJson<PagedResult<BillSummaryDto>>(res) };
}

export async function getBill(id: string): Promise<BillDetailDto | null> {
  const headers = await authHeader();
  if (!headers) return null;

  const res = await tryFetch(`/api/admin/bills/${id}`, { headers });
  if (!res || !res.ok) return null;
  return await safeJson<BillDetailDto>(res);
}

export interface CategoryState {
  error?: string;
  id?: string;
}

export async function createExpenseCategoryAction(
  name: string,
  description: string | null,
): Promise<CategoryState> {
  const headers = await authHeader();
  if (!headers) return { error: SESSION_EXPIRED };
  if (!name.trim()) return { error: "Category name is required." };

  const res = await tryFetch(`/api/admin/expenses/categories`, {
    method: "POST",
    headers: { ...headers, "Content-Type": "application/json" },
    body: JSON.stringify({ name: name.trim(), description: clean(description) }),
  });

  if (!res) return { error: NETWORK_ERROR };
  if (res.status === 401) return { error: SESSION_EXPIRED };
  if (!res.ok) return { error: await readProblem(res, "Failed to create category") };

  const body = await safeJson<{ id: string }>(res);
  if (!body?.id) return { error: "The category was saved but the server returned no id." };

  revalidatePath("/expenses/new");
  revalidatePath("/expenses/categories");
  return { id: body.id };
}

export interface BillState {
  error?: string;
  id?: string;
}

export async function createBillAction(input: CreateBillInput): Promise<BillState> {
  const headers = await authHeader();
  if (!headers) return { error: SESSION_EXPIRED };

  if (!input.vendorName.trim()) return { error: "Vendor name is required." };
  if (input.lines.length === 0) return { error: "Add at least one line item." };
  for (const line of input.lines) {
    if (!line.description.trim()) return { error: "Every line item needs a description." };
    if (!(line.quantity > 0)) {
      return { error: "Every line item needs a quantity greater than 0." };
    }
    if (!(line.unitCost >= 0)) return { error: "Unit cost cannot be negative." };
  }

  const res = await tryFetch(`/api/admin/bills`, {
    method: "POST",
    headers: { ...headers, "Content-Type": "application/json" },
    body: JSON.stringify({
      vendorName: input.vendorName.trim(),
      issueDate: input.issueDate,
      dueDate: input.dueDate,
      currency: clean(input.currency),
      supplierReference: clean(input.supplierReference),
      notes: clean(input.notes),
      attachmentUrl: clean(input.attachmentUrl),
      vendorId: null,
      lines: input.lines.map((l) => ({
        description: l.description.trim(),
        expenseCategoryId: l.expenseCategoryId,
        quantity: l.quantity,
        unitCost: l.unitCost,
        taxPercent: l.taxPercent,
      })),
    }),
  });

  if (!res) return { error: NETWORK_ERROR };
  if (res.status === 401) return { error: SESSION_EXPIRED };
  if (!res.ok) return { error: await readProblem(res, "Failed to create bill") };

  const body = await safeJson<{ id: string }>(res);
  if (!body?.id) return { error: "The bill was saved but the server returned no id." };

  revalidatePath("/expenses");
  return { id: body.id };
}

export interface ActionResult {
  error?: string;
  success?: boolean;
}

export interface PdfResult {
  error?: string;
  fileName?: string;
  base64?: string;
}

export async function getBillPdfAction(id: string): Promise<PdfResult> {
  const headers = await authHeader();
  if (!headers) return { error: SESSION_EXPIRED };

  const res = await tryFetch(`/api/admin/bills/${id}/pdf`, { headers });
  if (!res) return { error: NETWORK_ERROR };
  if (res.status === 401) return { error: SESSION_EXPIRED };
  if (!res.ok) return { error: await readProblem(res, "Failed to generate PDF") };

  const buffer = Buffer.from(await res.arrayBuffer());
  return { fileName: `bill-${id}.pdf`, base64: buffer.toString("base64") };
}

export async function approveBillAction(id: string): Promise<ActionResult> {
  return postBillAction(id, "approve", "Failed to approve bill");
}

export async function cancelBillAction(id: string): Promise<ActionResult> {
  return postBillAction(id, "cancel", "Failed to cancel bill");
}

async function postBillAction(
  id: string,
  action: string,
  fallback: string,
): Promise<ActionResult> {
  const headers = await authHeader();
  if (!headers) return { error: SESSION_EXPIRED };

  const res = await tryFetch(`/api/admin/bills/${id}/${action}`, {
    method: "POST",
    headers,
  });

  if (!res) return { error: NETWORK_ERROR };
  if (res.status === 401) return { error: SESSION_EXPIRED };
  if (!res.ok) return { error: await readProblem(res, fallback) };

  revalidatePath(`/expenses/${id}`);
  revalidatePath("/expenses");
  return { success: true };
}

export async function recordBillPaymentAction(
  id: string,
  payment: { amount: number; paidOn: string; method: string; reference: string | null },
): Promise<ActionResult> {
  const headers = await authHeader();
  if (!headers) return { error: SESSION_EXPIRED };

  if (!(payment.amount > 0)) return { error: "Payment amount must be greater than zero." };
  if (!payment.method.trim()) return { error: "Payment method is required." };

  const res = await tryFetch(`/api/admin/bills/${id}/payments`, {
    method: "POST",
    headers: { ...headers, "Content-Type": "application/json" },
    body: JSON.stringify({
      amount: payment.amount,
      paidOn: payment.paidOn,
      method: payment.method.trim(),
      reference: clean(payment.reference),
    }),
  });

  if (!res) return { error: NETWORK_ERROR };
  if (res.status === 401) return { error: SESSION_EXPIRED };
  if (!res.ok) return { error: await readProblem(res, "Failed to record payment") };

  revalidatePath(`/expenses/${id}`);
  revalidatePath("/expenses");
  return { success: true };
}

export interface UploadResult {
  error?: string;
  url?: string;
}

/** Forwards a receipt upload (multipart) to the API, attaching the admin bearer token. */
export async function uploadReceiptAction(formData: FormData): Promise<UploadResult> {
  const headers = await authHeader();
  if (!headers) return { error: SESSION_EXPIRED };

  const file = formData.get("file");
  if (!(file instanceof File) || file.size === 0) {
    return { error: "No file was selected." };
  }
  if (file.size > MAX_UPLOAD_BYTES) {
    return { error: `That file is larger than the ${MAX_UPLOAD_LABEL} limit.` };
  }

  const forward = new FormData();
  forward.append("file", file, file.name);

  const res = await tryFetch(`/api/admin/files`, {
    method: "POST",
    headers: { Authorization: headers.Authorization },
    body: forward,
  });

  if (!res) return { error: NETWORK_ERROR };
  if (res.status === 401) return { error: SESSION_EXPIRED };
  if (!res.ok) return { error: await readProblem(res, "Upload failed") };

  const body = await safeJson<{ url: string }>(res);
  if (!body?.url) return { error: "The file uploaded but the server returned no URL." };
  return { url: body.url };
}

function clean(value: string | null): string | null {
  if (value === null) return null;
  const trimmed = value.trim();
  return trimmed.length ? trimmed : null;
}
