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
import type { CreateProductInput } from "@/lib/types";

function toSlug(value: string): string {
  return value
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
}

export interface CreateCategoryResult {
  id?: string;
  error?: string;
}

export async function createCategoryAction(
  name: string,
  description: string | null,
): Promise<CreateCategoryResult> {
  const token = await getToken();
  if (!token) return { error: SESSION_EXPIRED };

  if (!name.trim()) return { error: "Category name is required." };

  const res = await tryFetch(`/api/admin/categories`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify({ name: name.trim(), description }),
  });

  if (!res) return { error: NETWORK_ERROR };
  if (res.status === 401) return { error: SESSION_EXPIRED };
  if (!res.ok) return { error: await readProblem(res, "Failed to create category") };

  const data = await safeJson<{ id: string }>(res);
  if (!data?.id) return { error: "The category was saved but the server returned no id." };

  revalidatePath("/products/new");
  return { id: data.id };
}

export interface CreateProductState {
  error?: string;
  success?: { id: string; slug: string; name: string };
}

export async function createProductAction(
  input: CreateProductInput,
): Promise<CreateProductState> {
  const token = await getToken();
  if (!token) return { error: SESSION_EXPIRED };

  if (!input.name.trim()) return { error: "Product name is required." };
  if (!input.description.trim()) return { error: "Description is required." };
  if (!input.categoryId) return { error: "Please choose a category." };

  const imageUrls = input.imageUrls.map((u) => u.trim()).filter(Boolean);
  const variants = input.variants
    .map((v) => ({
      sku: v.sku.trim(),
      name: v.name.trim(),
      price: Number(v.price),
      stockQuantity: Math.trunc(Number(v.stockQuantity)),
    }))
    .filter((v) => v.sku || v.name);

  if (variants.length === 0) {
    return { error: "Add at least one variant (SKU, name, price, stock)." };
  }
  for (const v of variants) {
    if (!v.sku) return { error: "Every variant needs a SKU." };
    if (!v.name) return { error: `Variant ${v.sku} needs a name.` };
    if (!Number.isFinite(v.price) || v.price <= 0) {
      return { error: `Variant ${v.sku} needs a price greater than 0.` };
    }
    if (!Number.isFinite(v.stockQuantity) || v.stockQuantity < 0) {
      return { error: `Variant ${v.sku} needs a stock quantity of 0 or more.` };
    }
  }

  const res = await tryFetch(`/api/admin/products`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify({
      name: input.name.trim(),
      description: input.description.trim(),
      categoryId: input.categoryId,
      imageUrls,
      variants,
      publish: input.publish,
    }),
  });

  if (!res) return { error: NETWORK_ERROR };
  if (res.status === 401) return { error: SESSION_EXPIRED };
  if (!res.ok) return { error: await readProblem(res, "Failed to create product") };

  const data = await safeJson<{ id: string }>(res);
  if (!data?.id) return { error: "The product was saved but the server returned no id." };

  // Refresh the dashboard so the new product (if published) appears.
  revalidatePath("/");

  return { success: { id: data.id, slug: toSlug(input.name), name: input.name.trim() } };
}
