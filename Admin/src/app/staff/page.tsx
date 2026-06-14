import { getStaff, getStaffRoles } from "@/actions/staff";
import { StaffActions } from "@/components/StaffActions";
import { StaffForm } from "@/components/StaffForm";

export const metadata = {
  title: "Staff — Nika Admin",
};

function ActiveBadge({ active }: { active: boolean }) {
  return (
    <span className={`inline-block px-2 py-0.5 text-xs font-medium ${active ? "bg-success/15 text-success" : "bg-danger/15 text-danger"}`}>
      {active ? "Active" : "Inactive"}
    </span>
  );
}

export default async function StaffPage() {
  const [staff, roles] = await Promise.all([getStaff(), getStaffRoles()]);
  const staffItems = staff.data ?? [];
  const roleItems = roles.data ?? [];

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold sm:text-3xl">Staff</h1>
        <p className="mt-1 text-sm text-muted">Manage admin users, roles, and active access.</p>
      </div>

      {staff.error || roles.error ? (
        <p className="border border-danger/40 bg-danger/10 p-4 text-sm text-danger">{staff.error ?? roles.error}</p>
      ) : null}

      <StaffForm roles={roleItems} />

      <div className="overflow-x-auto border border-border-soft">
        <table className="w-full text-left text-sm">
          <thead className="bg-surface-2 text-muted">
            <tr>
              <th className="px-4 py-3 font-medium">Staff member</th>
              <th className="px-4 py-3 font-medium">Roles</th>
              <th className="px-4 py-3 font-medium">Status</th>
              <th className="px-4 py-3 font-medium">Edit</th>
            </tr>
          </thead>
          <tbody>
            {staffItems.length === 0 ? (
              <tr><td className="px-4 py-6 text-muted" colSpan={4}>No staff found.</td></tr>
            ) : staffItems.map((member) => (
              <tr key={member.id} className="border-t border-border-soft bg-surface align-top">
                <td className="px-4 py-3">
                  <p className="font-medium">{member.fullName}</p>
                  <p className="text-xs text-muted">{member.email}</p>
                </td>
                <td className="px-4 py-3 text-muted">{member.roles.join(", ") || "—"}</td>
                <td className="px-4 py-3"><ActiveBadge active={member.isActive} /></td>
                <td className="px-4 py-3">
                  <StaffActions
                    staffId={member.id}
                    isActive={member.isActive}
                    currentRoles={member.roles}
                    roles={roleItems}
                  />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
