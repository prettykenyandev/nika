"use server";

import { revalidatePath } from "next/cache";
import { getToken } from "@/lib/auth";
import {
  NETWORK_ERROR,
  SESSION_EXPIRED,
  readProblem,
  safeJson,
  tryFetch,
} from "@/lib/http";
import type {
  ArSummaryDto,
  CreateInvoiceInput,
  InvoiceDetailDto,
  InvoiceSummaryDto,
  PagedResult,
} from "@/lib/types";

async function authHeader(): Promise<Record<string, string> | null> {
  const token = await getToken();
  if (!token) return null;
  return { Authorization: `Bearer ${token}`, Accept: "application/json" };
}

export async function getArSummary(): Promise<ArSummaryDto | null> {
  const headers = await authHeader();
  if (!headers) return null;

  const res = await tryFetch(`/api/admin/invoices/summary`, { headers });
  if (!res || !res.ok) return null;
  return await safeJson<ArSummaryDto>(res);
}

export interface InvoicesResult {
  data: PagedResult<InvoiceSummaryDto> | null;
  error?: string;
}

export async function getInvoices(params?: {
  status?: string;
  search?: string;
  overdueOnly?: boolean;
}): Promise<InvoicesResult> {
  const headers = await authHeader();
  if (!headers) return { data: null, error: SESSION_EXPIRED };

  const query = new URLSearchParams({ pageSize: "100" });
  if (params?.status) query.set("status", params.status);
  if (params?.search) query.set("search", params.search);
  if (params?.overdueOnly) query.set("overdueOnly", "true");

  const res = await tryFetch(`/api/admin/invoices?${query.toString()}`, { headers });
  if (!res) return { data: null, error: NETWORK_ERROR };
  if (res.status === 401) return { data: null, error: SESSION_EXPIRED };
  if (!res.ok) return { data: null, error: await readProblem(res, "Failed to load invoices") };

  return { data: await safeJson<PagedResult<InvoiceSummaryDto>>(res) };
}

export async function getInvoice(id: string): Promise<InvoiceDetailDto | null> {
  const headers = await authHeader();
  if (!headers) return null;

  const res = await tryFetch(`/api/admin/invoices/${id}`, { headers });
  if (!res || !res.ok) return null;
  return await safeJson<InvoiceDetailDto>(res);
}

export interface InvoiceState {
  error?: string;
  id?: string;
}

export async function createInvoiceAction(
  input: CreateInvoiceInput,
): Promise<InvoiceState> {
  const headers = await authHeader();
  if (!headers) return { error: SESSION_EXPIRED };

  if (!input.customerName.trim()) return { error: "Customer name is required." };
  if (input.lines.length === 0) return { error: "Add at least one line item." };
  for (const line of input.lines) {
    if (!line.description.trim()) return { error: "Every line item needs a description." };
    if (!(line.quantity > 0)) {
      return { error: "Every line item needs a quantity greater than 0." };
    }
    if (!(line.unitPrice >= 0)) return { error: "Unit price cannot be negative." };
  }

  const res = await tryFetch(`/api/admin/invoices`, {
    method: "POST",
    headers: { ...headers, "Content-Type": "application/json" },
    body: JSON.stringify({
      customerName: input.customerName.trim(),
      customerEmail: clean(input.customerEmail),
      issueDate: input.issueDate,
      dueDate: input.dueDate,
      currency: clean(input.currency),
      notes: clean(input.notes),
      customerId: null,
      lines: input.lines.map((l) => ({
        description: l.description.trim(),
        quantity: l.quantity,
        unitPrice: l.unitPrice,
        taxPercent: l.taxPercent,
      })),
    }),
  });

  if (!res) return { error: NETWORK_ERROR };
  if (res.status === 401) return { error: SESSION_EXPIRED };
  if (!res.ok) return { error: await readProblem(res, "Failed to create invoice") };

  const body = await safeJson<{ id: string }>(res);
  if (!body?.id) return { error: "The invoice was saved but the server returned no id." };

  revalidatePath("/invoices");
  return { id: body.id };
}

export interface ActionResult {
  error?: string;
  success?: boolean;
}

export async function sendInvoiceAction(id: string): Promise<ActionResult> {
  return postInvoiceAction(id, "send", "Failed to send invoice");
}

export async function voidInvoiceAction(id: string): Promise<ActionResult> {
  return postInvoiceAction(id, "void", "Failed to void invoice");
}

async function postInvoiceAction(
  id: string,
  action: string,
  fallback: string,
): Promise<ActionResult> {
  const headers = await authHeader();
  if (!headers) return { error: SESSION_EXPIRED };

  const res = await tryFetch(`/api/admin/invoices/${id}/${action}`, {
    method: "POST",
    headers,
  });

  if (!res) return { error: NETWORK_ERROR };
  if (res.status === 401) return { error: SESSION_EXPIRED };
  if (!res.ok) return { error: await readProblem(res, fallback) };

  revalidatePath(`/invoices/${id}`);
  revalidatePath("/invoices");
  return { success: true };
}

export async function recordInvoicePaymentAction(
  id: string,
  payment: { amount: number; receivedOn: string; method: string; reference: string | null },
): Promise<ActionResult> {
  const headers = await authHeader();
  if (!headers) return { error: SESSION_EXPIRED };

  if (!(payment.amount > 0)) return { error: "Receipt amount must be greater than zero." };
  if (!payment.method.trim()) return { error: "Payment method is required." };

  const res = await tryFetch(`/api/admin/invoices/${id}/payments`, {
    method: "POST",
    headers: { ...headers, "Content-Type": "application/json" },
    body: JSON.stringify({
      amount: payment.amount,
      receivedOn: payment.receivedOn,
      method: payment.method.trim(),
      reference: clean(payment.reference),
    }),
  });

  if (!res) return { error: NETWORK_ERROR };
  if (res.status === 401) return { error: SESSION_EXPIRED };
  if (!res.ok) return { error: await readProblem(res, "Failed to record receipt") };

  revalidatePath(`/invoices/${id}`);
  revalidatePath("/invoices");
  return { success: true };
}

function clean(value: string | null): string | null {
  if (value === null) return null;
  const trimmed = value.trim();
  return trimmed.length ? trimmed : null;
}
