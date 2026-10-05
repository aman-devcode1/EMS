// Backend ka standard response wrapper
export interface ApiResponse<T> {
    success: boolean;
    message: string;
    data: T;
}
// Pagination ke liye (Employee list mein use hoga)
export interface PagedResult<T> {
  pageNumber: number;
  pageSize: number;
  totalRecords: number;
  totalPages: number;
  items: T[];
}