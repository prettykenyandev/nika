import { LoginForm } from "@/components/LoginForm";

export const metadata = {
  title: "Sign in — Nika Admin",
};

export default function LoginPage() {
  return (
    <div className="mx-auto w-full max-w-sm py-6 sm:py-12">
      <h1 className="text-2xl font-bold">Admin sign in</h1>
      <p className="mt-2 text-sm text-muted">
        Manage the Nika Fitness catalogue and inventory.
      </p>
      <div className="mt-6 border border-border-soft bg-surface p-5 sm:p-6">
        <LoginForm />
      </div>
    </div>
  );
}
