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
import type { CompanySettings } from "@/lib/types";

/** Loads the company settings for the settings screen (admin-only). */
export async function getCompanySettings(): Promise<CompanySettings | null> {
  const token = await getToken();
  if (!token) return null;

  const res = await tryFetch(`/api/admin/settings`, {
    headers: { Authorization: `Bearer ${token}`, Accept: "application/json" },
  });

  if (!res || !res.ok) return null;
  return await safeJson<CompanySettings>(res);
}

export interface SaveSettingsState {
  error?: string;
  success?: boolean;
}

export async function updateCompanySettingsAction(
  input: CompanySettings,
): Promise<SaveSettingsState> {
  const token = await getToken();
  if (!token) return { error: SESSION_EXPIRED };

  if (!input.legalName.trim()) return { error: "Legal name is required." };
  if (!input.currency.trim() || input.currency.trim().length !== 3) {
    return { error: "Currency must be a 3-letter code (e.g. KES)." };
  }

  const res = await tryFetch(`/api/admin/settings`, {
    method: "PUT",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify({
      legalName: input.legalName.trim(),
      tradingName: clean(input.tradingName),
      email: clean(input.email),
      phone: clean(input.phone),
      taxIdentifier: clean(input.taxIdentifier),
      addressLine1: clean(input.addressLine1),
      addressLine2: clean(input.addressLine2),
      city: clean(input.city),
      country: clean(input.country),
      logoUrl: clean(input.logoUrl),
      currency: input.currency.trim().toUpperCase(),
      defaultTaxPercent: Number(input.defaultTaxPercent),
      invoiceNumberPrefix: input.invoiceNumberPrefix.trim() || "INV",
      billNumberPrefix: input.billNumberPrefix.trim() || "BILL",
      purchaseOrderNumberPrefix: input.purchaseOrderNumberPrefix.trim() || "PO",
      invoiceFooter: clean(input.invoiceFooter),
      paymentInstructions: clean(input.paymentInstructions),
    }),
  });

  if (!res) return { error: NETWORK_ERROR };
  if (res.status === 401) return { error: SESSION_EXPIRED };
  if (!res.ok) return { error: await readProblem(res, "Failed to save settings") };

  revalidatePath("/settings");
  return { success: true };
}

function clean(value: string | null): string | null {
  if (value === null) return null;
  const trimmed = value.trim();
  return trimmed.length ? trimmed : null;
}
