import test from "node:test";
import assert from "node:assert/strict";

import { apiErrorMessage } from "./client.ts";

test("apiErrorMessage prefers the first field validation error", () => {
  const message = apiErrorMessage(
    { title: "Validation failed", errors: { Email: ["Email is required."] } },
    400,
  );
  assert.equal(message, "Email is required.");
});

test("apiErrorMessage falls back to the title when there are no field errors", () => {
  assert.equal(apiErrorMessage({ title: "Not found" }, 404), "Not found");
});

test("apiErrorMessage falls back to a status message when body is empty", () => {
  assert.equal(apiErrorMessage({}, 500), "Request failed (500).");
});

test("apiErrorMessage flattens across fields, taking the first", () => {
  const message = apiErrorMessage(
    { errors: { A: [], B: ["second field error"] } },
    422,
  );
  assert.equal(message, "second field error");
});
