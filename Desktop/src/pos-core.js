"use strict";

/**
 * Pure point-of-sale helpers with no Electron / IO / network dependencies.
 *
 * These were previously inlined inside main.js's IPC handlers. Extracting them
 * keeps the calculation/validation/mapping rules in one testable place and
 * removes the duplication that had crept between the sale and sync code paths.
 */

const DEFAULT_CURRENCY = "KES";

// M-Pesa consumer numbers in the 254 country format (Safaricom/Airtel ranges).
const MPESA_PHONE_PATTERN = /^(2547|2541)\d{8}$/;

/** Normalise a scanned/typed SKU to the canonical trimmed-uppercase form. */
function normalizeSku(sku) {
  return String(sku || "").trim().toUpperCase();
}

/** True when the phone is a valid 254-format M-Pesa number. */
function isValidMpesaPhone(phone) {
  return MPESA_PHONE_PATTERN.test(String(phone || "").trim());
}

/** Sum of price × quantity across every sale line. */
function saleTotal(items) {
  return (items || []).reduce((sum, item) => sum + item.variant.price * item.quantity, 0);
}

/** Currency of the sale, taken from the first line (defaults to KES). */
function saleCurrency(items) {
  return items?.[0]?.variant.currency || DEFAULT_CURRENCY;
}

/**
 * Whether a stored auth session is currently usable. An expired token counts
 * as logged-out so callers re-authenticate before selling.
 */
function isSessionActive(auth, now = new Date()) {
  if (!auth?.token) return false;
  if (auth.expiresAtUtc && new Date(auth.expiresAtUtc) <= now) return false;
  return true;
}

/**
 * Return a copy of the SKU cache with the stock of every entry matching
 * `variantId` adjusted by `delta` (never below zero).
 */
function adjustCacheStock(cache, variantId, delta) {
  const next = { ...cache };
  for (const key of Object.keys(next)) {
    if (next[key].variantId === variantId) {
      next[key] = {
        ...next[key],
        stockQuantity: Math.max(0, next[key].stockQuantity + delta),
      };
    }
  }
  return next;
}

/** Map internal sale lines to the API's item shape. */
function toApiSaleItems(items) {
  return items.map((item) => ({
    productVariantId: item.variant.variantId,
    quantity: item.quantity,
  }));
}

/** Build the request body the API expects for a sale. */
function buildSalePayload(items, method, customerPhone) {
  return {
    items: toApiSaleItems(items),
    method,
    customerPhone: method === "Mpesa" ? customerPhone : null,
    customerEmail: null,
  };
}

module.exports = {
  DEFAULT_CURRENCY,
  MPESA_PHONE_PATTERN,
  normalizeSku,
  isValidMpesaPhone,
  saleTotal,
  saleCurrency,
  isSessionActive,
  adjustCacheStock,
  toApiSaleItems,
  buildSalePayload,
};
