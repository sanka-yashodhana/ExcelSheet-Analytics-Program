import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AnalyticsApiService, Employee, ExcelHeaderMapping, ImportResult } from '../services/analytics-api.service';

import { ExcelColumnMappingComponent } from '../components/excel-column-mapping/excel-column-mapping.component';

@Component({
  standalone: true,
  imports: [CommonModule, RouterLink, ExcelColumnMappingComponent],
  template: `
    <!-- Home Page Hero Banner Under Navbar with 3D Excel Asset -->
    <section class="home-hero-banner">
      <div class="hero-banner-inner">
        <div class="hero-text-col">
          <div class="hero-eyebrow">
            <span class="hero-live-dot"></span> EXCEL WORKBOOK ENGINE
          </div>
          <h1>Bring your spreadsheets to life.</h1>
          <p>Scan headers automatically, map columns with precision, and transform raw employee rows into clean, actionable workforce insights.</p>
          <div class="hero-badge-row">
            <span class="hero-pill">✓ Automatic Header Detection</span>
            <span class="hero-pill">✓ Department Normalization</span>
            <span class="hero-pill">✓ Live Analytics &amp; Charts</span>
          </div>
        </div>
        <div class="hero-art-col">
          <div class="hero-art-container">
            <img src="/assets/excel-3d-logo.png" alt="Excel 3D Spreadsheet" class="hero-3d-asset" />
            <div class="hero-art-glow"></div>
          </div>
        </div>
      </div>
    </section>

    <div class="import-layout">
      <div>
        <section class="panel upload-card">
          <div class="card-topline">
            <div><h2 class="card-title">Upload employee data</h2><p class="card-caption">Add an Excel file to scan and import your records.</p></div>
            <span class="step-chip"><span>01</span> Import</span>
          </div>

          <app-excel-column-mapping
            [isImporting]="isLoading"
            [importCompleted]="!!result"
            (fileChanged)="onWorkbookSelected()"
            (importRequested)="upload($event)"
          />

          <div class="notice notice-error upload-alert" *ngIf="errors.length">
            <span class="notice-icon"><span>!</span></span><div><strong>We couldn’t import this workbook.</strong><ul><li *ngFor="let error of errors">{{ error }}</li></ul></div>
          </div>
          <div class="loading-panel" *ngIf="isLoading" role="status" aria-live="polite">
            <span class="spinner"></span><div>Importing your workbook…<small>Validating mapped columns and saving employee records.</small></div>
          </div>
        </section>

        <section class="panel result-card" *ngIf="result" aria-live="polite">
          <div class="result-header"><span class="result-check">✓</span><div><h3>Import complete</h3><p>{{ result.message }}</p></div></div>
          <div class="result-file"><span>▤</span><div><strong>{{ result.fileName || loadedFileName }}</strong><small>Successfully saved to your employee database</small></div></div>
          <div class="result-stats"><div class="result-stat"><strong>{{ result.totalEmployees }}</strong><span>Employee records imported</span></div><div class="result-stat"><strong>#{{ result.importHistoryId }}</strong><span>Import reference</span></div></div>
          <div class="preview-heading"><strong>Imported employee preview</strong><a [routerLink]="['/imports', result.importHistoryId]">View this spreadsheet →</a></div>
          <div class="preview-list" *ngIf="importedEmployees.length; else previewState">
            <div class="preview-person" *ngFor="let employee of importedEmployees | slice:0:5">
              <span class="avatar">{{ initials(employee) }}</span><div><strong>{{ employee.firstName }} {{ employee.lastName }}</strong><span>{{ employee.employeeCode }} · {{ employee.department }}</span></div>
            </div>
          </div>
          <ng-template #previewState><small class="card-caption">{{ isPreviewLoading ? 'Loading imported employee records…' : 'Your import succeeded. View the directory to find these records.' }}</small></ng-template>
        </section>
      </div>

      <aside class="side-stack">
        <!-- 3D Excel Feature Card -->
        <section class="panel excel-showcase-card">
          <div class="excel-card-header">
            <img src="/assets/excel-3d-logo.png" alt="Excel Logo" class="excel-mini-logo" />
            <div>
              <h3>Excel Ingestion Engine</h3>
              <span>XLSX &bull; Multi-sheet ready</span>
            </div>
          </div>
          <p class="excel-card-desc">Upload any employee spreadsheet to automatically map columns, detect hire dates, and standardize departments.</p>
        </section>

        <section class="panel guide-card">
          <div class="guide-title"><h2>Workbook checklist</h2><span class="guide-icon">✓</span></div>
          <p class="guide-intro">Map each spreadsheet heading to the employee field it represents before importing.</p>
          <div class="column-list"><span class="column-pill" *ngFor="let column of columns">{{ column }}</span></div>
          <p class="guide-foot">Required fields must be filled. Salary must be positive, HireDate must be a valid date, and Department is standardized into predefined departments (e.g. HR / Human Resources → Human Resources, IT → Information Technology).</p>
        </section>
        <section class="tip-card">
          <p class="eyebrow">Department standardization</p><h3>Clean charts &amp; database records</h3>
          <p>Repeating department variations (like "HR" and "HUman resources", or "IT" and "Information Technology") are automatically unified into standard predefined departments.</p>
          <a class="history-link" routerLink="/employees">Explore employee directory <span>→</span></a>
        </section>
      </aside>
    </div>
  `
})
export class UploadPageComponent {
  private readonly api = inject(AnalyticsApiService);

  readonly columns = ['EmployeeCode', 'FirstName', 'LastName', 'Department', 'JobTitle', 'Salary', 'HireDate'];
  isLoading = false;
  errors: string[] = [];
  result: ImportResult | null = null;
  importedEmployees: Employee[] = [];
  loadedFileName = '';
  isPreviewLoading = false;

  upload(request: { file: File; headers: ExcelHeaderMapping[] }): void {
    if (this.isLoading) return;
    const { file, headers } = request;
    this.errors = [];
    this.result = null;
    this.importedEmployees = [];
    this.isLoading = true;
    this.api.upload(file, headers).subscribe({
      next: (result) => {
        this.result = result;
        this.loadedFileName = result.fileName || file.name;
        this.isLoading = false;
        this.isPreviewLoading = true;
        this.api.getEmployees().subscribe({
          next: employees => {
            this.importedEmployees = employees.filter(employee => employee.importHistoryId === result.importHistoryId);
            this.isPreviewLoading = false;
          },
          error: () => {
            this.importedEmployees = [];
            this.isPreviewLoading = false;
          }
        });
      },
      error: error => {
        this.errors = this.api.getErrorMessages(error);
        this.isLoading = false;
      }
    });
  }

  onWorkbookSelected(): void {
    this.errors = [];
    this.result = null;
    this.importedEmployees = [];
    this.loadedFileName = '';
    this.isPreviewLoading = false;
  }

  initials(employee: Employee): string {
    return `${employee.firstName.charAt(0)}${employee.lastName.charAt(0)}`.toUpperCase();
  }
}

