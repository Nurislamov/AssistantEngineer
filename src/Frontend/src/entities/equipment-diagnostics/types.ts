export type EquipmentDiagnosticBotResponseStatus =
  | "Answer"
  | "ClarificationRequired"
  | "NotFound"
  | "ReferenceOnly"
  | "Unsupported"
  | "UnsafeOrOutOfScope";

export type EquipmentDiagnosticBotEquipmentSide =
  | "Indoor"
  | "Outdoor"
  | "Chiller"
  | "Controller"
  | "CommissioningTool"
  | "Unknown";

export type EquipmentDiagnosticBotDisplayContext =
  | "WiredController"
  | "OduMainBoardLed"
  | "IduDisplay"
  | "CentralizedController"
  | "PortableCommissioningTool"
  | "MobileAppOrGateway"
  | "Unknown";

export interface EquipmentDiagnosticBotRequest {
  manufacturer: string;
  code: string;
  freeText?: string;
  series?: string;
  modelCode?: string;
  equipmentSide?: number;
  displayContext?: number;
  preferredLanguage?: string;
  operatorProvidedMeasurements?: Record<string, string>;
}

export interface EquipmentDiagnosticBotClarificationOption {
  label: string;
  manufacturer: string;
  series?: string | null;
  category: number;
  equipmentSide: number;
  displayContext: number;
  code: string;
  explanation: string;
  followUpPrompt: string;
}

export interface EquipmentDiagnosticBotResponse {
  status: number | EquipmentDiagnosticBotResponseStatus;
  title: string;
  message: string;
  normalizedManufacturer: string;
  normalizedCode: string;
  answerCard?: {
    title: string;
    summary: string;
    verificationBanner: string;
    likelyCauses: string[];
    recommendedChecks: string[];
    operatorNotes: string[];
  } | null;
  clarificationQuestion?: {
    prompt: string;
    options: EquipmentDiagnosticBotClarificationOption[];
  } | null;
  sourceCard?: {
    sourceType: string;
    evidenceLevel: string;
    summary: string;
    limitations: string[];
  } | null;
  safetyCard: {
    boundary: string;
    notes: string[];
  };
  verificationRequired: boolean;
  confidence: number | string;
  isManualVerified: boolean;
  isSeedKnowledge: boolean;
  operatorNextSteps: string[];
  warnings: string[];
}

export interface EquipmentDiagnosticBotValidationProblem {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}
