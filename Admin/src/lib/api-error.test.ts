import test from "node:test";
import assert from "node:assert/strict";

import { pickApiErrorMessage } from "./api-error.ts";

test("pickApiErrorMessage joins field validation errors with a space", () => {
  const message = pickApiErrorMessage(
    { errors: { Name: ["Name is required."], Sku: ["SKU is taken."] } },
    400,
    "Failed",
  );
  assert.equal(message, "Name is required. SKU is taken.");
});

test("pickApiErrorMessage prefers detail over message and title", () => {
  assert.equal(
    pickApiErrorMessage({ detail: "boom", message: "msg", title: "t" }, 400, "Failed"),
    "boom",
  );
});

test("pickApiErrorMessage falls back message then title", () => {
  assert.equal(pickApiErrorMessage({ message: "msg", title: "t" }, 400, "Failed"), "msg");
  assert.equal(pickApiErrorMessage({ title: "t" }, 400, "Failed"), "t");
});

test("pickApiErrorMessage uses the status fallback when body is empty", () => {
  assert.equal(pickApiErrorMessage({}, 500, "Failed"), "Failed (500).");
});

test("pickApiErrorMessage ignores an empty errors object", () => {
  assert.equal(pickApiErrorMessage({ errors: {}, title: "t" }, 400, "Failed"), "t");
});
