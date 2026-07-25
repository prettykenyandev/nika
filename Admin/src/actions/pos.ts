"use server";

import { revalidatePath } from "next/cache";
import { API_URL } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { readApiError } from "@/lib/api-error";
import type { PosSaleResult, PosVariant } from "@/lib/types";

export interface LookupSkuResult {
  variant?: PosVariant;
  error?: string;
}

/** Resolve a single variant by SKU / scanned barcode for the till. */
export async function lookupSkuAction(sku: string): Promise<LookupSkuResult> {
  const token = await getToken();
  if (!token) return { error: "Your session has expired. Please log in again." };

  const trimmed = sku.trim();
  if (!trimmed) return { error: "Enter or scan a SKU." };

  const res = await fetch(
    `${API_URL}/api/admin/pos/lookup?sku=${encodeURIComponent(trimmed)}`,
    {
      headers: { Authorization: `Bearer ${token}`, Accept: "application/json" },
      cache: "no-store",
    },
  );

  if (res.status === 401) return { error: "Session expired. Please log in again." };
  if (res.status === 404) return { error: `No product found for SKU “${trimmed}”.` };
  if (!res.ok) return { error: await readApiError(res, "Lookup failed") };

  return { variant: (await res.json()) as PosVariant };
}

export interface PosSaleInput {
  items: { productVariantId: string; quantity: number }[];
  method: "Card" | "Mpesa";
  customerPhone: string | null;
}

export interface PosSaleState {
  result?: PosSaleResult;
  error?: string;
}

/** Ring up an in-store Card or M-Pesa sale (reserves stock and places the order). */
export async function createPosSaleAction(
  input: PosSaleInput,
): Promise<PosSaleState> {
  const token = await getToken();
  if (!token) return { error: "Your session has expired. Please log in again." };

  if (!input.items.length) return { error: "Add at least one item to the sale." };

  const res = await fetch(`${API_URL}/api/admin/pos/sales`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify({
      items: input.items,
      method: input.method,
      customerPhone: input.customerPhone,
      customerEmail: null,
    }),
    cache: "no-store",
  });

  if (res.status === 401) return { error: "Session expired. Please log in again." };
  if (!res.ok) return { error: await readApiError(res, "Sale could not be completed") };

  // Stock changed — refresh the inventory dashboard.
  revalidatePath("/");

  return { result: (await res.json()) as PosSaleResult };
}
