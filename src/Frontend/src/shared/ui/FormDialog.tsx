import { Dialog, DialogContent, DialogTitle } from "@mui/material";
import type { ReactNode } from "react";

interface FormDialogProps {
  open: boolean;
  title: string;
  maxWidth?: "xs" | "sm" | "md" | "lg" | "xl";
  onClose: () => void;
  children: ReactNode;
}

export function FormDialog({
  open,
  title,
  maxWidth = "sm",
  onClose,
  children,
}: FormDialogProps): JSX.Element {
  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth={maxWidth}>
      <DialogTitle>{title}</DialogTitle>
      <DialogContent>{children}</DialogContent>
    </Dialog>
  );
}
