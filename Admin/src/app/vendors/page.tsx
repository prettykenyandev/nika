import Link from "next/link";
import { getVendors } from "@/actions/vendors";
import type { VendorSummaryDto } from "@/lib/types";

export const metadata = {
  title: "Vendors — Nika Admin",
};

function ActiveBadge({ active }: { active: boolean }) {
  return (
    <span
      className={`inline-block px-2 py-0.5 text-xs font-medium ${
        active ? "bg-success/15 text-success" : "bg-danger/15 text-danger"
      }`}
    >
      {active ? "Active" : "Inactive"}
    </span>
  );
}

export default async function VendorsPage({
  searchParams,
}: {
  searchParams: Promise<{ search?: string; activeOnly?: string }>;
}) {
  const params = await searchParams;
  const search = params.search?.trim() ?? "";
  const activeOnly = params.activeOnly === "true";
  const vendors = await getVendors({ search, activeOnly });
  const items: VendorSummaryDto[] = vendors.data?.items ?? [];
  const loadError = vendors.error ?? null;

  return (
    <div className="flex flex-col gap-6">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold sm:text-3xl">Vendors</h1>
          <p className="mt-1 text-sm text-muted">
            Manage suppliers and payment terms for purchasing.
          </p>
        </div>
        <Link
          href="/vendors/new"
          className="bg-accent-strong px-4 py-2 font-semibold text-white transition hover:bg-accent"
        >
          + New vendor
        </Link>
      </div>

      <form className="grid gap-3 border border-border-soft bg-surface p-4 sm:grid-cols-[1fr_auto_auto]">
        <input
          name="search"
          defaultValue={search}
          placeholder="Search vendors"
          className="w-full border border-border-soft bg-surface-2 px-3 py-2 text-sm outline-none focus:border-accent"
        />
        <label className="flex items-center gap-2 text-sm text-muted">
          <input
            type="checkbox"
            name="activeOnly"
            value="true"
            defaultChecked={activeOnly}
          />
          Active only
        </label>
        <button
          type="submit"
          className="border border-border-soft px-4 py-2 text-sm text-muted transition hover:border-accent hover:text-accent"
        >
          Filter
        </button>
      </form>

      {loadError ? (
        <p className="border border-danger/40 bg-danger/10 p-4 text-sm text-danger">
          {loadError}
        </p>
      ) : items.length === 0 ? (
        <p className="border border-border-soft bg-surface p-6 text-sm text-muted">
          No vendors found. Use{" "}
          <Link href="/vendors/new" className="text-accent underline">
            New vendor
          </Link>{" "}
          to add your first supplier.
        </p>
      ) : (
        <div className="overflow-x-auto border border-border-soft">
          <table className="w-full text-left text-sm">
            <thead className="bg-surface-2 text-muted">
              <tr>
                <th className="px-4 py-3 font-medium">Name</th>
                <th className="px-4 py-3 font-medium">Contact</th>
                <th className="px-4 py-3 font-medium">Email</th>
                <th className="px-4 py-3 text-right font-medium">Terms</th>
                <th className="px-4 py-3 font-medium">Status</th>
              </tr>
            </thead>
            <tbody>
              {items.map((vendor) => (
                <tr key={vendor.id} className="border-t border-border-soft bg-surface">
                  <td className="px-4 py-3">
                    <Link
                      href={`/vendors/${vendor.id}`}
                      className="font-medium text-accent underline"
                    >
                      {vendor.name}
                    </Link>
                  </td>
                  <td className="px-4 py-3 text-muted">
                    {vendor.contactName ?? vendor.phone ?? "—"}
                  </td>
                  <td className="px-4 py-3 text-muted">{vendor.email ?? "—"}</td>
                  <td className="px-4 py-3 text-right">
                    {vendor.paymentTermDays} days
                  </td>
                  <td className="px-4 py-3">
                    <ActiveBadge active={vendor.isActive} />
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
