import Link from "next/link";
import { notFound } from "next/navigation";
import { getVendor, setVendorActiveAction } from "@/actions/vendors";
import { VendorForm } from "@/components/VendorForm";

export const metadata = {
  title: "Vendor — Nika Admin",
};

export default async function VendorDetailPage({
  params,
}: {
  params: Promise<{ id: string }>;
}) {
  const { id } = await params;
  const vendor = await getVendor(id);
  if (!vendor) notFound();
  const currentVendor = vendor;

  async function toggleActive() {
    "use server";
    await setVendorActiveAction(currentVendor.id, !currentVendor.isActive);
  }

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Link href="/vendors" className="text-sm text-accent underline">
          ← Back to vendors
        </Link>
        <div className="mt-2 flex flex-wrap items-center justify-between gap-3">
          <div>
            <h1 className="text-2xl font-bold sm:text-3xl">{currentVendor.name}</h1>
            <p className="mt-1 text-sm text-muted">
              {currentVendor.contactName ?? "No contact"}
              {currentVendor.email ? ` · ${currentVendor.email}` : ""}
            </p>
          </div>
          <form action={toggleActive}>
            <button
              type="submit"
              className={`border px-4 py-2 text-sm transition ${
                currentVendor.isActive
                  ? "border-border-soft text-muted hover:border-danger hover:text-danger"
                  : "border-border-soft text-muted hover:border-accent hover:text-accent"
              }`}
            >
              {currentVendor.isActive ? "Deactivate" : "Activate"}
            </button>
          </form>
        </div>
      </div>

      <VendorForm vendor={currentVendor} />
    </div>
  );
}
