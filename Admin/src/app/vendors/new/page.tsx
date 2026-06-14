import Link from "next/link";
import { VendorForm } from "@/components/VendorForm";

export const metadata = {
  title: "New vendor — Nika Admin",
};

export default function NewVendorPage() {
  return (
    <div className="flex flex-col gap-6">
      <div>
        <Link href="/vendors" className="text-sm text-accent underline">
          ← Back to vendors
        </Link>
        <h1 className="mt-2 text-2xl font-bold sm:text-3xl">New vendor</h1>
        <p className="mt-1 text-sm text-muted">
          Add a supplier for purchase orders and bills.
        </p>
      </div>

      <VendorForm />
    </div>
  );
}
