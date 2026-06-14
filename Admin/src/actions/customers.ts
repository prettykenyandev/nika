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
  CustomerDetailDto,
  CustomerInput,
  CustomerSummaryDto,
  PagedResult,
} from "@/lib/types";

async function authHeader(): Promise<Record<string, string> | null> {
  const token = await getToken();
  if (!token) return null;
  return { Authorization: `Bearer ${token}`, Accept: "application/json" };
}

export interface CustomersResult {
  data: PagedResult<CustomerSummaryDto> | null;
  error?: string;
}

export async function getCustomers(params?: {
  search?: string;
  activeOnly?: boolean;
}): Promise<CustomersResult> {
  const headers = await authHeader();
  if (!headers) return { data: null, error: SESSION_EXPIRED };

  const query = new URLSearchParams({ page: "1", pageSize: "100" });
  if (params?.search) query.set("search", params.search);
  if (params?.activeOnly !== undefined) {
    query.set("activeOnly", params.activeOnly ? "true" : "false");
  }

  const res = await tryFetch(`/api/admin/customers?${query.toString()}`, { headers });
  if (!res) return { data: null, error: NETWORK_ERROR };
  if (res.status === 401) return { data: null, error: SESSION_EXPIRED };
  if (!res.ok) return { data: null, error: await readProblem(res, "Failed to load customers") };

  return { data: await safeJson<PagedResult<CustomerSummaryDto>>(res) };
}

export async function getCustomer(id: string): Promise<CustomerDetailDto | null> {
  const headers = await authHeader();
  if (!headers) return null;

  const res = await tryFetch(`/api/admin/customers/${id}`, { headers });
  if (!res || !res.ok) return null;
  return await safeJson<CustomerDetailDto>(res);
}

export interface CustomerState {
  error?: string;
  id?: string;
  success?: boolean;
  count?: number;
}

export async function createCustomerAction(input: CustomerInput): Promise<CustomerState> {
  const headers = await authHeader();
  if (!headers) return { error: SESSION_EXPIRED };
  const payload = cleanCustomer(input);
  if (!payload.name) return { error: "Customer name is required." };

  const res = await tryFetch(`/api/admin/customers`, {
    method: "POST",
    headers: { ...headers, "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });

  if (!res) return { error: NETWORK_ERROR };
  if (res.status === 401) return { error: SESSION_EXPIRED };
  if (!res.ok) return { error: await readProblem(res, "Failed to create customer") };

  const body = await safeJson<{ id: string }>(res);
  if (!body?.id) return { error: "The customer was saved but the server returned no id." };

  revalidatePath("/customers");
  return { id: body.id };
}

export async function updateCustomerAction(
  id: string,
  input: CustomerInput,
): Promise<CustomerState> {
  const headers = await authHeader();
  if (!headers) return { error: SESSION_EXPIRED };
  const payload = cleanCustomer(input);
  if (!payload.name) return { error: "Customer name is required." };

  const res = await tryFetch(`/api/admin/customers/${id}`, {
    method: "PUT",
    headers: { ...headers, "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });

  if (!res) return { error: NETWORK_ERROR };
  if (res.status === 401) return { error: SESSION_EXPIRED };
  if (!res.ok) return { error: await readProblem(res, "Failed to update customer") };

  revalidatePath(`/customers/${id}`);
  revalidatePath("/customers");
  return { success: true };
}

export async function setCustomerActiveAction(
  id: string,
  active: boolean,
): Promise<CustomerState> {
  const headers = await authHeader();
  if (!headers) return { error: SESSION_EXPIRED };

  const res = await tryFetch(
    `/api/admin/customers/${id}/${active ? "activate" : "deactivate"}`,
    { method: "POST", headers },
  );

  if (!res) return { error: NETWORK_ERROR };
  if (res.status === 401) return { error: SESSION_EXPIRED };
  if (!res.ok) {
    return {
      error: await readProblem(
        res,
        active ? "Failed to activate customer" : "Failed to deactivate customer",
      ),
    };
  }

  revalidatePath(`/customers/${id}`);
  revalidatePath("/customers");
  return { success: true };
}

export async function backfillCustomersAction(): Promise<CustomerState> {
  const headers = await authHeader();
  if (!headers) return { error: SESSION_EXPIRED };

  const res = await tryFetch(`/api/admin/customers/backfill`, {
    method: "POST",
    headers,
  });

  if (!res) return { error: NETWORK_ERROR };
  if (res.status === 401) return { error: SESSION_EXPIRED };
  if (!res.ok) return { error: await readProblem(res, "Failed to backfill customers") };

  const body = await safeJson<Record<string, unknown>>(res);
  const numeric = body ? Object.values(body).find((v): v is number => typeof v === "number") : 0;
  revalidatePath("/customers");
  return { success: true, count: numeric ?? 0 };
}

function cleanCustomer(input: CustomerInput): CustomerInput {
  return {
    name: input.name.trim(),
    email: clean(input.email),
    phone: clean(input.phone),
    addressLine1: clean(input.addressLine1),
    city: clean(input.city),
    country: clean(input.country),
    notes: clean(input.notes),
  };
}

function clean(value: string | null): string | null {
  if (value === null) return null;
  const trimmed = value.trim();
  return trimmed.length ? trimmed : null;
}
