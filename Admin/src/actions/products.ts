"use server";

import { revalidatePath } from "next/cache";
import { API_URL } from "@/lib/api";
import { getToken } from "@/lib/auth";
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
  if (!token) return { error: "Your session has expired. Please log in again." };

  if (!name.trim()) return { error: "Category name is required." };

  const res = await fetch(`${API_URL}/api/admin/categories`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify({ name: name.trim(), description }),
    cache: "no-store",
  });

  if (res.status === 401) return { error: "Session expired. Please log in again." };
  if (!res.ok) return { error: await readError(res, "Failed to create category") };

  const data = (await res.json()) as { id: string };
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
  if (!token) return { error: "Your session has expired. Please log in again." };

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

  const res = await fetch(`${API_URL}/api/admin/products`, {
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
    cache: "no-store",
  });

  if (res.status === 401) return { error: "Session expired. Please log in again." };
  if (!res.ok) return { error: await readError(res, "Failed to create product") };

  const data = (await res.json()) as { id: string };

  // Refresh the dashboard so the new product (if published) appears.
  revalidatePath("/");

  return { success: { id: data.id, slug: toSlug(input.name), name: input.name.trim() } };
}

async function readError(res: Response, fallback: string): Promise<string> {
  try {
    const body = (await res.json()) as {
      detail?: string;
      title?: string;
      errors?: Record<string, string[]>;
    };
    if (body.errors) {
      const messages = Object.values(body.errors).flat();
      if (messages.length) return messages.join(" ");
    }
    if (body.detail) return body.detail;
    if (body.title) return body.title;
  } catch {
    // ignore parse failures
  }
  return `${fallback} (${res.status}).`;
}
