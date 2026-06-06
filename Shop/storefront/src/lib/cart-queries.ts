"use client";

import {
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import {
  addToCart,
  fetchCart,
  removeCartItem,
  updateCartItem,
} from "@/lib/client";
import type { CartDto } from "@/lib/types";

const CART_KEY = ["cart"] as const;

export function useCart() {
  return useQuery({
    queryKey: CART_KEY,
    queryFn: fetchCart,
    staleTime: 30_000,
  });
}

export function useAddToCart() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ variantId, quantity }: { variantId: string; quantity: number }) =>
      addToCart(variantId, quantity),
    onSuccess: (cart) => queryClient.setQueryData<CartDto>(CART_KEY, cart),
  });
}

export function useUpdateCartItem() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({ variantId, quantity }: { variantId: string; quantity: number }) =>
      updateCartItem(variantId, quantity),
    onSuccess: (cart) => queryClient.setQueryData<CartDto>(CART_KEY, cart),
  });
}

export function useRemoveCartItem() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (variantId: string) => removeCartItem(variantId),
    onSuccess: (cart) => queryClient.setQueryData<CartDto>(CART_KEY, cart),
  });
}
