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
import type { PosSaleResult, PosVariant } from "@/lib/types";

export interface LookupSkuResult {
  variant?: PosVariant;
  error?: string;
}

/** Resolve a single variant by SKU / scanned barcode for the till. */
export async function lookupSkuAction(sku: string): Promise<LookupSkuResult> {
  const token = await getToken();
  if (!token) return { error: SESSION_EXPIRED };

  const trimmed = sku.trim();
  if (!trimmed) return { error: "Enter or scan a SKU." };

  const res = await tryFetch(
    `/api/admin/pos/lookup?sku=${encodeURIComponent(trimmed)}`,
    {
      headers: { Authorization: `Bearer ${token}`, Accept: "application/json" },
    },
  );

  if (!res) return { error: NETWORK_ERROR };
  if (res.status === 401) return { error: SESSION_EXPIRED };
  if (res.status === 404) return { error: `No product found for SKU “${trimmed}”.` };
  if (!res.ok) return { error: await readProblem(res, "Lookup failed") };

  const variant = await safeJson<PosVariant>(res);
  if (!variant) return { error: "Lookup failed: the server returned an unexpected response." };
  return { variant };
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
  if (!token) return { error: SESSION_EXPIRED };

  if (!input.items.length) return { error: "Add at least one item to the sale." };

  const res = await tryFetch(`/api/admin/pos/sales`, {
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
  });

  if (!res) return { error: NETWORK_ERROR };
  if (res.status === 401) return { error: SESSION_EXPIRED };
  if (!res.ok) return { error: await readProblem(res, "Sale could not be completed") };

  const result = await safeJson<PosSaleResult>(res);
  if (!result) return { error: "The sale may not have completed: unexpected server response." };

  // Stock changed — refresh the inventory dashboard.
  revalidatePath("/");

  return { result };
}
