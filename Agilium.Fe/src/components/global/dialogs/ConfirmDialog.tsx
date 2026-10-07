import { Dialog } from "./Dialog";

export function ConfirmDialog({
  open,
  title,
  message,
  confirmLabel = "Potvrdit",
  cancelLabel = "Zrušit",
  onConfirm,
  onCancel,
}: {
  open: boolean;
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  onConfirm: () => void;
  onCancel: () => void;
}) {
  return (
    <Dialog open={open} onOpenChange={(isOpen) => !isOpen && onCancel()} title={title}>
      <p className="text-sm text-gray-700">{message}</p>
      <div className="mt-4 flex justify-end gap-2">
        <button onClick={onCancel} className="rounded border border-gray-300 px-3 py-2 text-sm hover:bg-gray-50">
          {cancelLabel}
        </button>
        <button onClick={onConfirm} className="rounded bg-red-600 px-3 py-2 text-sm font-medium text-white hover:bg-red-700">
          {confirmLabel}
        </button>
      </div>
    </Dialog>
  );
}
