"use client";

import type { CartDto, CheckoutResult, OrderDto } from "@/lib/types";

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5087";
const CART_ID_KEY = "nika_cart_id";
const CART_ID_HEADER = "X-Cart-Id";

/** A stable, browser-scoped cart identifier persisted in localStorage. */
export function getCartId(): string {
  if (typeof window === "undefined") return "";

  let id = window.localStorage.getItem(CART_ID_KEY);
  if (!id) {
    id = crypto.randomUUID();
    window.localStorage.setItem(CART_ID_KEY, id);
  }
  return id;
}

interface ApiError {
  title?: string;
  errors?: Record<string, string[]>;
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${API_URL}${path}`, {
    ...init,
    headers: {
      "Content-Type": "application/json",
      Accept: "application/json",
      [CART_ID_HEADER]: getCartId(),
      ...init?.headers,
    },
  });

  if (!response.ok) {
    let message = `Request failed (${response.status}).`;
    try {
      const problem = (await response.json()) as ApiError;
      const firstFieldError = problem.errors
        ? Object.values(problem.errors).flat()[0]
        : undefined;
      message = firstFieldError ?? problem.title ?? message;
    } catch {
      // non-JSON error body; keep the default message
    }
    throw new Error(message);
  }

  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}

export function fetchCart(): Promise<CartDto> {
  return request<CartDto>("/api/cart");
}

export function addToCart(productVariantId: string, quantity: number): Promise<CartDto> {
  return request<CartDto>("/api/cart/items", {
    method: "POST",
    body: JSON.stringify({ productVariantId, quantity }),
  });
}

export function updateCartItem(variantId: string, quantity: number): Promise<CartDto> {
  return request<CartDto>(`/api/cart/items/${variantId}`, {
    method: "PUT",
    body: JSON.stringify({ quantity }),
  });
}

export function removeCartItem(variantId: string): Promise<CartDto> {
  return request<CartDto>(`/api/cart/items/${variantId}`, { method: "DELETE" });
}

export interface CheckoutInput {
  email: string;
  fullName: string;
  line1: string;
  city: string;
  country: string;
  phoneNumber: string;
  line2?: string;
  postalCode?: string;
}

export function checkout(input: CheckoutInput): Promise<CheckoutResult> {
  return request<CheckoutResult>("/api/orders/checkout", {
    method: "POST",
    body: JSON.stringify(input),
  });
}

export function fetchOrder(orderId: string): Promise<OrderDto> {
  return request<OrderDto>(`/api/orders/${orderId}`);
}
