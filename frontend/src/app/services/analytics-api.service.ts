import {
  HttpClient,
  HttpErrorResponse,
  HttpHeaders,
  HttpParams,
} from "@angular/common/http";
import { Injectable, inject } from "@angular/core";
import { Observable } from "rxjs";

export interface Employee {
  id: number;
  employeeCode: string;
  firstName: string;
  lastName: string;
  department: string;
  jobTitle: string;
  salary: number;
  hireDate: string;
  importHistoryId: number;
  importFileName: string;
}

export interface ImportHistory {
  id: number;
  fileName: string;
  uploadedDate: string;
  totalRows: number;
  processedRows?: number;
  importedRows?: number;
  rejectedRows?: number;
  employeeCount: number;
  status: string;
  hasErrorReport?: boolean;
  jobId?: string | null;
  errorMessage?: string | null;
}

export interface DeleteEmployeesResult {
  message: string;
  deletedCount: number;
}

export interface DeleteImportHistoryResult {
  message: string;
  importHistoryId: number;
  deletedEmployees: number;
}

export interface ImportResult {
  success: boolean;
  message: string;
  importHistoryId: number;
  totalEmployees: number;
  totalRecords?: number;
  processedRecords?: number;
  rejectedRecords?: number;
  jobId?: string | null;
  hasErrorReport?: boolean;
  fileName: string;
  errors: string[];
}

export interface ExcelImportConfig {
  maxFileSizeMB: number;
  maxFileSizeBytes: number;
  maxRecords: number;
  batchSize: number;
}

export interface StartImportResponse {
  jobId: string;
  importHistoryId: number;
  fileName: string;
  status: string;
  message: string;
}

export interface ImportJobProgress {
  jobId: string;
  importHistoryId: number;
  fileName: string;
  status: string;
  totalRecords: number;
  processedRecords: number;
  importedRecords: number;
  rejectedRecords: number;
  percentage: number;
  currentMessage: string;
  hasErrorReport: boolean;
  errorReportUrl: string | null;
  sampleErrors: string[];
  startedAt: string;
  completedAt: string | null;
}

export interface PagedEmployeesResponse {
  items: Employee[];
  totalCount: number;
  totalEmployeesInDb: number;
  page: number;
  pageSize: number;
  totalPages: number;
  distinctDepartmentCount: number;
}

export interface DepartmentStatisticDto {
  department: string;
  employeeCount: number;
}

export interface DepartmentSalaryStatisticDto {
  department: string;
  totalSalary: number;
}

export interface HiringTrendStatisticDto {
  year: number;
  employeeCount: number;
}

export interface EmployeeAnalyticsSummary {
  totalEmployees: number;
  totalDepartments: number;
  averageSalary: number;
  byDepartment: DepartmentStatisticDto[];
  salaryByDepartment: DepartmentSalaryStatisticDto[];
  hiringTrends: HiringTrendStatisticDto[];
}

export interface ExcelHeaderMapping {
  excelHeader: string;
  suggestedField: string | null;
  isRequired: boolean;
  isMatched: boolean;
}

export interface ExcelHeaderDetectionResponse {
  headers: ExcelHeaderMapping[];
  isValid: boolean;
  missingRequiredFields: string[];
}

export interface PagedEmployeesQuery {
  page?: number;
  pageSize?: number;
  search?: string;
  department?: string;
  importHistoryId?: number;
  sortBy?: string;
  sortDir?: string;
}

@Injectable({ providedIn: "root" })
export class AnalyticsApiService {
  private readonly http = inject(HttpClient);
  private readonly noCacheHeaders = new HttpHeaders({
    "Cache-Control": "no-cache",
    Pragma: "no-cache",
  });
  private readonly baseUrl =
    window.location.port === "4200"
      ? "http://localhost:5239/api"
      : "https://employee-excel-analytics-api-fbbdcteuegdne3f7.eastasia-01.azurewebsites.net/api";

  getImportConfig(): Observable<ExcelImportConfig> {
    return this.http.get<ExcelImportConfig>(
      `${this.baseUrl}/ExcelImport/config`,
      {
        headers: this.noCacheHeaders,
      },
    );
  }

  upload(
    file: File,
    headerMappings: ExcelHeaderMapping[],
  ): Observable<ImportResult> {
    const formData = new FormData();
    formData.append("file", file);
    formData.append("headerMappings", JSON.stringify(headerMappings));
    return this.http.post<ImportResult>(
      `${this.baseUrl}/ExcelImport/upload`,
      formData,
    );
  }

