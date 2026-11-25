import { CommonModule, CurrencyPipe, DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import { AnalyticsApiService, Employee } from '../services/analytics-api.service';
import { PREDEFINED_DEPARTMENTS } from '../services/employee-analytics.service';

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule, CurrencyPipe, DatePipe, DecimalPipe, RouterLink],
  template: `
    <section class="page-heading">
      <div><p class="eyebrow">Your people at a glance</p><h1>Employee directory</h1><p>Every imported employee, together in one place.</p></div>
      <div class="heading-actions">
        <button class="secondary-button" type="button" *ngIf="totalEmployeesInDb > 0" [disabled]="isExporting" (click)="exportToExcel()">
          <svg class="btn-svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/></svg>
          {{ isExporting ? 'Exporting…' : 'Export .xlsx' }}
        </button>
        <button class="danger-button" type="button" *ngIf="totalEmployeesInDb > 0" [disabled]="isDeleting" (click)="deleteAllEmployees()">{{ isDeleting ? 'Deleting…' : 'Delete all employee data' }}</button>
        <a class="primary-button" routerLink="/"> Import a workbook</a>
      </div>
    </section>

    <div class="directory-stats">
      <section class="panel metric-card">
        <span class="metric-icon">
          <svg class="metric-svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/>
            <circle cx="9" cy="7" r="4"/>
            <path d="M22 21v-2a4 4 0 0 0-3-3.87"/>
            <path d="M16 3.13a4 4 0 0 1 0 7.75"/>
          </svg>
        </span>
        <div><span>Total employees</span><strong>{{ totalEmployeesInDb | number }}</strong></div>
      </section>

      <section class="panel metric-card">
        <span class="metric-icon">
          <svg class="metric-svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <rect x="4" y="2" width="16" height="20" rx="2" ry="2"/>
            <path d="M9 22v-4h6v4"/>
            <path d="M8 6h.01"/>
            <path d="M16 6h.01"/>
            <path d="M8 10h.01"/>
            <path d="M16 10h.01"/>
            <path d="M8 14h.01"/>
            <path d="M16 14h.01"/>
          </svg>
        </span>
        <div><span>Departments</span><strong>{{ distinctDepartmentCount | number }}</strong></div>
      </section>

      <section class="panel metric-card">
        <span class="metric-icon">
          <svg class="metric-svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
            <polyline points="14 2 14 8 20 8"/>
            <line x1="16" y1="13" x2="8" y2="13"/>
            <line x1="16" y1="17" x2="8" y2="17"/>
            <line x1="10" y1="9" x2="8" y2="9"/>
          </svg>
        </span>
        <div><span>Matching records</span><strong>{{ totalCount | number }}</strong></div>
      </section>
    </div>

    <div class="notice notice-success delete-notice" *ngIf="deleteMessage"><span class="notice-icon"><span>✓</span></span>{{ deleteMessage }}</div>

    <section class="panel directory-card">
      <div class="table-toolbar">
        <div><h2>All employee records</h2><p>Server-side paginated view of your Excel imports</p></div>
        <div class="toolbar-controls">
          <select class="dept-filter-select" [(ngModel)]="selectedDepartment" (ngModelChange)="onDepartmentChange()" aria-label="Filter by department">
            <option value="">All departments</option>
            <option *ngFor="let dept of departments" [value]="dept">{{ dept }}</option>
          </select>
          <label class="search-wrap">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="m16 16 4 4" stroke-linecap="round"/></svg>
            <input
              class="search-input"
              type="search"
              [ngModel]="searchTerm"
              (ngModelChange)="onSearchInput($event)"
              placeholder="Search people, roles, codes…"
              aria-label="Search employee records"
            >
          </label>
        </div>
      </div>

      <div class="loading-state" *ngIf="isLoading"><span class="spinner"></span>Loading employee records…</div>
      <div class="notice notice-error directory-error" *ngIf="errors.length"><span class="notice-icon"><span>!</span></span><div><strong>Could not load employee records.</strong><ul><li *ngFor="let error of errors">{{ error }}</li></ul></div></div>
      <div class="empty-state" *ngIf="!isLoading && !errors.length && totalEmployeesInDb === 0"><span class="empty-symbol">♙</span><h3>No employee records currently stored</h3><p>Import a workbook to add employees, or review previous uploads in your history.</p><div class="empty-actions"><button class="primary-button" type="button" (click)="navigateToImport()">Import workbook ↗</button><a class="secondary-button" routerLink="/imports">View upload history</a></div></div>
      <div class="empty-state" *ngIf="!isLoading && !errors.length && totalEmployeesInDb > 0 && employees.length === 0"><span class="empty-symbol">⌕</span><h3>No matching employees</h3><p>Try a different name, employee code, department, or job title.</p></div>

      <div class="table-scroll" *ngIf="!isLoading && !errors.length && employees.length">
        <table class="employee-table">
          <thead><tr><th>Employee</th><th>Employee code</th><th>Department</th><th>Job title</th><th>Salary</th><th>Hire date</th><th>Source workbook</th></tr></thead>
          <tbody><tr *ngFor="let employee of employees">
            <td><div class="employee-cell"><span class="avatar">{{ initials(employee) }}</span><div><strong>{{ employee.firstName }} {{ employee.lastName }}</strong><span>Employee #{{ employee.id }}</span></div></div></td>
            <td><span class="code-tag">{{ employee.employeeCode }}</span></td>
            <td><span class="department-tag">{{ employee.department }}</span></td>
            <td>{{ employee.jobTitle }}</td>
            <td class="salary-cell">{{ employee.salary | currency:'USD':'symbol':'1.0-2' }}</td>
            <td>{{ employee.hireDate | date:'MMM d, y' }}</td>
            <td><a class="source-workbook-link" [routerLink]="['/imports', employee.importHistoryId]"><strong>{{ employee.importFileName || 'Workbook #' + employee.importHistoryId }}</strong><small>History ID #{{ employee.importHistoryId }}</small></a></td>
          </tr></tbody>
        </table>
      </div>

      <div class="table-footer pagination-bar" *ngIf="!isLoading && !errors.length && totalEmployeesInDb > 0">
        <span>Showing {{ rangeStart | number }}–{{ rangeEnd | number }} of {{ totalCount | number }} records</span>
        <div class="page-controls" *ngIf="totalCount > 0">
          <label class="page-size-label">
            Rows:
            <select class="page-select" [(ngModel)]="pageSize" (ngModelChange)="onPageSizeChange()" aria-label="Rows per page">
              <option [ngValue]="10">10</option>
              <option [ngValue]="25">25</option>
              <option [ngValue]="50">50</option>
              <option [ngValue]="100">100</option>
            </select>
          </label>
          <button class="page-btn" type="button" [disabled]="page <= 1" (click)="goToPage(1)" title="First page">«</button>
          <button class="page-btn" type="button" [disabled]="page <= 1" (click)="goToPage(page - 1)">Prev</button>
          <span class="page-indicator">Page {{ page | number }} of {{ totalPages | number }}</span>
          <button class="page-btn" type="button" [disabled]="page >= totalPages" (click)="goToPage(page + 1)">Next</button>
          <button class="page-btn" type="button" [disabled]="page >= totalPages" (click)="goToPage(totalPages)" title="Last page">»</button>
        </div>
      </div>
    </section>
  `
})
export class EmployeesPageComponent implements OnInit, OnDestroy {
  private readonly api = inject(AnalyticsApiService);
  private readonly router = inject(Router);
  private readonly search$ = new Subject<string>();
  private readonly destroy$ = new Subject<void>();

