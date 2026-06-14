import Link from "next/link";
import { CustomerForm } from "@/components/CustomerForm";

export const metadata = {
  title: "New customer — Nika Admin",
};

export default function NewCustomerPage() {
  return (
    <div className="flex flex-col gap-6">
      <div>
        <Link href="/customers" className="text-sm text-accent underline">
          ← Back to customers
        </Link>
        <h1 className="mt-2 text-2xl font-bold sm:text-3xl">New customer</h1>
        <p className="mt-1 text-sm text-muted">Add a CRM record for invoicing and order tracking.</p>
      </div>
      <CustomerForm />
    </div>
  );
}
