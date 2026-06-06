"use server";

import { cookies } from "next/headers";
import { redirect } from "next/navigation";
import { API_URL } from "@/lib/api";
import { TOKEN_COOKIE, USER_COOKIE } from "@/lib/constants";
import type { AuthResponse } from "@/lib/types";

export interface LoginState {
  error?: string;
}

export async function loginAction(
  _prev: LoginState,
  formData: FormData,
): Promise<LoginState> {
  const email = String(formData.get("email") ?? "").trim();
  const password = String(formData.get("password") ?? "");

  if (!email || !password) {
    return { error: "Email and password are required." };
  }

  let res: Response;
  try {
    res = await fetch(`${API_URL}/api/auth/login`, {
      method: "POST",
      headers: { "Content-Type": "application/json", Accept: "application/json" },
      body: JSON.stringify({ email, password }),
      cache: "no-store",
    });
  } catch {
    return { error: "Could not reach the API. Make sure it is running." };
  }

  if (res.status === 401) return { error: "Invalid email or password." };
  if (!res.ok) return { error: `Login failed (${res.status}).` };

  const auth = (await res.json()) as AuthResponse;
  if (!auth.roles?.includes("Admin")) {
    return { error: "This account does not have administrator access." };
  }

  const store = await cookies();
  const expires = new Date(auth.expiresAtUtc);
  const secure = process.env.NODE_ENV === "production";
  store.set(TOKEN_COOKIE, auth.token, {
    httpOnly: true,
    sameSite: "lax",
    secure,
    path: "/",
    expires,
  });
  store.set(
    USER_COOKIE,
    JSON.stringify({ email: auth.email, fullName: auth.fullName }),
    { httpOnly: true, sameSite: "lax", secure, path: "/", expires },
  );

  // redirect throws, so it must stay outside the try/catch above.
  redirect("/");
}

export async function logoutAction(): Promise<void> {
  const store = await cookies();
  store.delete(TOKEN_COOKIE);
  store.delete(USER_COOKIE);
  redirect("/login");
}