  readonly departments = PREDEFINED_DEPARTMENTS;

  employees: Employee[] = [];
  isLoading = true;
  isDeleting = false;
  searchTerm = '';
  selectedDepartment = '';
  errors: string[] = [];
  deleteMessage = '';
  isExporting = false;

  page = 1;
  pageSize = 25;
  totalCount = 0;
  totalEmployeesInDb = 0;
  totalPages = 1;
  distinctDepartmentCount = 0;

  get rangeStart(): number {
    if (this.totalCount === 0) return 0;
    return (this.page - 1) * this.pageSize + 1;
  }

  get rangeEnd(): number {
    return Math.min(this.page * this.pageSize, this.totalCount);
  }

  ngOnInit(): void {
    this.search$.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe((term) => {
      this.searchTerm = term;
      this.page = 1;
      this.loadEmployees();
    });

    this.loadEmployees();
  }

  loadEmployees(): void {
    this.isLoading = true;
    this.errors = [];

    this.api.getEmployeesPaged({
      page: this.page,
      pageSize: this.pageSize,
      search: this.searchTerm.trim() || undefined,
      department: this.selectedDepartment || undefined
    }).pipe(takeUntil(this.destroy$)).subscribe({
      next: (res) => {
        this.employees = res.items;
        this.totalCount = res.totalCount;
        this.totalEmployeesInDb = res.totalEmployeesInDb;
        this.page = res.page;
        this.pageSize = res.pageSize;
        this.totalPages = res.totalPages;
        this.distinctDepartmentCount = res.distinctDepartmentCount;
        this.isLoading = false;
      },
      error: (error) => {
        this.errors = this.api.getErrorMessages(error);
        this.isLoading = false;
      }
    });
  }

  onSearchInput(term: string): void {
    this.searchTerm = term;
    this.search$.next(term);
  }

  onDepartmentChange(): void {
    this.page = 1;
    this.loadEmployees();
  }

  onPageSizeChange(): void {
    this.page = 1;
    this.loadEmployees();
  }

  goToPage(targetPage: number): void {
    if (targetPage < 1 || targetPage > this.totalPages || targetPage === this.page) return;
    this.page = targetPage;
    this.loadEmployees();
  }

  exportToExcel(): void {
    if (this.isExporting) return;
    this.isExporting = true;
    this.api.downloadEmployeesWorkbook().subscribe({
      next: (blob: Blob) => {
        this.isExporting = false;
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.download = 'employees_export.xlsx';
        a.href = url;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
      },
      error: (err: unknown) => {
        this.isExporting = false;
        alert('Failed to export employee records: ' + this.api.getErrorMessages(err).join(' '));
      }
    });
  }

  initials(employee: Employee): string {
    return `${employee.firstName.charAt(0)}${employee.lastName.charAt(0)}`.toUpperCase();
  }

  navigateToImport(): void {
    void this.router.navigateByUrl('/');
  }

  deleteAllEmployees(): void {
    if (this.isDeleting || !window.confirm('Delete all employee records? Import history will be kept, but employee data cannot be recovered.')) return;
    this.isDeleting = true;
    this.deleteMessage = '';
    this.api.deleteAllEmployees().subscribe({
      next: result => {
        this.employees = [];
        this.totalCount = 0;
        this.totalEmployeesInDb = 0;
        this.distinctDepartmentCount = 0;
        this.totalPages = 1;
        this.page = 1;
        this.deleteMessage = result.message;
        this.isDeleting = false;
      },
      error: error => {
        this.errors = this.api.getErrorMessages(error);
        this.isDeleting = false;
      }
    });
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
