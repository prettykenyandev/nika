"use client";

import { useActionState } from "react";
import { loginAction, type LoginState } from "@/actions/auth";

const initialState: LoginState = {};

export function LoginForm() {
  const [state, formAction, pending] = useActionState(loginAction, initialState);

  return (
    <form action={formAction} className="flex flex-col gap-4">
      <label className="flex flex-col gap-1 text-sm">
        <span className="text-muted">Email</span>
        <input
          type="email"
          name="email"
          autoComplete="username"
          defaultValue="admin@nikafitness.local"
          required
          className="border border-border-soft bg-surface-2 px-3 py-2 outline-none focus:border-accent"
        />
      </label>

      <label className="flex flex-col gap-1 text-sm">
        <span className="text-muted">Password</span>
        <input
          type="password"
          name="password"
          autoComplete="current-password"
          required
          className="border border-border-soft bg-surface-2 px-3 py-2 outline-none focus:border-accent"
        />
      </label>

      {state.error ? (
        <p className="text-sm text-danger">{state.error}</p>
      ) : null}

      <button
        type="submit"
        disabled={pending}
        className="bg-accent-strong px-4 py-2.5 font-semibold text-white transition hover:bg-accent disabled:cursor-not-allowed disabled:opacity-50"
      >
        {pending ? "Signing in…" : "Sign in"}
      </button>

      <p className="text-xs text-muted">
        Default dev admin: admin@nikafitness.local / Admin123!
      </p>
    </form>
  );
}
