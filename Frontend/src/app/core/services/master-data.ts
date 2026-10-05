import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiResponse } from '../models/api-response.model';
import {
  DepartmentDto,
  DesignationDto,
  CreateUpdateDepartmentDto,
  CreateUpdateDesignationDto,
} from '../models/master-data.model';
import { API_ENDPOINTS } from '../constants/api-endpoints';

@Injectable({
  providedIn: 'root',
})
export class MasterDataService {
  constructor(private http: HttpClient) {}

  // ============================================================
  // DEPARTMENTS
  // ============================================================
  getDepartments(includeInactive: boolean = false): Observable<ApiResponse<DepartmentDto[]>> {
    const params = new HttpParams().set('includeInactive', includeInactive.toString());
    return this.http.get<ApiResponse<DepartmentDto[]>>(
      API_ENDPOINTS.ADMIN.DEPARTMENTS,
      { params }
    );
  }

  createDepartment(data: CreateUpdateDepartmentDto): Observable<ApiResponse<DepartmentDto>> {
    return this.http.post<ApiResponse<DepartmentDto>>(
      API_ENDPOINTS.ADMIN.DEPARTMENTS,
      data
    );
  }

  updateDepartment(id: number, data: CreateUpdateDepartmentDto): Observable<ApiResponse<DepartmentDto>> {
    return this.http.put<ApiResponse<DepartmentDto>>(
      `${API_ENDPOINTS.ADMIN.DEPARTMENTS}/${id}`,
      data
    );
  }

  toggleDepartment(id: number): Observable<ApiResponse<DepartmentDto>> {
    return this.http.patch<ApiResponse<DepartmentDto>>(
      `${API_ENDPOINTS.ADMIN.DEPARTMENTS}/${id}/toggle-active`,
      {}
    );
  }

  // ============================================================
  // DESIGNATIONS
  // ============================================================
  getDesignations(includeInactive: boolean = false): Observable<ApiResponse<DesignationDto[]>> {
    const params = new HttpParams().set('includeInactive', includeInactive.toString());
    return this.http.get<ApiResponse<DesignationDto[]>>(
      API_ENDPOINTS.ADMIN.DESIGNATIONS,
      { params }
    );
  }

  createDesignation(data: CreateUpdateDesignationDto): Observable<ApiResponse<DesignationDto>> {
    return this.http.post<ApiResponse<DesignationDto>>(
      API_ENDPOINTS.ADMIN.DESIGNATIONS,
      data
    );
  }

  updateDesignation(id: number, data: CreateUpdateDesignationDto): Observable<ApiResponse<DesignationDto>> {
    return this.http.put<ApiResponse<DesignationDto>>(
      `${API_ENDPOINTS.ADMIN.DESIGNATIONS}/${id}`,
      data
    );
  }

  toggleDesignation(id: number): Observable<ApiResponse<DesignationDto>> {
    return this.http.patch<ApiResponse<DesignationDto>>(
      `${API_ENDPOINTS.ADMIN.DESIGNATIONS}/${id}/toggle-active`,
      {}
    );
  }
}