import Link from "next/link";
import { CartLink } from "@/components/CartLink";

export function SiteHeader() {
  return (
    <header className="sticky top-0 z-10 border-b border-border-soft bg-background/80 backdrop-blur">
      <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-4">
        <Link href="/" className="text-xl font-black tracking-tight">
          NIKA<span className="text-accent">FITNESS</span>
        </Link>
        <nav className="flex items-center gap-6 text-sm">
          <Link href="/products" className="text-muted transition hover:text-accent">
            Shop
          </Link>
          <CartLink />
        </nav>
      </div>
    </header>
  );
}
