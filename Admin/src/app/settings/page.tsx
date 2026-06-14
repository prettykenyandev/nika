import { getCompanySettings } from "@/actions/settings";
import { SettingsForm } from "@/components/SettingsForm";

export const metadata = {
  title: "Settings — Nika Admin",
};

export default async function SettingsPage() {
  const settings = await getCompanySettings();

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold sm:text-3xl">Business settings</h1>
        <p className="mt-1 text-sm text-muted">
          Company profile, tax, currency and document numbering used across invoices,
          bills and purchase orders.
        </p>
      </div>

      {settings ? (
        <SettingsForm initial={settings} />
      ) : (
        <p className="border border-danger/40 bg-danger/10 p-4 text-sm text-danger">
          Could not load settings. Is the API running and are you signed in?
        </p>
      )}
    </div>
  );
}
