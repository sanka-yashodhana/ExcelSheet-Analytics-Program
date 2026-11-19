import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AnalyticsApiService, Employee, ImportResult } from '../services/analytics-api.service';

@Component({
  standalone: true,
  imports: [CommonModule, RouterLink],
  template: `
    <section class="page-heading">
      <div>
        <p class="eyebrow">Employee data, made simple</p>
        <h1>Bring your spreadsheet<br class="mobile-break"> to life.</h1>
        <p>Upload an employee workbook and turn rows into insight.</p>
      </div>
      <span class="soft-label"><i></i> Secure workspace</span>
    </section>

    <div class="import-layout">
      <div>
        <section class="panel upload-card">
          <div class="card-topline">
            <div><h2 class="card-title">Upload employee data</h2><p class="card-caption">Add an Excel file to scan and import your records.</p></div>
            <span class="step-chip"><span>01</span> Import</span>
          </div>

          <div class="drop-zone" [class.dragging]="isDragging" (dragover)="onDragOver($event)" (dragleave)="isDragging = false" (drop)="onDrop($event)">
            <div class="drop-icon"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7"><path d="M12 16V4m0 0L7.5 8.5M12 4l4.5 4.5M4 15v4a1 1 0 0 0 1 1h14a1 1 0 0 0 1-1v-4" stroke-linecap="round" stroke-linejoin="round"/></svg></div>
            <h3>Drop your workbook here</h3>
            <p>or select a file from your device</p>
            <input #fileInput class="file-input" type="file" accept=".xlsx" (change)="onFileSelected($event)" aria-label="Choose an Excel workbook">
            <button type="button" class="browse-button" (click)="fileInput.click()">Browse files</button>
          </div>
          <p class="file-limit">Excel workbook <b>·</b> .xlsx format <b>·</b> Up to 5 MB</p>

          <div class="selected-file" *ngIf="selectedFile">
            <span class="file-badge">XLSX</span>
            <div class="file-info"><strong>{{ selectedFile.name }}</strong><small>{{ formatBytes(selectedFile.size) }} · Ready to scan</small></div>
            <button class="remove-file" type="button" aria-label="Remove selected file" (click)="reset()" [disabled]="isLoading">×</button>
          </div>

          <div class="notice notice-error upload-alert" *ngIf="errors.length">
            <span class="notice-icon"><span>!</span></span><div><strong>We couldn’t import this workbook.</strong><ul><li *ngFor="let error of errors">{{ error }}</li></ul></div>
          </div>
          <div class="loading-panel" *ngIf="isLoading" role="status" aria-live="polite">
            <span class="spinner"></span><div>Scanning your workbook…<small>Checking columns, validating employee records, and saving data.</small></div>
          </div>

          <div class="card-actions">
            <button class="secondary-button" type="button" *ngIf="selectedFile && !isLoading" (click)="reset()">Clear</button>
            <button class="primary-button" type="button" (click)="upload()" [disabled]="!selectedFile || isLoading">
              <span *ngIf="!isLoading">{{ result ? 'Import another file' : 'Scan & import' }} <span aria-hidden="true">↗</span></span>
              <span *ngIf="isLoading">Scanning…</span>
            </button>
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
        <section class="panel guide-card">
          <div class="guide-title"><h2>Workbook checklist</h2><span class="guide-icon">✓</span></div>
          <p class="guide-intro">Make sure row 1 contains these column names in this exact order.</p>
          <div class="column-list"><span class="column-pill" *ngFor="let column of columns">{{ column }}</span></div>
          <p class="guide-foot">Required fields must be filled. Salary must be positive and HireDate must be a valid Excel date.</p>
        </section>
        <section class="tip-card">
          <p class="eyebrow">A little heads-up</p><h3>Already in the database? We’ll catch it.</h3>
          <p>Duplicate employee codes are checked before saving, so you can fix the sheet without creating messy records.</p>
          <a class="history-link" routerLink="/employees">Explore employee directory <span>→</span></a>
        </section>
      </aside>
    </div>
  `
})
export class UploadPageComponent {
  private readonly api = inject(AnalyticsApiService);
  readonly columns = ['EmployeeCode', 'FirstName', 'LastName', 'Department', 'JobTitle', 'Salary', 'HireDate'];
  selectedFile: File | null = null;
  isDragging = false;
  isLoading = false;
  errors: string[] = [];
  result: ImportResult | null = null;
  importedEmployees: Employee[] = [];
  loadedFileName = '';
  isPreviewLoading = false;

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];
    if (file) this.setFile(file);
    input.value = '';
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDragging = true;
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragging = false;
    const file = event.dataTransfer?.files[0];
    if (file) this.setFile(file);
  }

  upload(): void {
    if (!this.selectedFile || this.isLoading) return;
    const file = this.selectedFile;
    this.errors = [];
    this.result = null;
    this.importedEmployees = [];
    this.isLoading = true;
    this.api.upload(file).subscribe({
      next: (result) => {
        this.result = result;
        this.loadedFileName = result.fileName || file.name;
        this.selectedFile = null;
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

  reset(): void {
    this.selectedFile = null;
    this.errors = [];
    this.result = null;
    this.importedEmployees = [];
    this.loadedFileName = '';
    this.isPreviewLoading = false;
  }

  formatBytes(bytes: number): string {
    return bytes < 1024 * 1024 ? `${(bytes / 1024).toFixed(0)} KB` : `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
  }

  initials(employee: Employee): string {
    return `${employee.firstName.charAt(0)}${employee.lastName.charAt(0)}`.toUpperCase();
  }

  private setFile(file: File): void {
    this.errors = [];
    this.result = null;
    this.importedEmployees = [];
    this.loadedFileName = '';
    this.isPreviewLoading = false;
    if (!file.name.toLowerCase().endsWith('.xlsx')) {
      this.selectedFile = null;
      this.errors = ['Only .xlsx Excel files are allowed.'];
      return;
    }
    if (file.size > 5 * 1024 * 1024) {
      this.selectedFile = null;
      this.errors = ['File size must not exceed 5 MB.'];
      return;
    }
    this.selectedFile = file;
  }
}
