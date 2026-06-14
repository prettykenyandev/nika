import Link from "next/link";
import { getAuditLog } from "@/actions/staff";
import { formatDate } from "@/lib/format";

export const metadata = {
  title: "Audit Log — Nika Admin",
};

export default async function AuditLogPage({
  searchParams,
}: {
  searchParams: Promise<{ search?: string; page?: string }>;
}) {
  const params = await searchParams;
  const search = params.search?.trim() ?? "";
  const page = Math.max(1, Number(params.page) || 1);
  const result = await getAuditLog({ search, page });
  const entries = result.data?.items ?? [];
  const totalPages = result.data?.totalPages ?? 1;

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold sm:text-3xl">Audit Log</h1>
        <p className="mt-1 text-sm text-muted">Review administrative actions and system changes.</p>
      </div>

      <form className="grid gap-3 border border-border-soft bg-surface p-4 sm:grid-cols-[1fr_auto]">
        <input name="search" defaultValue={search} placeholder="Search actor, action, or summary" className="w-full border border-border-soft bg-surface-2 px-3 py-2 text-sm outline-none focus:border-accent" />
        <button type="submit" className="border border-border-soft px-4 py-2 text-sm text-muted transition hover:border-accent hover:text-accent">
          Search
        </button>
      </form>

      {result.error ? (
        <p className="border border-danger/40 bg-danger/10 p-4 text-sm text-danger">{result.error}</p>
      ) : (
        <div className="overflow-x-auto border border-border-soft">
          <table className="w-full text-left text-sm">
            <thead className="bg-surface-2 text-muted">
              <tr>
                <th className="px-4 py-3 font-medium">Time</th>
                <th className="px-4 py-3 font-medium">Actor</th>
                <th className="px-4 py-3 font-medium">Action</th>
                <th className="px-4 py-3 font-medium">Summary</th>
              </tr>
            </thead>
            <tbody>
              {entries.length === 0 ? (
                <tr><td colSpan={4} className="px-4 py-6 text-muted">No audit entries found.</td></tr>
              ) : entries.map((entry) => (
                <tr key={entry.id} className="border-t border-border-soft bg-surface">
                  <td className="px-4 py-3 text-muted">{formatDate(entry.occurredAtUtc)}</td>
                  <td className="px-4 py-3">{entry.actorEmail}</td>
                  <td className="px-4 py-3 font-medium">{entry.action}</td>
                  <td className="px-4 py-3 text-muted">{entry.summary ?? "—"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      <div className="flex items-center justify-between text-sm text-muted">
        <span>Page {page} of {totalPages}</span>
        <div className="flex gap-2">
          {page > 1 ? <PageLink page={page - 1} search={search}>Previous</PageLink> : null}
          {result.data?.hasNextPage ? <PageLink page={page + 1} search={search}>Next</PageLink> : null}
        </div>
      </div>
    </div>
  );
}

function PageLink({ page, search, children }: { page: number; search: string; children: React.ReactNode }) {
  const query = new URLSearchParams({ page: String(page) });
  if (search) query.set("search", search);
  return (
    <Link href={`/audit-log?${query.toString()}`} className="border border-border-soft px-3 py-1.5 transition hover:border-accent hover:text-accent">
      {children}
    </Link>
  );
}
