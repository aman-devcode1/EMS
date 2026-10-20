import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiResponse } from '../models/api-response.model';
import {
  DepartmentDto,
  DesignationDto,
  CreateUpdateDepartmentDto,
  CreateUpdateDesignationDto,
  MasterDataResponse,
} from '../models/master-data.model';
import { API_ENDPOINTS } from '../constants/api-endpoints';

@Injectable({
  providedIn: 'root',
})
export class MasterDataService {
  constructor(private http: HttpClient) { }

  // ============================================================
  // Get ALL Master Data in one call
  // ============================================================
  getAllMasterData(includeInactive: boolean = false): Observable<ApiResponse<MasterDataResponse>> {
    const params = new HttpParams().set('includeInactive', includeInactive.toString());
    return this.http.get<ApiResponse<MasterDataResponse>>(
      `${API_ENDPOINTS.ADMIN.MASTER_DATA}/all`,
      { params }
    );
  }

  // ============================================================
  // DEPARTMENTS (Create, Update, Toggle, Delete)
  // ============================================================
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

  deleteDepartment(id: number): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(
      `${API_ENDPOINTS.ADMIN.DEPARTMENTS}/${id}`
    );
  }

  // ============================================================
  // DESIGNATIONS (Create, Update, Toggle, Delete)
  // ============================================================
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

  deleteDesignation(id: number): Observable<ApiResponse<object>> {
    return this.http.delete<ApiResponse<object>>(
      `${API_ENDPOINTS.ADMIN.DESIGNATIONS}/${id}`
    );
  }
}