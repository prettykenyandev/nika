import test from "node:test";
import assert from "node:assert/strict";

import { buildProductsQuery } from "./api.ts";

test("buildProductsQuery returns empty string when no params", () => {
  assert.equal(buildProductsQuery({}), "");
});

test("buildProductsQuery includes only the provided params", () => {
  assert.equal(buildProductsQuery({ category: "tops" }), "?category=tops");
  assert.equal(buildProductsQuery({ page: 2 }), "?page=2");
});

test("buildProductsQuery ignores falsy page (0) and empty strings", () => {
  assert.equal(buildProductsQuery({ page: 0 }), "");
  assert.equal(buildProductsQuery({ category: "" }), "");
});

test("buildProductsQuery encodes values and combines params", () => {
  assert.equal(
    buildProductsQuery({ category: "men tops", search: "t&t", page: 3 }),
    "?category=men+tops&search=t%26t&page=3",
  );
});
