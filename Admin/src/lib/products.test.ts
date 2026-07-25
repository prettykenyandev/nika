import test from "node:test";
import assert from "node:assert/strict";

import {
  toSlug,
  normalizeImageUrls,
  normalizeVariants,
  buildCreateProductPayload,
} from "./products.ts";
import type { CreateProductInput } from "./types.ts";

test("toSlug lowercases, hyphenates and trims stray hyphens", () => {
  assert.equal(toSlug("  Men's Training Tee!  "), "men-s-training-tee");
  assert.equal(toSlug("Already-Slug"), "already-slug");
  assert.equal(toSlug("multiple   spaces"), "multiple-spaces");
});

test("normalizeImageUrls trims and drops blanks", () => {
  assert.deepEqual(
    normalizeImageUrls([" a.png ", "", "   ", "b.png"]),
    ["a.png", "b.png"],
  );
});

test("normalizeVariants trims, coerces numbers and truncates stock", () => {
  const result = normalizeVariants([
    { sku: " TEE-M ", name: " Medium ", price: 1500.5 as unknown as number, stockQuantity: 3.9 as unknown as number },
  ]);
  assert.deepEqual(result, [
    { sku: "TEE-M", name: "Medium", price: 1500.5, stockQuantity: 3 },
  ]);
});

test("normalizeVariants drops rows with no sku and no name", () => {
  const result = normalizeVariants([
    { sku: "", name: "", price: 10, stockQuantity: 1 },
    { sku: "A", name: "", price: 10, stockQuantity: 1 },
  ]);
  assert.equal(result.length, 1);
  assert.equal(result[0].sku, "A");
});

const baseInput = (): CreateProductInput => ({
  name: "Training Tee",
  description: "Comfy tee",
  categoryId: "cat-1",
  imageUrls: [" img.png "],
  variants: [{ sku: "tee-m", name: "Medium", price: 1500, stockQuantity: 10 }],
  publish: true,
});

test("buildCreateProductPayload returns a normalised payload on success", () => {
  const result = buildCreateProductPayload(baseInput());
  assert.equal(result.ok, true);
  if (!result.ok) return;
  assert.deepEqual(result.payload, {
    name: "Training Tee",
    description: "Comfy tee",
    categoryId: "cat-1",
    imageUrls: ["img.png"],
    variants: [{ sku: "tee-m", name: "Medium", price: 1500, stockQuantity: 10 }],
    publish: true,
  });
});

test("buildCreateProductPayload validates required fields in order", () => {
  const expectError = (mutate: (i: CreateProductInput) => void, message: string) => {
    const input = baseInput();
    mutate(input);
    const result = buildCreateProductPayload(input);
    assert.equal(result.ok, false);
    if (!result.ok) assert.equal(result.error, message);
  };

  expectError((i) => (i.name = "  "), "Product name is required.");
  expectError((i) => (i.description = ""), "Description is required.");
  expectError((i) => (i.categoryId = ""), "Please choose a category.");
  expectError(
    (i) => (i.variants = []),
    "Add at least one variant (SKU, name, price, stock).",
  );
  expectError(
    (i) => (i.variants = [{ sku: "", name: "Medium", price: 10, stockQuantity: 1 }]),
    // row is kept (name is non-blank) then fails the SKU check
    "Every variant needs a SKU.",
  );
  expectError(
    (i) => (i.variants = [{ sku: "A", name: "", price: 10, stockQuantity: 1 }]),
    "Variant A needs a name.",
  );
  expectError(
    (i) => (i.variants = [{ sku: "A", name: "M", price: 0, stockQuantity: 1 }]),
    "Variant A needs a price greater than 0.",
  );
  expectError(
    (i) => (i.variants = [{ sku: "A", name: "M", price: 10, stockQuantity: -1 }]),
    "Variant A needs a stock quantity of 0 or more.",
  );
});
