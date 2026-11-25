import { CommonModule, DecimalPipe } from '@angular/common';
import { Component, OnDestroy, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Subject, Subscription, interval, switchMap, takeUntil } from 'rxjs';
import {
  AnalyticsApiService,
  Employee,
  ExcelHeaderMapping,
  ImportJobProgress
} from '../services/analytics-api.service';

import { ExcelColumnMappingComponent } from '../components/excel-column-mapping/excel-column-mapping.component';

@Component({
  standalone: true,
  imports: [CommonModule, DecimalPipe, RouterLink, ExcelColumnMappingComponent],
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
            [importCompleted]="isImportFinishedSuccess"
            (fileChanged)="onWorkbookSelected()"
            (importRequested)="upload($event)"
          />

          <div class="notice notice-error upload-alert" *ngIf="errors.length">
            <span class="notice-icon"><span>!</span></span>
            <div>
              <strong>We couldn’t import this workbook.</strong>
              <ul><li *ngFor="let error of errors">{{ error }}</li></ul>
              <button
                *ngIf="activeJob?.hasErrorReport"
                class="secondary-button error-report-btn"
                type="button"
                [disabled]="isDownloadingErrorReport"
                (click)="downloadErrorReport()"
                style="margin-top: 12px"
              >
                {{ isDownloadingErrorReport ? 'Downloading error report…' : 'Download Error Report (.csv)' }}
              </button>
            </div>
          </div>

          <!-- Real-Time Large Import Progress Panel -->
          <section class="import-progress-panel" *ngIf="activeJob" aria-live="polite">
            <div class="progress-header-row">
              <div class="progress-title-group">
                <span class="spinner" *ngIf="isLoading"></span>
                <div>
                  <strong>{{ activeJob.fileName || loadedFileName }}</strong>
                  <small>{{ activeJob.currentMessage }}</small>
                </div>
              </div>
              <span class="import-status-badge" [ngClass]="statusBadgeClass(activeJob.status)">
                {{ formatStatus(activeJob.status) }}
              </span>
            </div>

            <div class="progress-bar-wrap">
              <div class="progress-track">
                <div
                  class="progress-fill"
                  [class.progress-fill-error]="activeJob.status === 'Failed'"
                  [class.progress-fill-warn]="activeJob.status === 'CompletedWithErrors'"
                  [style.width.%]="activeJob.percentage"
                ></div>
              </div>
              <span class="progress-percent">{{ activeJob.percentage | number:'1.0-1' }}%</span>
            </div>

            <div class="progress-stats-grid">
              <div class="progress-stat">
                <span>Total records</span>
                <strong>{{ activeJob.totalRecords | number }}</strong>
              </div>
              <div class="progress-stat">
                <span>Processed</span>
                <strong>{{ activeJob.processedRecords | number }}</strong>
              </div>
              <div class="progress-stat stat-success">
                <span>Imported</span>
                <strong>{{ activeJob.importedRecords | number }}</strong>
              </div>
              <div class="progress-stat" [class.stat-rejected]="activeJob.rejectedRecords > 0">
                <span>Rejected</span>
                <strong>{{ activeJob.rejectedRecords | number }}</strong>
              </div>
            </div>
          </section>
        </section>

        <!-- Completed Import Summary & Error Report Card -->
        <section class="panel result-card" *ngIf="activeJob && isImportFinishedSuccess" aria-live="polite">
          <div class="result-header">
            <span class="result-check" [class.result-warn]="activeJob.rejectedRecords > 0">
              {{ activeJob.rejectedRecords > 0 ? '!' : '✓' }}
            </span>
            <div>
              <h3>{{ activeJob.rejectedRecords > 0 ? 'Import completed with rejected rows' : 'Import complete' }}</h3>
              <p>{{ activeJob.currentMessage }}</p>
            </div>
          </div>

          <div class="result-file">
            <span>▤</span>
            <div>
              <strong>{{ activeJob.fileName || loadedFileName }}</strong>
              <small>History reference #{{ activeJob.importHistoryId }}</small>
            </div>
          </div>

          <div class="result-stats">
            <div class="result-stat">
              <strong>{{ activeJob.importedRecords | number }}</strong>
              <span>Successfully imported</span>
            </div>
            <div class="result-stat">
              <strong>{{ activeJob.rejectedRecords | number }}</strong>
              <span>Rejected rows</span>
            </div>
          </div>

          <!-- Rejected Rows Error Report Section -->
          <div class="error-report-box" *ngIf="activeJob.rejectedRecords > 0">
            <div class="error-report-header">
              <div>
                <strong>{{ activeJob.rejectedRecords | number }} invalid or duplicate rows were quarantined</strong>
                <p>Valid rows were safely bulk-imported. Download the error report to inspect every rejected row and reason.</p>
              </div>
              <button
                *ngIf="activeJob.hasErrorReport"
                class="secondary-button error-report-btn"
                type="button"
                [disabled]="isDownloadingErrorReport"
                (click)="downloadErrorReport()"
              >
                <svg class="btn-svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/></svg>
                {{ isDownloadingErrorReport ? 'Downloading…' : 'Download Error Report (.csv)' }}
              </button>
            </div>

            <ul class="sample-error-list" *ngIf="activeJob.sampleErrors.length">
              <li *ngFor="let err of activeJob.sampleErrors | slice:0:10">{{ err }}</li>
            </ul>
            <small class="card-caption" *ngIf="activeJob.rejectedRecords > 10">
              Showing first 10 errors. Download the CSV report to inspect all {{ activeJob.rejectedRecords | number }} rejected rows.
            </small>
          </div>

          <div class="preview-heading">
            <strong>Imported employee preview</strong>
            <a [routerLink]="['/imports', activeJob.importHistoryId]">View this spreadsheet →</a>
          </div>
          <div class="preview-list" *ngIf="importedEmployees.length; else previewState">
            <div class="preview-person" *ngFor="let employee of importedEmployees | slice:0:5">
              <span class="avatar">{{ initials(employee) }}</span>
              <div>
                <strong>{{ employee.firstName }} {{ employee.lastName }}</strong>
                <span>{{ employee.employeeCode }} · {{ employee.department }}</span>
              </div>
            </div>
          </div>
          <ng-template #previewState>
            <small class="card-caption">{{ isPreviewLoading ? 'Loading imported employee records…' : 'Your import succeeded. View the directory to find these records.' }}</small>
          </ng-template>
        </section>
      </div>

      <aside class="side-stack">
        <!-- 3D Excel Feature Card -->
        <section class="panel excel-showcase-card">
          <div class="excel-card-header">
            <img src="/assets/excel-3d-logo.png" alt="Excel Logo" class="excel-mini-logo" />
            <div>
              <h3>Excel Ingestion Engine</h3>
              <span>XLSX &bull; Streaming Bulk Import</span>
            </div>
          </div>
          <p class="excel-card-desc">Upload employee spreadsheets with thousands of records to automatically map columns, validate batches, and bulk-insert into SQL Server.</p>
        </section>

        <section class="panel guide-card">
          <div class="guide-title"><h2>Workbook checklist</h2><span class="guide-icon">✓</span></div>
          <p class="guide-intro">Map each spreadsheet heading to the employee field it represents before importing.</p>
          <div class="column-list"><span class="column-pill" *ngFor="let column of columns">{{ column }}</span></div>
          <p class="guide-foot">Required fields must be filled. EmployeeCode must follow the format EMP000001 (EMP + 6–7 digits). Salary must be positive, HireDate must be a valid date, duplicate Employee Codes are quarantined, and Department is standardized into predefined departments.</p>
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
export class UploadPageComponent implements OnDestroy {
  private readonly api = inject(AnalyticsApiService);
  private readonly destroy$ = new Subject<void>();
  private pollSub: Subscription | null = null;

  readonly columns = ['EmployeeCode', 'FirstName', 'LastName', 'Department', 'JobTitle', 'Salary', 'HireDate'];
  isLoading = false;
  errors: string[] = [];
  activeJob: ImportJobProgress | null = null;
  importedEmployees: Employee[] = [];
  loadedFileName = '';
  isPreviewLoading = false;
  isDownloadingErrorReport = false;

  get isImportFinishedSuccess(): boolean {
    if (!this.activeJob) return false;
    return this.activeJob.status === 'Completed' || this.activeJob.status === 'CompletedWithErrors';
  }

  upload(request: { file: File; headers: ExcelHeaderMapping[] }): void {
    if (this.isLoading) return;
    this.stopPolling();

    const { file, headers } = request;
    this.errors = [];
    this.activeJob = null;
    this.importedEmployees = [];
    this.loadedFileName = file.name;
    this.isLoading = true;

    this.api.startLargeImport(file, headers).pipe(takeUntil(this.destroy$)).subscribe({
      next: (startRes) => {
        this.activeJob = {
          jobId: startRes.jobId,
          importHistoryId: startRes.importHistoryId,
          fileName: startRes.fileName,
          status: startRes.status,
          totalRecords: 0,
          processedRecords: 0,
          importedRecords: 0,
          rejectedRecords: 0,
          percentage: 0,
          currentMessage: startRes.message,
          hasErrorReport: false,
          errorReportUrl: null,
          sampleErrors: [],
          startedAt: new Date().toISOString(),
          completedAt: null
        };
        this.startPolling(startRes.jobId);
      },
      error: (error) => {
        this.errors = this.api.getErrorMessages(error);
        this.isLoading = false;
      }
    });
  }

  private startPolling(jobId: string): void {
    this.stopPolling();

    this.pollSub = interval(450).pipe(
      takeUntil(this.destroy$),
      switchMap(() => this.api.getImportJobProgress(jobId))
    ).subscribe({
      next: (progress) => {
        this.activeJob = progress;

        const isTerminal =
          progress.status === 'Completed' ||
          progress.status === 'CompletedWithErrors' ||
          progress.status === 'Failed';

        if (!isTerminal) {
          return;
        }

        this.stopPolling();
        this.isLoading = false;

        if (progress.status === 'Failed') {
          this.errors = progress.sampleErrors.length
            ? progress.sampleErrors.slice(0, 10)
            : [progress.currentMessage || 'Import failed.'];
          return;
        }

        this.loadImportedPreview(progress.importHistoryId);
      },
      error: (error) => {
        this.stopPolling();
        this.errors = this.api.getErrorMessages(error);
        this.isLoading = false;
      }
    });
  }

  private loadImportedPreview(importHistoryId: number): void {
    if (!importHistoryId) return;
    this.isPreviewLoading = true;
    this.api.getEmployeesPaged({ importHistoryId, page: 1, pageSize: 5 })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (paged) => {
          this.importedEmployees = paged.items;
          this.isPreviewLoading = false;
        },
        error: () => {
          this.importedEmployees = [];
          this.isPreviewLoading = false;
        }
      });
  }

  downloadErrorReport(): void {
    if (!this.activeJob?.jobId || this.isDownloadingErrorReport) return;
    const jobId = this.activeJob.jobId;
    const fileName = this.activeJob.fileName || this.loadedFileName || 'import';
    this.isDownloadingErrorReport = true;

    this.api.downloadJobErrorReport(jobId).pipe(takeUntil(this.destroy$)).subscribe({
      next: (blob: Blob) => {
        this.isDownloadingErrorReport = false;
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        const baseName = fileName.replace(/\.xlsx$/i, '');
        a.download = `${baseName}_error_report.csv`;
        a.href = url;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
      },
      error: (err: unknown) => {
        this.isDownloadingErrorReport = false;
        alert('Failed to download error report: ' + this.api.getErrorMessages(err).join(' '));
      }
    });
  }

  onWorkbookSelected(): void {
    this.stopPolling();
    this.errors = [];
    this.activeJob = null;
    this.importedEmployees = [];
    this.loadedFileName = '';
    this.isPreviewLoading = false;
    this.isLoading = false;
  }

  initials(employee: Employee): string {
    return `${employee.firstName.charAt(0)}${employee.lastName.charAt(0)}`.toUpperCase();
  }

  formatStatus(status: string): string {
    switch (status) {
      case 'CompletedWithErrors': return 'Completed with errors';
      default: return status;
    }
  }

  statusBadgeClass(status: string): string {
    switch (status) {
      case 'Completed': return 'status-badge-success';
      case 'CompletedWithErrors': return 'status-badge-warn';
      case 'Failed': return 'status-badge-error';
      default: return 'status-badge-active';
    }
  }

  private stopPolling(): void {
    if (this.pollSub) {
      this.pollSub.unsubscribe();
      this.pollSub = null;
    }
  }

  ngOnDestroy(): void {
    this.stopPolling();
    this.destroy$.next();
    this.destroy$.complete();
  }
}
