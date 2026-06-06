import { cookies } from "next/headers";
import { TOKEN_COOKIE, USER_COOKIE } from "@/lib/constants";

export async function getToken(): Promise<string | null> {
  const store = await cookies();
  return store.get(TOKEN_COOKIE)?.value ?? null;
}

export interface AdminUser {
  email: string;
  fullName: string;
}

export async function getAdminUser(): Promise<AdminUser | null> {
  const store = await cookies();
  const raw = store.get(USER_COOKIE)?.value;
  if (!raw) return null;
  try {
    return JSON.parse(raw) as AdminUser;
  } catch {
    return null;
  }
}
