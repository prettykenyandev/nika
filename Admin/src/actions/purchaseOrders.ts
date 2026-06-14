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
  CreatePurchaseOrderInput,
  PagedResult,
  PurchaseOrderDetailDto,
  PurchaseOrderSummaryDto,
  VariantOptionDto,
} from "@/lib/types";

async function authHeader(): Promise<Record<string, string> | null> {
  const token = await getToken();
  if (!token) return null;
  return { Authorization: `Bearer ${token}`, Accept: "application/json" };
}

export interface PurchaseOrdersResult {
  data: PagedResult<PurchaseOrderSummaryDto> | null;
  error?: string;
}

export async function getPurchaseOrders(params?: {
  status?: string;
  vendorId?: string;
  search?: string;
}): Promise<PurchaseOrdersResult> {
  const headers = await authHeader();
  if (!headers) return { data: null, error: SESSION_EXPIRED };

  const query = new URLSearchParams({ page: "1", pageSize: "50" });
  if (params?.status) query.set("status", params.status);
  if (params?.vendorId) query.set("vendorId", params.vendorId);
  if (params?.search) query.set("search", params.search);

  const res = await tryFetch(`/api/admin/purchase-orders?${query.toString()}`, { headers });
  if (!res) return { data: null, error: NETWORK_ERROR };
  if (res.status === 401) return { data: null, error: SESSION_EXPIRED };
  if (!res.ok) {
    return { data: null, error: await readProblem(res, "Failed to load purchase orders") };
  }

  return { data: await safeJson<PagedResult<PurchaseOrderSummaryDto>>(res) };
}

export async function getPurchaseOrder(id: string): Promise<PurchaseOrderDetailDto | null> {
  const headers = await authHeader();
  if (!headers) return null;

  const res = await tryFetch(`/api/admin/purchase-orders/${id}`, { headers });
  if (!res || !res.ok) return null;
  return await safeJson<PurchaseOrderDetailDto>(res);
}

export async function getVariantOptions(search?: string): Promise<VariantOptionDto[]> {
  const headers = await authHeader();
  if (!headers) return [];

  const query = new URLSearchParams();
  if (search) query.set("search", search);
  const suffix = query.toString() ? `?${query.toString()}` : "";
  const res = await tryFetch(`/api/admin/purchasing/variants${suffix}`, { headers });
  if (!res || !res.ok) return [];
  return (await safeJson<VariantOptionDto[]>(res)) ?? [];
}

export interface PurchaseOrderState {
  error?: string;
  id?: string;
  success?: boolean;
  billId?: string | null;
}

export async function createPurchaseOrderAction(
  input: CreatePurchaseOrderInput,
): Promise<PurchaseOrderState> {
  const headers = await authHeader();
  if (!headers) return { error: SESSION_EXPIRED };

  if (!input.vendorId) return { error: "Choose a vendor." };
  if (input.lines.length === 0) return { error: "Add at least one line item." };
  for (const line of input.lines) {
    if (!line.description.trim()) return { error: "Every line item needs a description." };
    if (!(line.quantity > 0)) {
      return { error: "Every line item needs a quantity greater than 0." };
    }
    if (!(line.unitCost >= 0)) return { error: "Unit cost cannot be negative." };
  }

  const res = await tryFetch(`/api/admin/purchase-orders`, {
    method: "POST",
    headers: { ...headers, "Content-Type": "application/json" },
    body: JSON.stringify({
      vendorId: input.vendorId,
      orderDate: input.orderDate,
      expectedDate: clean(input.expectedDate),
      currency: clean(input.currency),
      notes: clean(input.notes),
      lines: input.lines.map((l) => ({
        productVariantId: l.productVariantId,
        description: l.description.trim(),
        sku: clean(l.sku),
        quantity: l.quantity,
        unitCost: l.unitCost,
        taxPercent: l.taxPercent,
      })),
    }),
  });

  if (!res) return { error: NETWORK_ERROR };
  if (res.status === 401) return { error: SESSION_EXPIRED };
  if (!res.ok) {
    return { error: await readProblem(res, "Failed to create purchase order") };
  }

  const body = await safeJson<{ id: string }>(res);
  if (!body?.id) {
    return { error: "The purchase order was saved but the server returned no id." };
  }

  revalidatePath("/purchase-orders");
  return { id: body.id };
}

export async function sendPurchaseOrderAction(id: string): Promise<PurchaseOrderState> {
  return postPurchaseOrderAction(id, "send", "Failed to send purchase order");
}

export async function cancelPurchaseOrderAction(id: string): Promise<PurchaseOrderState> {
  return postPurchaseOrderAction(id, "cancel", "Failed to cancel purchase order");
}

export async function receivePurchaseOrderAction(
  id: string,
  receipts: { lineId: string; quantity: number }[],
): Promise<PurchaseOrderState> {
  const headers = await authHeader();
  if (!headers) return { error: SESSION_EXPIRED };

  const valid = receipts.filter((r) => r.lineId && r.quantity > 0);
  if (valid.length === 0) return { error: "Enter at least one quantity to receive." };

  const res = await tryFetch(`/api/admin/purchase-orders/${id}/receive`, {
    method: "POST",
    headers: { ...headers, "Content-Type": "application/json" },
    body: JSON.stringify({ receipts: valid }),
  });

  if (!res) return { error: NETWORK_ERROR };
  if (res.status === 401) return { error: SESSION_EXPIRED };
  if (!res.ok) return { error: await readProblem(res, "Failed to receive goods") };

  revalidatePath(`/purchase-orders/${id}`);
  revalidatePath("/purchase-orders");
  return { success: true };
}

export async function closePurchaseOrderAction(
  id: string,
  generateBill: boolean,
): Promise<PurchaseOrderState> {
  const headers = await authHeader();
  if (!headers) return { error: SESSION_EXPIRED };

  const res = await tryFetch(`/api/admin/purchase-orders/${id}/close`, {
    method: "POST",
    headers: { ...headers, "Content-Type": "application/json" },
    body: JSON.stringify({ generateBill }),
  });

  if (!res) return { error: NETWORK_ERROR };
  if (res.status === 401) return { error: SESSION_EXPIRED };
  if (!res.ok) return { error: await readProblem(res, "Failed to close purchase order") };

  const body = await safeJson<{ billId: string | null }>(res);
  revalidatePath(`/purchase-orders/${id}`);
  revalidatePath("/purchase-orders");
  if (body?.billId) revalidatePath(`/expenses/${body.billId}`);
  return { success: true, billId: body?.billId ?? null };
}

async function postPurchaseOrderAction(
  id: string,
  action: string,
  fallback: string,
): Promise<PurchaseOrderState> {
  const headers = await authHeader();
  if (!headers) return { error: SESSION_EXPIRED };

  const res = await tryFetch(`/api/admin/purchase-orders/${id}/${action}`, {
    method: "POST",
    headers,
  });

  if (!res) return { error: NETWORK_ERROR };
  if (res.status === 401) return { error: SESSION_EXPIRED };
  if (!res.ok) return { error: await readProblem(res, fallback) };

  revalidatePath(`/purchase-orders/${id}`);
  revalidatePath("/purchase-orders");
  return { success: true };
}

function clean(value: string | null): string | null {
  if (value === null) return null;
  const trimmed = value.trim();
  return trimmed.length ? trimmed : null;
}
