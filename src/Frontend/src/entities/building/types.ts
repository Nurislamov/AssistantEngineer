export interface ProjectDto {
  id: number;
  name: string;
}

export interface CreateProjectRequest {
  name: string;
}

export interface UpdateProjectRequest {
  name: string;
}

export interface BuildingDto {
  id: number;
  projectId: number;
  name: string;
  address?: string;
  description?: string;
  climateZoneId?: number | null;
  climateZoneName?: string | null;
  createdAt?: string;
}

export interface CreateBuildingRequest {
  name: string;
  address?: string;
  description?: string;
  climateZoneId?: number | null;
}

export interface UpdateBuildingRequest {
  name: string;
  climateZoneId?: number | null;
}

export interface BuildingApiResponse {
  id: number;
  projectId: number;
  name: string;
  climateZoneId?: number | null;
  climateZoneName?: string | null;
}

export interface ProjectApiResponse {
  id: number;
  name: string;
}

export interface BuildingReadinessIssueApiResponse {
  code: string;
  severity: string;
  message: string;
  entityType?: string | null;
  entityId?: number | null;
}

export interface BuildingReadinessApiResponse {
  buildingId: number;
  buildingName: string;
  weatherYear: number;
  isReady: boolean;
  issues: BuildingReadinessIssueApiResponse[];
}

export interface BuildingValidationIssueApiResponse {
  code: string;
  severity: string;
  message: string;
  entityType?: string | null;
  entityId?: number | null;
}

export interface BuildingValidationApiResponse {
  buildingId: number;
  buildingName: string;
  weatherYear: number;
  isValid: boolean;
  issues: BuildingValidationIssueApiResponse[];
}
