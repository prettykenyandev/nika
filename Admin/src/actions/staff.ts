"use server";

import { revalidatePath } from "next/cache";
import { getToken } from "@/lib/auth";
import {
  NETWORK_ERROR,
  SESSION_EXPIRED,
  readProblem,
  safeJson,
  tryFetch,
} from "@/lib/http";
import type { AuditLogEntryDto, CreateStaffInput, PagedResult, StaffDto } from "@/lib/types";

async function authHeader(): Promise<Record<string, string> | null> {
  const token = await getToken();
  if (!token) return null;
  return { Authorization: `Bearer ${token}`, Accept: "application/json" };
}

export interface StaffResult {
  data: StaffDto[] | null;
  error?: string;
}

export interface RolesResult {
  data: string[] | null;
  error?: string;
}

export async function getStaffRoles(): Promise<RolesResult> {
  const headers = await authHeader();
  if (!headers) return { data: null, error: SESSION_EXPIRED };

  const res = await tryFetch(`/api/admin/staff/roles`, { headers });
  if (!res) return { data: null, error: NETWORK_ERROR };
  if (res.status === 401) return { data: null, error: SESSION_EXPIRED };
  if (!res.ok) return { data: null, error: await readProblem(res, "Failed to load roles") };
  return { data: await safeJson<string[]>(res) };
}

export async function getStaff(): Promise<StaffResult> {
  const headers = await authHeader();
  if (!headers) return { data: null, error: SESSION_EXPIRED };

  const res = await tryFetch(`/api/admin/staff`, { headers });
  if (!res) return { data: null, error: NETWORK_ERROR };
  if (res.status === 401) return { data: null, error: SESSION_EXPIRED };
  if (!res.ok) return { data: null, error: await readProblem(res, "Failed to load staff") };
  return { data: await safeJson<StaffDto[]>(res) };
}

export interface StaffState {
  error?: string;
  id?: string;
  success?: boolean;
}

export async function createStaffAction(input: CreateStaffInput): Promise<StaffState> {
  const headers = await authHeader();
  if (!headers) return { error: SESSION_EXPIRED };
  const payload = {
    email: input.email.trim(),
    fullName: input.fullName.trim(),
    password: input.password,
    roles: input.roles,
  };
  if (!payload.email) return { error: "Email is required." };
  if (!payload.fullName) return { error: "Full name is required." };
  if (!payload.password) return { error: "Password is required." };
  if (!payload.roles.length) return { error: "Select at least one role." };

  const res = await tryFetch(`/api/admin/staff`, {
    method: "POST",
    headers: { ...headers, "Content-Type": "application/json" },
    body: JSON.stringify(payload),
  });

  if (!res) return { error: NETWORK_ERROR };
  if (res.status === 401) return { error: SESSION_EXPIRED };
  if (!res.ok) return { error: await readProblem(res, "Failed to create staff member") };

  const body = await safeJson<{ id: string }>(res);
  if (!body?.id) return { error: "The staff member was saved but the server returned no id." };

  revalidatePath("/staff");
  return { id: body.id, success: true };
}

export async function updateStaffRolesAction(
  id: string,
  roles: string[],
): Promise<StaffState> {
  const headers = await authHeader();
  if (!headers) return { error: SESSION_EXPIRED };
  if (!roles.length) return { error: "Select at least one role." };

  const res = await tryFetch(`/api/admin/staff/${id}/roles`, {
    method: "PUT",
    headers: { ...headers, "Content-Type": "application/json" },
    body: JSON.stringify({ roles }),
  });

  if (!res) return { error: NETWORK_ERROR };
  if (res.status === 401) return { error: SESSION_EXPIRED };
  if (!res.ok) return { error: await readProblem(res, "Failed to update roles") };

  revalidatePath("/staff");
  return { success: true };
}

export async function setStaffActiveAction(
  id: string,
  active: boolean,
): Promise<StaffState> {
  const headers = await authHeader();
  if (!headers) return { error: SESSION_EXPIRED };

  const res = await tryFetch(
    `/api/admin/staff/${id}/${active ? "activate" : "deactivate"}`,
    { method: "POST", headers },
  );

  if (!res) return { error: NETWORK_ERROR };
  if (res.status === 401) return { error: SESSION_EXPIRED };
  if (!res.ok) {
    return {
      error: await readProblem(
        res,
        active ? "Failed to activate staff member" : "Failed to deactivate staff member",
      ),
    };
  }

  revalidatePath("/staff");
  return { success: true };
}

export interface AuditLogResult {
  data: PagedResult<AuditLogEntryDto> | null;
  error?: string;
}

export async function getAuditLog(params?: {
  search?: string;
  page?: number;
}): Promise<AuditLogResult> {
  const headers = await authHeader();
  if (!headers) return { data: null, error: SESSION_EXPIRED };

  const query = new URLSearchParams({
    page: String(params?.page ?? 1),
    pageSize: "50",
  });
  if (params?.search) query.set("search", params.search);

  const res = await tryFetch(`/api/admin/audit-log?${query.toString()}`, { headers });
  if (!res) return { data: null, error: NETWORK_ERROR };
  if (res.status === 401) return { data: null, error: SESSION_EXPIRED };
  if (!res.ok) return { data: null, error: await readProblem(res, "Failed to load audit log") };

  return { data: await safeJson<PagedResult<AuditLogEntryDto>>(res) };
}
