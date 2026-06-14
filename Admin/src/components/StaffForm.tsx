"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { createStaffAction } from "@/actions/staff";

const inputClass =
  "w-full border border-border-soft bg-surface-2 px-3 py-2 text-sm outline-none focus:border-accent";

export function StaffForm({ roles }: { roles: string[] }) {
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [fullName, setFullName] = useState("");
  const [password, setPassword] = useState("");
  const [selectedRoles, setSelectedRoles] = useState<string[]>(roles[0] ? [roles[0]] : []);
  const [error, setError] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();

  function toggleRole(role: string) {
    setSelectedRoles((prev) =>
      prev.includes(role) ? prev.filter((r) => r !== role) : [...prev, role],
    );
  }

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    startTransition(async () => {
      const res = await createStaffAction({ email, fullName, password, roles: selectedRoles });
      if (res.error) {
        setError(res.error);
        return;
      }
      setEmail("");
      setFullName("");
      setPassword("");
      setSelectedRoles(roles[0] ? [roles[0]] : []);
      router.refresh();
    });
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4 border border-border-soft bg-surface p-4 sm:p-6">
      <h2 className="text-lg font-semibold">Add staff</h2>
      {error ? (
        <p className="border border-danger/40 bg-danger/10 p-3 text-sm text-danger">
          {error}
        </p>
      ) : null}
      <div className="grid gap-4 sm:grid-cols-3">
        <label className="flex flex-col gap-1 text-sm">
          <span className="text-muted">Email *</span>
          <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} className={inputClass} required />
        </label>
        <label className="flex flex-col gap-1 text-sm">
          <span className="text-muted">Full name *</span>
          <input value={fullName} onChange={(e) => setFullName(e.target.value)} className={inputClass} required />
        </label>
        <label className="flex flex-col gap-1 text-sm">
          <span className="text-muted">Password *</span>
          <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} className={inputClass} required />
        </label>
      </div>
      <div className="flex flex-wrap gap-3">
        {roles.map((role) => (
          <label key={role} className="flex items-center gap-2 text-sm text-muted">
            <input
              type="checkbox"
              checked={selectedRoles.includes(role)}
              onChange={() => toggleRole(role)}
            />
            {role}
          </label>
        ))}
      </div>
      <div>
        <button
          type="submit"
          disabled={pending}
          className="bg-accent-strong px-5 py-2 font-semibold text-white transition hover:bg-accent disabled:opacity-50"
        >
          {pending ? "Creating…" : "Add staff"}
        </button>
      </div>
    </form>
  );
}
