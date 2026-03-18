import * as React from "react";
import * as DialogPrimitive from "@radix-ui/react-dialog";

export type DialogProps = {
  open?: boolean;
  onOpenChange?: (open: boolean) => void;

  title?: React.ReactNode;
  children: React.ReactNode;

  className?: string;
};

export const Dialog: React.FC<DialogProps> = ({
  open,
  onOpenChange,
  title,
  children,
  className,
}) => {
  return (
    <DialogPrimitive.Root open={open} onOpenChange={onOpenChange}>
      <DialogPrimitive.Portal>
        {/* Overlay */}
        <DialogPrimitive.Overlay
          className="
            fixed inset-0 bg-black/50 backdrop-blur-sm
            data-[state=open]:animate-in data-[state=closed]:animate-out
          "
        />

        {/* Content */}
        <DialogPrimitive.Content
          className={`
            fixed top-1/2 left-1/2
            w-[90vw] max-w-md
            -translate-x-1/2 -translate-y-1/2
            rounded-2xl bg-white p-6 shadow-xl
            focus:outline-none
            ${className ?? ""}
          `}
        >
          {title && (
            <DialogPrimitive.Title className="text-lg font-semibold mb-3">
              {title}
            </DialogPrimitive.Title>
          )}

          {children}
        </DialogPrimitive.Content>
      </DialogPrimitive.Portal>
    </DialogPrimitive.Root>
  );
};