  startLargeImport(
    file: File,
    headerMappings: ExcelHeaderMapping[],
  ): Observable<StartImportResponse> {
    const formData = new FormData();
    formData.append("file", file);
    formData.append("headerMappings", JSON.stringify(headerMappings));
    return this.http.post<StartImportResponse>(
      `${this.baseUrl}/ExcelImport/start`,
      formData,
    );
  }

  getImportJobProgress(jobId: string): Observable<ImportJobProgress> {
    return this.http.get<ImportJobProgress>(
      `${this.baseUrl}/ExcelImport/jobs/${encodeURIComponent(jobId)}`,
      { headers: this.noCacheHeaders },
    );
  }

  downloadJobErrorReport(jobId: string): Observable<Blob> {
    return this.http.get(
      `${this.baseUrl}/ExcelImport/jobs/${encodeURIComponent(jobId)}/error-report`,
      { responseType: "blob" },
    );
  }

  downloadHistoryErrorReport(id: number): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/ImportHistories/${id}/error-report`, {
      responseType: "blob",
    });
  }

  getEmployees(importHistoryId?: number): Observable<Employee[]> {
    const url = `${this.baseUrl}/Employees`;
    return this.http.get<Employee[]>(
      url,
      importHistoryId === undefined ? {} : { params: { importHistoryId } },
    );
  }

  getEmployeesPaged(
    query: PagedEmployeesQuery,
  ): Observable<PagedEmployeesResponse> {
    let params = new HttpParams();
    if (query.page !== undefined) params = params.set("page", query.page);
    if (query.pageSize !== undefined)
      params = params.set("pageSize", query.pageSize);
    if (query.search) params = params.set("search", query.search);
    if (query.department) params = params.set("department", query.department);
    if (query.importHistoryId !== undefined) {
      params = params.set("importHistoryId", query.importHistoryId);
    }
    if (query.sortBy) params = params.set("sortBy", query.sortBy);
    if (query.sortDir) params = params.set("sortDir", query.sortDir);

    return this.http.get<PagedEmployeesResponse>(
      `${this.baseUrl}/Employees/paged`,
      {
        params,
        headers: this.noCacheHeaders,
      },
    );
  }

  getAnalyticsSummary(): Observable<EmployeeAnalyticsSummary> {
    return this.http.get<EmployeeAnalyticsSummary>(
      `${this.baseUrl}/Employees/analytics`,
      {
        headers: this.noCacheHeaders,
      },
    );
  }

  getImportHistories(): Observable<ImportHistory[]> {
    return this.http.get<ImportHistory[]>(`${this.baseUrl}/ImportHistories`, {
      headers: this.noCacheHeaders,
    });
  }

  getImportHistoryById(id: number): Observable<ImportHistory> {
    return this.http.get<ImportHistory>(
      `${this.baseUrl}/ImportHistories/${id}`,
    );
  }

  deleteAllEmployees(): Observable<DeleteEmployeesResult> {
    return this.http.delete<DeleteEmployeesResult>(`${this.baseUrl}/Employees`);
  }

  deleteImportHistory(id: number): Observable<DeleteImportHistoryResult> {
    return this.http.delete<DeleteImportHistoryResult>(
      `${this.baseUrl}/ImportHistories/${id}`,
    );
  }

  getErrorMessages(error: unknown): string[] {
    if (!(error instanceof HttpErrorResponse)) {
      return ["Something went wrong. Please try again."];
    }

    const body: unknown = error.error;
    if (typeof body === "string" && body.trim().length > 0) {
      return [body];
    }
    if (typeof body === "object" && body !== null) {
      const details = body as { message?: unknown; errors?: unknown };
      if (Array.isArray(details.errors)) {
        const messages = details.errors.filter(
          (item): item is string => typeof item === "string",
        );
        if (messages.length) return messages;
      }
      if (typeof details.message === "string") return [details.message];
    }

    if (error.status === 0) {
      return [
        "Could not reach the API. Make sure the .NET backend is running at http://localhost:5239.",
      ];
    }
    return [error.message || "The request could not be completed."];
  }

  // Detect Excel headers
  detectHeaders(file: File): Observable<ExcelHeaderDetectionResponse> {
    const formData = new FormData();
    formData.append("file", file);
    return this.http.post<ExcelHeaderDetectionResponse>(
      `${this.baseUrl}/ExcelImport/detect-headers`,
      formData,
    );
  }

  downloadImportHistoryWorkbook(id: number): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/ImportHistories/${id}/download`, {
      responseType: "blob",
    });
  }

  downloadEmployeesWorkbook(importHistoryId?: number): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/Employees/download`, {
      params: importHistoryId !== undefined ? { importHistoryId } : {},
      responseType: "blob",
    });
  }
}
