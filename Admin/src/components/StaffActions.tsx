"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { setStaffActiveAction, updateStaffRolesAction } from "@/actions/staff";

export function StaffActions({
  staffId,
  isActive,
  currentRoles,
  roles,
}: {
  staffId: string;
  isActive: boolean;
  currentRoles: string[];
  roles: string[];
}) {
  const router = useRouter();
  const [selectedRoles, setSelectedRoles] = useState<string[]>(currentRoles);
  const [error, setError] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();

  function toggleRole(role: string) {
    setSelectedRoles((prev) =>
      prev.includes(role) ? prev.filter((r) => r !== role) : [...prev, role],
    );
  }

  function saveRoles() {
    setError(null);
    startTransition(async () => {
      const res = await updateStaffRolesAction(staffId, selectedRoles);
      if (res.error) setError(res.error);
      else router.refresh();
    });
  }

  function toggleActive() {
    setError(null);
    startTransition(async () => {
      const res = await setStaffActiveAction(staffId, !isActive);
      if (res.error) setError(res.error);
      else router.refresh();
    });
  }

  return (
    <div className="flex min-w-64 flex-col gap-3">
      {error ? <p className="text-sm text-danger">{error}</p> : null}
      <div className="flex flex-wrap gap-2">
        {roles.map((role) => (
          <label key={role} className="flex items-center gap-1 text-xs text-muted">
            <input
              type="checkbox"
              checked={selectedRoles.includes(role)}
              onChange={() => toggleRole(role)}
            />
            {role}
          </label>
        ))}
      </div>
      <div className="flex flex-wrap gap-2">
        <button
          type="button"
          onClick={saveRoles}
          disabled={pending}
          className="border border-border-soft px-3 py-1.5 text-xs text-muted transition hover:border-accent hover:text-accent disabled:opacity-50"
        >
          Save roles
        </button>
        <button
          type="button"
          onClick={toggleActive}
          disabled={pending}
          className="border border-border-soft px-3 py-1.5 text-xs text-muted transition hover:border-danger hover:text-danger disabled:opacity-50"
        >
          {isActive ? "Deactivate" : "Activate"}
        </button>
      </div>
    </div>
  );
}
