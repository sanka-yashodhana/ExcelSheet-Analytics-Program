import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AnalyticsApiService, Employee, ImportHistory } from '../services/analytics-api.service';

@Component({
  standalone: true,
  imports: [CommonModule, CurrencyPipe, DatePipe, RouterLink],
  template: `
    <section class="page-heading">
      <div>
        <p class="eyebrow">{{ historyId === null ? 'Every workbook, accounted for' : 'Workbook history #' + historyId }}</p>
        <h1>{{ historyId === null ? 'Import history' : (selectedHistory?.fileName || 'Workbook details') }}</h1>
        <p>{{ historyId === null ? 'Browse uploaded spreadsheets and open the exact employee data from each one.' : 'Employee records linked to this specific spreadsheet upload.' }}</p>
      </div>
      <a class="secondary-button" *ngIf="historyId !== null" routerLink="/imports">← All spreadsheets</a>
      <a class="primary-button" *ngIf="historyId === null" routerLink="/"> Import a workbook</a>
    </section>

    <ng-container *ngIf="historyId === null; else detailView">
      <div class="directory-stats history-stats">
        <section class="panel metric-card">
          <span class="metric-icon">
            <svg class="metric-svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
              <polyline points="14 2 14 8 20 8"/>
              <path d="M8 13h8"/>
              <path d="M8 17h8"/>
              <path d="M12 13v8"/>
            </svg>
          </span>
          <div><span>Excel sheets uploaded</span><strong>{{ histories.length }}</strong></div>
        </section>

        <section class="panel metric-card">
          <span class="metric-icon">
            <svg class="metric-svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <ellipse cx="12" cy="5" rx="9" ry="3"/>
              <path d="M21 12c0 1.66-4 3-9 3s-9-1.34-9-3"/>
              <path d="M3 5v14c0 1.66 4 3 9 3s9-1.34 9-3V5"/>
            </svg>
          </span>
          <div><span>Employee records stored</span><strong>{{ storedEmployeeCount }}</strong></div>
        </section>

        <section class="panel metric-card">
          <span class="metric-icon">
            <svg class="metric-svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/>
              <polyline points="22 4 12 14.01 9 11.01"/>
            </svg>
          </span>
          <div><span>Successful imports</span><strong>{{ completedImportCount }}</strong></div>
        </section>
      </div>

      <section class="panel directory-card">
        <div class="table-toolbar"><div><h2>Uploaded spreadsheets</h2><p>Select a workbook to inspect only the employee data imported from it.</p></div><span class="soft-label"><i></i> {{ histories.length }} total uploads</span></div>
        <div class="loading-state" *ngIf="isLoading"><span class="spinner"></span>Loading import history…</div>
        <div class="notice notice-error directory-error" *ngIf="errors.length"><span class="notice-icon"><span>!</span></span><div><strong>Could not load import history.</strong><ul><li *ngFor="let error of errors">{{ error }}</li></ul></div></div>
        <div class="empty-state" *ngIf="!isLoading && !errors.length && histories.length === 0"><span class="empty-symbol">▤</span><h3>No spreadsheets uploaded yet</h3><p>Your completed Excel imports will be listed here.</p><a class="primary-button" routerLink="/" style="margin-top:16px">Import workbook ↗</a></div>
        <div class="table-scroll" *ngIf="!isLoading && !errors.length && histories.length">
          <table class="employee-table history-table">
            <thead><tr><th>Spreadsheet</th><th>History ID</th><th>Uploaded</th><th>Rows imported</th><th>Records now</th><th>Status</th><th style="text-align: right">Actions</th></tr></thead>
            <tbody><tr *ngFor="let history of histories">
              <td><div class="employee-cell"><span class="file-badge">XLSX</span><div><strong>{{ history.fileName }}</strong><span>Workbook import</span></div></div></td>
              <td><span class="code-tag">#{{ history.id }}</span></td>
              <td>{{ history.uploadedDate | date:'MMM d, y · h:mm a' }}</td>
              <td>{{ history.totalRows }}</td>
              <td>{{ history.employeeCount }}</td>
              <td><span class="department-tag">{{ history.status }}</span></td>
              <td style="text-align: right">
                <div class="table-actions">
                  <button class="table-action-button" type="button" (click)="downloadHistory(history, $event)" [disabled]="isDownloading(history.id)" title="Download this Excel file">
                    <svg class="btn-svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/></svg>
                    {{ isDownloading(history.id) ? 'Downloading…' : 'Download .xlsx' }}
                  </button>
                  <a class="history-view-link" [routerLink]="['/imports', history.id]">View data →</a>
                </div>
              </td>
            </tr></tbody>
          </table>
        </div>
        <div class="table-footer" *ngIf="!isLoading && !errors.length && histories.length"><span>{{ histories.length }} spreadsheet{{ histories.length === 1 ? '' : 's' }} in upload history</span><span class="table-status">History retained in database</span></div>
      </section>
    </ng-container>

    <ng-template #detailView>
      <div class="loading-state panel history-load" *ngIf="isLoading"><span class="spinner"></span>Loading upload history…</div>
      <div class="notice notice-error history-load-error" *ngIf="!isLoading && errors.length"><span class="notice-icon"><span>!</span></span><div><strong>Could not load workbook details.</strong><ul><li *ngFor="let error of errors">{{ error }}</li></ul></div></div>
      <div class="detail-summary panel" *ngIf="!isLoading && !errors.length && selectedHistory as history">
        <span class="detail-file-icon">
          <svg class="metric-svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/>
            <polyline points="14 2 14 8 20 8"/>
            <path d="M8 13h8"/>
            <path d="M8 17h8"/>
            <path d="M12 13v8"/>
          </svg>
        </span><div class="detail-file-name"><span>UPLOADED WORKBOOK</span><strong>{{ history.fileName }}</strong></div>
        <div class="detail-meta"><span>History ID</span><strong>#{{ history.id }}</strong></div>
        <div class="detail-meta"><span>Uploaded</span><strong>{{ history.uploadedDate | date:'MMM d, y · h:mm a' }}</strong></div>
        <div class="detail-meta"><span>Records currently stored</span><strong>{{ history.employeeCount }} <small>/ {{ history.totalRows }} imported</small></strong></div>
        <div class="detail-action">
          <button class="primary-button" type="button" (click)="downloadHistory(history)" [disabled]="isDownloading(history.id)">
            <svg class="btn-svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/><polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/></svg>
            {{ isDownloading(history.id) ? 'Downloading…' : 'Download spreadsheet (.xlsx)' }}
          </button>
        </div>
      </div>

      <section class="panel directory-card detail-table-card" *ngIf="!isLoading && !errors.length">
        <div class="table-toolbar"><div><h2>Records from this spreadsheet</h2><p>Only employees with history ID #{{ historyId }} are shown.</p></div><span class="soft-label"><i></i> History ID #{{ historyId }}</span></div>
        <div class="loading-state" *ngIf="isDetailLoading"><span class="spinner"></span>Loading records for this workbook…</div>
        <div class="notice notice-error directory-error" *ngIf="detailErrors.length"><span class="notice-icon"><span>!</span></span><div><strong>Could not load this workbook’s records.</strong><ul><li *ngFor="let error of detailErrors">{{ error }}</li></ul></div></div>
        <div class="empty-state" *ngIf="!isLoading && !errors.length && !isDetailLoading && !detailErrors.length && !selectedHistory"><span class="empty-symbol">⌕</span><h3>Spreadsheet history not found</h3><p>This history ID does not match an uploaded workbook.</p></div>
        <div class="empty-state" *ngIf="!isDetailLoading && !detailErrors.length && selectedHistory && employees.length === 0"><span class="empty-symbol">♙</span><h3>No employee records currently stored</h3><p>This upload’s history is retained, but its employee data may have been deleted.</p></div>
        <div class="table-scroll" *ngIf="!isDetailLoading && !detailErrors.length && employees.length">
          <table class="employee-table"><thead><tr><th>Employee</th><th>Employee code</th><th>Department</th><th>Job title</th><th>Salary</th><th>Hire date</th><th>History ID</th></tr></thead>
            <tbody><tr *ngFor="let employee of employees">
              <td><div class="employee-cell"><span class="avatar">{{ initials(employee) }}</span><div><strong>{{ employee.firstName }} {{ employee.lastName }}</strong><span>Employee #{{ employee.id }}</span></div></div></td>
              <td><span class="code-tag">{{ employee.employeeCode }}</span></td><td><span class="department-tag">{{ employee.department }}</span></td><td>{{ employee.jobTitle }}</td>
              <td class="salary-cell">{{ employee.salary | currency:'USD':'symbol':'1.0-2' }}</td><td>{{ employee.hireDate | date:'MMM d, y' }}</td><td><span class="code-tag">#{{ employee.importHistoryId }}</span></td>
            </tr></tbody>
          </table>
        </div>
        <div class="table-footer" *ngIf="!isDetailLoading && !detailErrors.length && employees.length"><span>{{ employees.length }} record{{ employees.length === 1 ? '' : 's' }} from this workbook</span><span class="table-status">Filtered by history ID</span></div>
      </section>
    </ng-template>
  `
})
export class ImportHistoryPageComponent implements OnInit {
  private readonly api = inject(AnalyticsApiService);
  private readonly route = inject(ActivatedRoute);
  histories: ImportHistory[] = [];
  employees: Employee[] = [];
  historyId: number | null = null;
  isLoading = true;
  isDetailLoading = false;
  errors: string[] = [];
  detailErrors: string[] = [];

