"use server";

import { revalidatePath } from "next/cache";
import { API_URL } from "@/lib/api";
import { getToken } from "@/lib/auth";
import { buildCreateProductPayload, toSlug } from "@/lib/products";
import { readApiError } from "@/lib/api-error";
import type { CreateProductInput } from "@/lib/types";

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
  if (!res.ok) return { error: await readApiError(res, "Failed to create category") };

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

  const built = buildCreateProductPayload(input);
  if (!built.ok) return { error: built.error };

  const res = await fetch(`${API_URL}/api/admin/products`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      Authorization: `Bearer ${token}`,
    },
    body: JSON.stringify(built.payload),
    cache: "no-store",
  });

  if (res.status === 401) return { error: "Session expired. Please log in again." };
  if (!res.ok) return { error: await readApiError(res, "Failed to create product") };

  const data = (await res.json()) as { id: string };

  // Refresh the dashboard so the new product (if published) appears.
  revalidatePath("/");

  return { success: { id: data.id, slug: toSlug(input.name), name: input.name.trim() } };
}
