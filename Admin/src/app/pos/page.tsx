import PosTerminal from "@/components/PosTerminal";

export const metadata = {
  title: "Point of sale — Nika Admin",
};

export default function PosPage() {
  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold sm:text-3xl">Point of sale</h1>
        <p className="mt-1 text-sm text-muted">
          Scan or type a SKU to add items, take cash, and the stock updates
          automatically.
        </p>
      </div>
      <PosTerminal />
    </div>
  );
}
