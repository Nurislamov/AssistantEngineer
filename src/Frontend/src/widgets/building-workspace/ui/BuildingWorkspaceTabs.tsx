import { Tab, Tabs } from "@mui/material";
import { DataCard } from "@/shared/ui/DataCard";

export type WorkspaceTab =
  | "summary"
  | "floors"
  | "envelope"
  | "zones"
  | "ventilation"
  | "ground"
  | "calculations"
  | "reports"
  | "equipment";

const tabs: Array<{ value: WorkspaceTab; label: string }> = [
  { value: "summary", label: "Summary" },
  { value: "floors", label: "Floors & Rooms" },
  { value: "envelope", label: "Envelope" },
  { value: "zones", label: "Thermal zones" },
  { value: "ventilation", label: "Ventilation" },
  { value: "ground", label: "Ground contact" },
  { value: "calculations", label: "Calculations" },
  { value: "reports", label: "Reports" },
  { value: "equipment", label: "Equipment" },
];

export function BuildingWorkspaceTabs({
  tab,
  onChange,
}: {
  tab: WorkspaceTab;
  onChange: (tab: WorkspaceTab) => void;
}): JSX.Element {
  return (
    <DataCard compact>
      <Tabs
        value={tab}
        onChange={(_, next: WorkspaceTab) => onChange(next)}
        variant="scrollable"
        scrollButtons="auto"
      >
        {tabs.map((item) => (
          <Tab key={item.value} value={item.value} label={item.label} />
        ))}
      </Tabs>
    </DataCard>
  );
}
