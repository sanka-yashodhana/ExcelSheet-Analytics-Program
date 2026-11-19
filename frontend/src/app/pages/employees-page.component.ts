import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AnalyticsApiService, Employee } from '../services/analytics-api.service';

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule, CurrencyPipe, DatePipe, RouterLink],
  template: `
    <section class="page-heading">
      <div><p class="eyebrow">Your people at a glance</p><h1>Employee directory</h1><p>Every imported employee, together in one place.</p></div>
      <div class="heading-actions">
        <button class="danger-button" type="button" *ngIf="employees.length" [disabled]="isDeleting" (click)="deleteAllEmployees()">{{ isDeleting ? 'Deleting…' : 'Delete all employee data' }}</button>
        <a class="primary-button" routerLink="/"> Import a workbook</a>
      </div>
    </section>

    <div class="directory-stats">
      <section class="panel metric-card"><span class="metric-icon">♙</span><div><span>Total employees</span><strong>{{ employees.length }}</strong></div></section>
      <section class="panel metric-card"><span class="metric-icon green">▦</span><div><span>Departments</span><strong>{{ departmentCount }}</strong></div></section>
      <section class="panel metric-card"><span class="metric-icon blue">↗</span><div><span>Showing records</span><strong>{{ filteredEmployees.length }}</strong></div></section>
    </div>

    <div class="notice notice-success delete-notice" *ngIf="deleteMessage"><span class="notice-icon"><span>✓</span></span>{{ deleteMessage }}</div>

    <section class="panel directory-card">
      <div class="table-toolbar">
        <div><h2>All employee records</h2><p>Records saved from your Excel imports</p></div>
        <label class="search-wrap"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><circle cx="11" cy="11" r="7"/><path d="m16 16 4 4" stroke-linecap="round"/></svg><input class="search-input" type="search" [(ngModel)]="searchTerm" placeholder="Search people, roles, codes…" aria-label="Search employee records"></label>
      </div>

      <div class="loading-state" *ngIf="isLoading"><span class="spinner"></span>Loading employee records…</div>
      <div class="notice notice-error directory-error" *ngIf="errors.length"><span class="notice-icon"><span>!</span></span><div><strong>Could not load employee records.</strong><ul><li *ngFor="let error of errors">{{ error }}</li></ul></div></div>
      <div class="empty-state" *ngIf="!isLoading && !errors.length && employees.length === 0"><span class="empty-symbol">♙</span><h3>No employee records currently stored</h3><p>Import a workbook to add employees, or review previous uploads in your history.</p><div class="empty-actions"><button class="primary-button" type="button" (click)="navigateToImport()">Import workbook ↗</button><a class="secondary-button" routerLink="/imports">View upload history</a></div></div>
      <div class="empty-state" *ngIf="!isLoading && !errors.length && employees.length > 0 && filteredEmployees.length === 0"><span class="empty-symbol">⌕</span><h3>No matching employees</h3><p>Try a different name, employee code, department, or job title.</p></div>

      <div class="table-scroll" *ngIf="!isLoading && !errors.length && filteredEmployees.length">
        <table class="employee-table">
          <thead><tr><th>Employee</th><th>Employee code</th><th>Department</th><th>Job title</th><th>Salary</th><th>Hire date</th><th>Source workbook</th></tr></thead>
          <tbody><tr *ngFor="let employee of filteredEmployees">
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
      <div class="table-footer" *ngIf="!isLoading && !errors.length && employees.length > 0"><span>Showing {{ filteredEmployees.length }} of {{ employees.length }} records</span><span class="table-status">Synced with database</span></div>
    </section>
  `
})
export class EmployeesPageComponent implements OnInit {
  private readonly api = inject(AnalyticsApiService);
  private readonly router = inject(Router);
  employees: Employee[] = [];
  isLoading = true;
  isDeleting = false;
  searchTerm = '';
  errors: string[] = [];
  deleteMessage = '';

  ngOnInit(): void {
    this.api.getEmployees().subscribe({
      next: employees => { this.employees = employees; this.isLoading = false; },
      error: error => { this.errors = this.api.getErrorMessages(error); this.isLoading = false; }
    });
  }

  get filteredEmployees(): Employee[] {
    const term = this.searchTerm.trim().toLowerCase();
    if (!term) return this.employees;
    return this.employees.filter(employee => [employee.firstName, employee.lastName, employee.employeeCode, employee.department, employee.jobTitle].some(value => value.toLowerCase().includes(term)));
  }

  get departmentCount(): number {
    return new Set(this.employees.map(employee => employee.department)).size;
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
        this.deleteMessage = result.message;
        this.isDeleting = false;
      },
      error: error => {
        this.errors = this.api.getErrorMessages(error);
        this.isDeleting = false;
      }
    });
  }
}
