"use client";

import { useState, useTransition } from "react";
import { createExpenseCategoryAction } from "@/actions/expenses";
import type { ExpenseCategoryDto } from "@/lib/types";

const inputClass =
  "w-full border border-border-soft bg-surface-2 px-3 py-2 text-sm outline-none focus:border-accent";

export function ExpenseCategoryManager({
  initial,
}: {
  initial: ExpenseCategoryDto[];
}) {
  const [categories, setCategories] = useState(initial);
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();

  function addCategory(e: React.FormEvent) {
    e.preventDefault();
    const trimmed = name.trim();
    if (!trimmed) return;
    setError(null);
    startTransition(async () => {
      const res = await createExpenseCategoryAction(trimmed, description || null);
      if (res.error || !res.id) {
        setError(res.error ?? "Could not create category.");
        return;
      }
      setCategories((prev) =>
        [
          ...prev,
          {
            id: res.id!,
            name: trimmed,
            slug: trimmed.toLowerCase().replace(/[^a-z0-9]+/g, "-"),
            description: description || null,
          },
        ].sort((a, b) => a.name.localeCompare(b.name)),
      );
      setName("");
      setDescription("");
    });
  }

  return (
    <div className="grid gap-6 lg:grid-cols-[1fr_1.4fr]">
      <form
        onSubmit={addCategory}
        className="flex h-fit flex-col gap-3 border border-border-soft bg-surface p-4"
      >
        <h2 className="text-sm font-semibold">New category</h2>
        <label className="flex flex-col gap-1 text-xs">
          <span className="text-muted">Name *</span>
          <input
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="e.g. Rent"
            className={inputClass}
            required
          />
        </label>
        <label className="flex flex-col gap-1 text-xs">
          <span className="text-muted">Description</span>
          <textarea
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            rows={2}
            className={inputClass}
          />
        </label>
        {error ? <p className="text-xs text-danger">{error}</p> : null}
        <button
          type="submit"
          disabled={pending}
          className="bg-accent-strong px-4 py-2 text-sm font-semibold text-white transition hover:bg-accent disabled:opacity-50"
        >
          {pending ? "Adding…" : "Add category"}
        </button>
      </form>

      <div className="border border-border-soft bg-surface">
        {categories.length === 0 ? (
          <p className="p-6 text-sm text-muted">No categories yet.</p>
        ) : (
          <table className="w-full text-left text-sm">
            <thead className="bg-surface-2 text-muted">
              <tr>
                <th className="px-4 py-3 font-medium">Name</th>
                <th className="px-4 py-3 font-medium">Description</th>
              </tr>
            </thead>
            <tbody>
              {categories.map((c) => (
                <tr key={c.id} className="border-t border-border-soft">
                  <td className="px-4 py-3 font-medium">{c.name}</td>
                  <td className="px-4 py-3 text-muted">{c.description ?? "—"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
}
