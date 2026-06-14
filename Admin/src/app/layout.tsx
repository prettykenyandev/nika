import type { Metadata } from "next";
import Link from "next/link";
import "./globals.css";
import { getAdminUser } from "@/lib/auth";
import { logoutAction } from "@/actions/auth";

export const metadata: Metadata = {
  title: "Nika Fitness — Admin",
  description: "Inventory management for the Nika Fitness store.",
};

export default async function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const user = await getAdminUser();

  return (
    <html lang="en">
      <body className="min-h-screen">
        <header className="border-b border-border-soft bg-surface/60 backdrop-blur">
          <div className="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-3 px-4 py-3 sm:px-6">
            <Link href="/" className="text-lg font-extrabold tracking-tight">
              NIKA<span className="text-accent">ADMIN</span>
            </Link>

            {user ? (
              <nav className="flex flex-wrap items-center gap-2 text-sm sm:gap-4">
                <Link
                  href="/"
                  className="text-muted transition hover:text-accent"
                >
                  Dashboard
                </Link>
                <Link
                  href="/pos"
                  className="text-muted transition hover:text-accent"
                >
                  POS
                </Link>
                <Link
                  href="/expenses"
                  className="text-muted transition hover:text-accent"
                >
                  Expenses
                </Link>
                <Link
                  href="/invoices"
                  className="text-muted transition hover:text-accent"
                >
                  Invoices
                </Link>
                <Link
                  href="/settings"
                  className="text-muted transition hover:text-accent"
                >
                  Settings
                </Link>
                <Link
                  href="/products/new"
                  className="bg-accent-strong px-3 py-1.5 font-semibold text-white transition hover:bg-accent"
                >
                  + New product
                </Link>
                <span className="hidden text-muted sm:inline">
                  {user.fullName}
                </span>
                <form action={logoutAction}>
                  <button
                    type="submit"
                    className="border border-border-soft px-3 py-1.5 text-muted transition hover:border-accent hover:text-accent"
                  >
                    Log out
                  </button>
                </form>
              </nav>
            ) : null}
          </div>
        </header>

        <main className="mx-auto w-full max-w-6xl px-4 py-6 sm:px-6 sm:py-10">
          {children}
        </main>
      </body>
    </html>
  );
}
