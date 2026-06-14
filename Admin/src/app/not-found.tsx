import Link from "next/link";

export default function NotFound() {
  return (
    <div className="mx-auto flex max-w-md flex-col items-center gap-4 py-16 text-center">
      <p className="text-4xl font-extrabold tracking-tight">404</p>
      <h1 className="text-2xl font-bold">Page not found</h1>
      <p className="text-sm text-muted">
        The page you’re looking for doesn’t exist or may have been moved.
      </p>
      <Link
        href="/"
        className="bg-accent-strong px-4 py-2 text-sm font-semibold text-white transition hover:bg-accent"
      >
        Go to dashboard
      </Link>
    </div>
  );
}
