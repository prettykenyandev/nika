"use strict";

const test = require("node:test");
const assert = require("node:assert/strict");

const {
  DEFAULT_CURRENCY,
  normalizeSku,
  isValidMpesaPhone,
  saleTotal,
  saleCurrency,
  isSessionActive,
  adjustCacheStock,
  toApiSaleItems,
  buildSalePayload,
} = require("../src/pos-core");

test("normalizeSku trims and uppercases", () => {
  assert.equal(normalizeSku("  abc-123 "), "ABC-123");
  assert.equal(normalizeSku(""), "");
  assert.equal(normalizeSku(null), "");
  assert.equal(normalizeSku(undefined), "");
});

test("isValidMpesaPhone accepts 254 7/1 numbers and rejects others", () => {
  assert.equal(isValidMpesaPhone("254712345678"), true);
  assert.equal(isValidMpesaPhone("254112345678"), true);
  assert.equal(isValidMpesaPhone("  254712345678  "), true);
  assert.equal(isValidMpesaPhone("0712345678"), false);
  assert.equal(isValidMpesaPhone("254812345678"), false);
  assert.equal(isValidMpesaPhone("25471234567"), false);
  assert.equal(isValidMpesaPhone(""), false);
  assert.equal(isValidMpesaPhone(null), false);
});

const line = (price, quantity, currency) => ({
  variant: { variantId: `v${price}`, price, currency },
  quantity,
});

test("saleTotal sums price times quantity", () => {
  assert.equal(saleTotal([line(100, 2), line(50, 3)]), 350);
  assert.equal(saleTotal([]), 0);
  assert.equal(saleTotal(undefined), 0);
});

test("saleCurrency reads the first line and defaults to KES", () => {
  assert.equal(saleCurrency([line(100, 1, "USD")]), "USD");
  assert.equal(saleCurrency([line(100, 1, undefined)]), DEFAULT_CURRENCY);
  assert.equal(saleCurrency([]), DEFAULT_CURRENCY);
});

test("isSessionActive honours token presence and expiry", () => {
  const now = new Date("2026-01-01T12:00:00Z");
  assert.equal(isSessionActive(null, now), false);
  assert.equal(isSessionActive({ token: "" }, now), false);
  assert.equal(isSessionActive({ token: "t" }, now), true);
  assert.equal(
    isSessionActive({ token: "t", expiresAtUtc: "2026-01-01T13:00:00Z" }, now),
    true,
  );
  assert.equal(
    isSessionActive({ token: "t", expiresAtUtc: "2026-01-01T11:00:00Z" }, now),
    false,
  );
  assert.equal(
    isSessionActive({ token: "t", expiresAtUtc: "2026-01-01T12:00:00Z" }, now),
    false,
  );
});

test("adjustCacheStock changes matching variants and clamps at zero", () => {
  const cache = {
    "SKU-A": { variantId: "v1", stockQuantity: 5 },
    "SKU-B": { variantId: "v2", stockQuantity: 1 },
    "SKU-C": { variantId: "v1", stockQuantity: 2 },
  };
  const next = adjustCacheStock(cache, "v1", -3);

  assert.equal(next["SKU-A"].stockQuantity, 2);
  assert.equal(next["SKU-C"].stockQuantity, 0, "clamped, not negative");
  assert.equal(next["SKU-B"].stockQuantity, 1, "untouched");
  assert.equal(cache["SKU-A"].stockQuantity, 5, "input not mutated");
});

test("toApiSaleItems maps to the API shape", () => {
  const items = [{ variant: { variantId: "v1" }, quantity: 4 }];
  assert.deepEqual(toApiSaleItems(items), [{ productVariantId: "v1", quantity: 4 }]);
});

test("buildSalePayload only carries the phone for M-Pesa", () => {
  const items = [{ variant: { variantId: "v1" }, quantity: 1 }];

  assert.deepEqual(buildSalePayload(items, "Mpesa", "254712345678"), {
    items: [{ productVariantId: "v1", quantity: 1 }],
    method: "Mpesa",
    customerPhone: "254712345678",
    customerEmail: null,
  });

  assert.deepEqual(buildSalePayload(items, "Card", "254712345678"), {
    items: [{ productVariantId: "v1", quantity: 1 }],
    method: "Card",
    customerPhone: null,
    customerEmail: null,
  });
});
