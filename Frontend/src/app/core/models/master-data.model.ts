// Response DTOs
export interface DepartmentDto {
  id: number;
  name: string;
  description?: string | null;
  isActive: boolean;
  isAssigned: boolean;
  createdAt: string;
}

export interface DesignationDto {
  id: number;
  name: string;
  description?: string | null;
  isActive: boolean;
  isAssigned: boolean;
  createdAt: string;
}

// Create / Update DTOs
export interface CreateUpdateDepartmentDto {
  name: string;
  description?: string | null;
}

export interface CreateUpdateDesignationDto {
  name: string;
  description?: string | null;
}

export interface MasterDataResponse {
  departments: DepartmentDto[];
  designations: DesignationDto[];
}