  ngOnInit(): void {
    this.api.getImportHistories().subscribe({
      next: histories => { this.histories = histories; this.isLoading = false; },
      error: error => { this.errors = this.api.getErrorMessages(error); this.isLoading = false; }
    });

    this.route.paramMap.subscribe(params => {
      const idValue = params.get('id');
      this.historyId = idValue === null ? null : Number(idValue);
      this.employees = [];
      this.detailErrors = [];
      this.isDetailLoading = false;
      if (this.historyId === null) return;
      if (!Number.isSafeInteger(this.historyId) || this.historyId <= 0) {
        this.detailErrors = ['The spreadsheet history ID is invalid.'];
        return;
      }
      this.isDetailLoading = true;
      this.api.getEmployees(this.historyId).subscribe({
        next: employees => { this.employees = employees; this.isDetailLoading = false; },
        error: error => { this.detailErrors = this.api.getErrorMessages(error); this.isDetailLoading = false; }
      });
    });
  }

  get selectedHistory(): ImportHistory | undefined {
    return this.histories.find(history => history.id === this.historyId);
  }

  get storedEmployeeCount(): number {
    return this.histories.reduce((count, history) => count + history.employeeCount, 0);
  }

  get completedImportCount(): number {
    return this.histories.filter(history => history.status.toLowerCase() === 'completed').length;
  }

  initials(employee: Employee): string {
    return `${employee.firstName.charAt(0)}${employee.lastName.charAt(0)}`.toUpperCase();
  }

  downloadingIds = new Set<number>();

  isDownloading(id: number): boolean {
    return this.downloadingIds.has(id);
  }

  downloadHistory(history: ImportHistory | undefined, event?: Event): void {
    if (event) {
      event.stopPropagation();
    }
    if (!history) return;

    this.downloadingIds.add(history.id);
    this.api.downloadImportHistoryWorkbook(history.id).subscribe({
      next: (blob: Blob) => {
        this.downloadingIds.delete(history.id);
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        const baseName = history.fileName?.trim() || `spreadsheet_${history.id}`;
        a.download = baseName.toLowerCase().endsWith('.xlsx') ? baseName : `${baseName}.xlsx`;
        a.href = url;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
      },
      error: (err: unknown) => {
        this.downloadingIds.delete(history.id);
        alert('Failed to download spreadsheet: ' + this.api.getErrorMessages(err).join(' '));
      }
    });
  }
}
