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
  PagedResult,
  VendorDetailDto,
  VendorInput,
  VendorSummaryDto,
} from "@/lib/types";

async function authHeader(): Promise<Record<string, string> | null> {
  const token = await getToken();
  if (!token) return null;
  return { Authorization: `Bearer ${token}`, Accept: "application/json" };
}

export interface VendorsResult {
  data: PagedResult<VendorSummaryDto> | null;
  error?: string;
}

export async function getVendors(params?: {
  search?: string;
  activeOnly?: boolean;
}): Promise<VendorsResult> {
  const headers = await authHeader();
  if (!headers) return { data: null, error: SESSION_EXPIRED };

  const query = new URLSearchParams({ page: "1", pageSize: "100" });
  if (params?.search) query.set("search", params.search);
  if (params?.activeOnly !== undefined) {
    query.set("activeOnly", params.activeOnly ? "true" : "false");
  }

  const res = await tryFetch(`/api/admin/vendors?${query.toString()}`, { headers });
  if (!res) return { data: null, error: NETWORK_ERROR };
  if (res.status === 401) return { data: null, error: SESSION_EXPIRED };
  if (!res.ok) return { data: null, error: await readProblem(res, "Failed to load vendors") };

  return { data: await safeJson<PagedResult<VendorSummaryDto>>(res) };
}

export async function getVendor(id: string): Promise<VendorDetailDto | null> {
  const headers = await authHeader();
  if (!headers) return null;

  const res = await tryFetch(`/api/admin/vendors/${id}`, { headers });
  if (!res || !res.ok) return null;
  return await safeJson<VendorDetailDto>(res);
}

export interface VendorState {
  error?: string;
  id?: string;
  success?: boolean;
}

export async function createVendorAction(input: VendorInput): Promise<VendorState> {
  const headers = await authHeader();
  if (!headers) return { error: SESSION_EXPIRED };
  const payload = cleanVendor(input);
  if (!payload.name) return { error: "Vendor name is required." };
  if (!(payload.paymentTermDays >= 0)) {
    return { error: "Payment terms cannot be negative." };
  }

  const res = await tryFetch(`/api/admin/vendors`, {
    method: "POST",
    headers: { ...headers, "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });

  if (!res) return { error: NETWORK_ERROR };
  if (res.status === 401) return { error: SESSION_EXPIRED };
  if (!res.ok) return { error: await readProblem(res, "Failed to create vendor") };

  const body = await safeJson<{ id: string }>(res);
  if (!body?.id) return { error: "The vendor was saved but the server returned no id." };

  revalidatePath("/vendors");
  return { id: body.id };
}

export async function updateVendorAction(
  id: string,
  input: VendorInput,
): Promise<VendorState> {
  const headers = await authHeader();
  if (!headers) return { error: SESSION_EXPIRED };
  const payload = cleanVendor(input);
  if (!payload.name) return { error: "Vendor name is required." };
  if (!(payload.paymentTermDays >= 0)) {
    return { error: "Payment terms cannot be negative." };
  }

  const res = await tryFetch(`/api/admin/vendors/${id}`, {
    method: "PUT",
    headers: { ...headers, "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });

  if (!res) return { error: NETWORK_ERROR };
  if (res.status === 401) return { error: SESSION_EXPIRED };
  if (!res.ok) return { error: await readProblem(res, "Failed to update vendor") };

  revalidatePath(`/vendors/${id}`);
  revalidatePath("/vendors");
  return { success: true };
}

export async function setVendorActiveAction(
  id: string,
  active: boolean,
): Promise<VendorState> {
  const headers = await authHeader();
  if (!headers) return { error: SESSION_EXPIRED };

  const res = await tryFetch(
    `/api/admin/vendors/${id}/${active ? "activate" : "deactivate"}`,
    { method: "POST", headers },
  );

  if (!res) return { error: NETWORK_ERROR };
  if (res.status === 401) return { error: SESSION_EXPIRED };
  if (!res.ok) {
    return {
      error: await readProblem(res, active ? "Failed to activate vendor" : "Failed to deactivate vendor"),
    };
  }

  revalidatePath(`/vendors/${id}`);
  revalidatePath("/vendors");
  return { success: true };
}

function cleanVendor(input: VendorInput): VendorInput {
  return {
    name: input.name.trim(),
    contactName: clean(input.contactName),
    email: clean(input.email),
    phone: clean(input.phone),
    addressLine1: clean(input.addressLine1),
    city: clean(input.city),
    country: clean(input.country),
    taxIdentifier: clean(input.taxIdentifier),
    paymentTermDays: input.paymentTermDays,
    notes: clean(input.notes),
  };
}

function clean(value: string | null): string | null {
  if (value === null) return null;
  const trimmed = value.trim();
  return trimmed.length ? trimmed : null;
}
