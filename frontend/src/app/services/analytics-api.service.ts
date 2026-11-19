import { HttpClient, HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

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
  employeeCount: number;
  status: string;
}

export interface DeleteEmployeesResult {
  message: string;
  deletedCount: number;
}

export interface ImportResult {
  success: boolean;
  message: string;
  importHistoryId: number;
  totalEmployees: number;
  fileName: string;
  errors: string[];
}

@Injectable({ providedIn: 'root' })
export class AnalyticsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = 'http://localhost:5239/api';

  upload(file: File): Observable<ImportResult> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<ImportResult>(`${this.baseUrl}/ExcelImport/upload`, formData);
  }

  getEmployees(importHistoryId?: number): Observable<Employee[]> {
    const url = `${this.baseUrl}/Employees`;
    return this.http.get<Employee[]>(url, importHistoryId === undefined ? {} : { params: { importHistoryId } });
  }

  getImportHistories(): Observable<ImportHistory[]> {
    return this.http.get<ImportHistory[]>(`${this.baseUrl}/ImportHistories`);
  }

  deleteAllEmployees(): Observable<DeleteEmployeesResult> {
    return this.http.delete<DeleteEmployeesResult>(`${this.baseUrl}/Employees`);
  }

  getErrorMessages(error: unknown): string[] {
    if (!(error instanceof HttpErrorResponse)) {
      return ['Something went wrong. Please try again.'];
    }

    const body: unknown = error.error;
    if (typeof body === 'object' && body !== null) {
      const details = body as { message?: unknown; errors?: unknown };
      if (Array.isArray(details.errors)) {
        const messages = details.errors.filter((item): item is string => typeof item === 'string');
        if (messages.length) return messages;
      }
      if (typeof details.message === 'string') return [details.message];
    }

    if (error.status === 0) {
      return ['Could not reach the API. Make sure the .NET backend is running at http://localhost:5239.'];
    }
    return [error.message || 'The request could not be completed.'];
  }
}
