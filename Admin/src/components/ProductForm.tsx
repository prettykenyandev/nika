"use client";

import { useState, useTransition } from "react";
import {
  createCategoryAction,
  createProductAction,
  type CreateProductState,
} from "@/actions/products";
import type { CategoryDto } from "@/lib/types";

const STOREFRONT_URL =
  process.env.NEXT_PUBLIC_STOREFRONT_URL ?? "http://localhost:3000";

interface VariantRow {
  sku: string;
  name: string;
  price: string;
  stockQuantity: string;
}

const emptyVariant: VariantRow = { sku: "", name: "", price: "", stockQuantity: "" };

const inputClass =
  "w-full border border-border-soft bg-surface-2 px-3 py-2 text-sm outline-none focus:border-accent";

export function ProductForm({ categories: initial }: { categories: CategoryDto[] }) {
  const [categories, setCategories] = useState<CategoryDto[]>(initial);
  const [categoryId, setCategoryId] = useState<string>(initial[0]?.id ?? "");
  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [publish, setPublish] = useState(true);
  const [imageUrls, setImageUrls] = useState<string[]>([""]);
  const [variants, setVariants] = useState<VariantRow[]>([{ ...emptyVariant }]);
  const [result, setResult] = useState<CreateProductState>({});
  const [pending, startTransition] = useTransition();

  // Inline "add category"
  const [showNewCategory, setShowNewCategory] = useState(false);
  const [newCategory, setNewCategory] = useState("");
  const [catError, setCatError] = useState<string | null>(null);
  const [catPending, startCatTransition] = useTransition();

  function updateImage(index: number, value: string) {
    setImageUrls((prev) => prev.map((u, i) => (i === index ? value : u)));
  }
  function addImage() {
    setImageUrls((prev) => [...prev, ""]);
  }
  function removeImage(index: number) {
    setImageUrls((prev) => prev.filter((_, i) => i !== index));
  }

  function updateVariant(index: number, field: keyof VariantRow, value: string) {
    setVariants((prev) =>
      prev.map((v, i) => (i === index ? { ...v, [field]: value } : v)),
    );
  }
  function addVariant() {
    setVariants((prev) => [...prev, { ...emptyVariant }]);
  }
  function removeVariant(index: number) {
    setVariants((prev) => prev.filter((_, i) => i !== index));
  }

  function handleAddCategory() {
    const trimmed = newCategory.trim();
    if (!trimmed) return;
    setCatError(null);
    startCatTransition(async () => {
      const res = await createCategoryAction(trimmed, null);
      if (res.error || !res.id) {
        setCatError(res.error ?? "Could not create category.");
        return;
      }
      const created: CategoryDto = {
        id: res.id,
        name: trimmed,
        slug: trimmed.toLowerCase().replace(/[^a-z0-9]+/g, "-"),
        description: null,
      };
      setCategories((prev) => [...prev, created]);
      setCategoryId(created.id);
      setNewCategory("");
      setShowNewCategory(false);
    });
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setResult({});
    startTransition(async () => {
      const res = await createProductAction({
        name,
        description,
        categoryId,
        imageUrls,
        variants: variants.map((v) => ({
          sku: v.sku,
          name: v.name,
          price: Number(v.price),
          stockQuantity: Number(v.stockQuantity),
        })),
        publish,
      });
      setResult(res);
      if (res.success) {
        setName("");
        setDescription("");
        setImageUrls([""]);
        setVariants([{ ...emptyVariant }]);
      }
    });
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-6">
      {result.success ? (
        <div className="border border-success/40 bg-success/10 p-4 text-sm">
          <p className="font-semibold text-success">
            “{result.success.name}” was created.
          </p>
          <a
            href={`${STOREFRONT_URL}/products/${result.success.slug}`}
            target="_blank"
            rel="noreferrer"
            className="mt-1 inline-block text-accent underline"
          >
            View it on the storefront →
          </a>
        </div>
      ) : null}

      {result.error ? (
        <p className="border border-danger/40 bg-danger/10 p-4 text-sm text-danger">
          {result.error}
        </p>
      ) : null}

      {/* Details */}
      <section className="flex flex-col gap-4 border border-border-soft bg-surface p-4 sm:p-6">
        <h2 className="text-lg font-semibold">Details</h2>

        <label className="flex flex-col gap-1 text-sm">
          <span className="text-muted">Product name</span>
          <input
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="e.g. Nika Training Tee"
            className={inputClass}
            required
          />
        </label>

        <label className="flex flex-col gap-1 text-sm">
          <span className="text-muted">Description</span>
          <textarea
            value={description}
            onChange={(e) => setDescription(e.target.value)}
            placeholder="Short description shown on the product page."
            rows={3}
            className={inputClass}
            required
          />
        </label>

        <div className="flex flex-col gap-1 text-sm">
          <span className="text-muted">Category</span>
          <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
            <select
              value={categoryId}
              onChange={(e) => setCategoryId(e.target.value)}
              className={`${inputClass} sm:flex-1`}
              required
            >
              {categories.length === 0 ? (
                <option value="">No categories yet</option>
              ) : null}
              {categories.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.name}
                </option>
              ))}
            </select>
            <button
              type="button"
              onClick={() => setShowNewCategory((v) => !v)}
              className="border border-border-soft px-3 py-2 text-sm text-muted transition hover:border-accent hover:text-accent"
            >
              {showNewCategory ? "Cancel" : "+ New category"}
            </button>
          </div>

          {showNewCategory ? (
            <div className="mt-2 flex flex-col gap-2 border border-border-soft bg-surface-2 p-3 sm:flex-row">
              <input
                value={newCategory}
                onChange={(e) => setNewCategory(e.target.value)}
                placeholder="New category name"
                className={`${inputClass} sm:flex-1`}
              />
              <button
                type="button"
                onClick={handleAddCategory}
                disabled={catPending}
                className="bg-accent-strong px-4 py-2 text-sm font-semibold text-white transition hover:bg-accent disabled:opacity-50"
              >
                {catPending ? "Adding…" : "Add"}
              </button>
            </div>
          ) : null}
          {catError ? <p className="text-xs text-danger">{catError}</p> : null}
        </div>
      </section>

      {/* Images */}
      <section className="flex flex-col gap-4 border border-border-soft bg-surface p-4 sm:p-6">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold">Images</h2>
          <button
            type="button"
            onClick={addImage}
            className="text-sm text-accent underline"
          >
            + Add image
          </button>
        </div>
        <p className="text-xs text-muted">
          Paste image URLs (the first one is used as the main thumbnail).
        </p>
        {imageUrls.map((url, i) => (
          <div key={i} className="flex items-center gap-2">
            <input
              value={url}
              onChange={(e) => updateImage(i, e.target.value)}
              placeholder="https://…"
              className={inputClass}
            />
            {imageUrls.length > 1 ? (
              <button
                type="button"
                onClick={() => removeImage(i)}
                className="shrink-0 border border-border-soft px-3 py-2 text-sm text-muted transition hover:border-danger hover:text-danger"
                aria-label="Remove image"
              >
                ✕
              </button>
            ) : null}
          </div>
        ))}
      </section>

      {/* Variants */}
      <section className="flex flex-col gap-4 border border-border-soft bg-surface p-4 sm:p-6">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold">Variants &amp; pricing</h2>
          <button
            type="button"
            onClick={addVariant}
            className="text-sm text-accent underline"
          >
            + Add variant
          </button>
        </div>
        <p className="text-xs text-muted">
          Each variant (e.g. a size or colour) needs a unique SKU, a price and a
          stock count.
        </p>

        <div className="flex flex-col gap-4">
          {variants.map((v, i) => (
            <div
              key={i}
              className="grid grid-cols-1 gap-3 border border-border-soft bg-surface-2 p-3 sm:grid-cols-2 lg:grid-cols-[1fr_1.4fr_0.9fr_0.8fr_auto] lg:items-end"
            >
              <label className="flex flex-col gap-1 text-xs">
                <span className="text-muted">SKU</span>
                <input
                  value={v.sku}
                  onChange={(e) => updateVariant(i, "sku", e.target.value)}
                  placeholder="TEE-BLU-M"
                  className={inputClass}
                />
              </label>
              <label className="flex flex-col gap-1 text-xs">
                <span className="text-muted">Variant name</span>
                <input
                  value={v.name}
                  onChange={(e) => updateVariant(i, "name", e.target.value)}
                  placeholder="Blue / Medium"
                  className={inputClass}
                />
              </label>
              <label className="flex flex-col gap-1 text-xs">
                <span className="text-muted">Price (Ksh)</span>
                <input
                  type="number"
                  min="0"
                  step="1"
                  inputMode="decimal"
                  value={v.price}
                  onChange={(e) => updateVariant(i, "price", e.target.value)}
                  placeholder="2500"
                  className={inputClass}
                />
              </label>
              <label className="flex flex-col gap-1 text-xs">
                <span className="text-muted">Stock</span>
                <input
                  type="number"
                  min="0"
                  step="1"
                  inputMode="numeric"
                  value={v.stockQuantity}
                  onChange={(e) =>
                    updateVariant(i, "stockQuantity", e.target.value)
                  }
                  placeholder="20"
                  className={inputClass}
                />
              </label>
              {variants.length > 1 ? (
                <button
                  type="button"
                  onClick={() => removeVariant(i)}
                  className="h-fit border border-border-soft px-3 py-2 text-sm text-muted transition hover:border-danger hover:text-danger"
                >
                  Remove
                </button>
              ) : (
                <span className="hidden lg:block" />
              )}
            </div>
          ))}
        </div>
      </section>

      {/* Publish + submit */}
      <section className="flex flex-col gap-4 border border-border-soft bg-surface p-4 sm:flex-row sm:items-center sm:justify-between sm:p-6">
        <label className="flex items-center gap-2 text-sm">
          <input
            type="checkbox"
            checked={publish}
            onChange={(e) => setPublish(e.target.checked)}
            className="h-4 w-4 accent-[var(--accent-strong)]"
          />
          <span>Publish immediately (show on storefront)</span>
        </label>
        <button
          type="submit"
          disabled={pending}
          className="bg-accent-strong px-6 py-3 font-semibold text-white transition hover:bg-accent disabled:cursor-not-allowed disabled:opacity-50"
        >
          {pending ? "Saving…" : "Create product"}
        </button>
      </section>
    </form>
  );
}
