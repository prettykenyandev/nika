import type { Metadata } from "next";
import type { ReactNode } from "react";
import { Providers } from "@/app/providers";
import { SiteHeader } from "@/components/SiteHeader";
import "@/app/globals.css";

export const metadata: Metadata = {
  title: {
    default: "Nika Fitness — Performance gear, fuel & equipment",
    template: "%s | Nika Fitness",
  },
  description:
    "Shop Nika Fitness gym clothes and accessories. Fast checkout with M-Pesa.",
};

export default function RootLayout({ children }: { children: ReactNode }) {
  return (
    <html lang="en">
      <body className="min-h-screen">
        <Providers>
          <SiteHeader />
          <main className="mx-auto max-w-6xl px-4 py-8">{children}</main>
          <footer className="border-t border-border-soft py-8 text-center text-sm text-muted">
            © {new Date().getFullYear()} Nika Fitness. Built for the grind.
          </footer>
        </Providers>
      </body>
    </html>
  );
}
