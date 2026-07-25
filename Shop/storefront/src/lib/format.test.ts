import test from "node:test";
import assert from "node:assert/strict";

import { formatMoney } from "./format.ts";

test("formatMoney renders a currency string for KES", () => {
  const result = formatMoney(1500, "KES");
  assert.match(result, /1,500/);
});

test("formatMoney has no fractional digits", () => {
  const result = formatMoney(1999.99, "KES");
  assert.doesNotMatch(result, /\./);
});

test("formatMoney falls back gracefully for an unknown currency code", () => {
  const result = formatMoney(1000, "NOTACURRENCY");
  assert.match(result, /NOTACURRENCY/);
  assert.match(result, /1,000/);
});
