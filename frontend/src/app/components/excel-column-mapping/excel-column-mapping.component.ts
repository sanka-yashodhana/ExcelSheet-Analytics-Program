import { Component, EventEmitter, inject, Input, OnInit, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

import {
  AnalyticsApiService,
  ExcelHeaderDetectionResponse,
  ExcelHeaderMapping
} from 'src/app/services/analytics-api.service';

@Component({
  selector: 'app-excel-column-mapping',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './excel-column-mapping.component.html',
  styleUrl: './excel-column-mapping.component.css'
})
export class ExcelColumnMappingComponent implements OnInit {

  private excelImportService = inject(AnalyticsApiService);

  @Input() isImporting = false;
  @Input() importCompleted = false;

  @Output() importRequested = new EventEmitter<{
    file: File;
    headers: ExcelHeaderMapping[];
  }>();

  @Output() fileChanged = new EventEmitter<void>();

  selectedFile: File | null = null;
  isDragging = false;

  maxFileSizeMB = 100;
  maxRecords = 500000;

  headers: ExcelHeaderMapping[] = [];

  detectionResult: ExcelHeaderDetectionResponse | null = null;

  employeeFields: string[] = [
    'EmployeeCode',
    'FirstName',
    'LastName',
    'Department',
    'JobTitle',
    'Salary',
    'HireDate'
  ];

  isLoading = false;

  errorMessage = '';

  successMessage = '';

  ngOnInit(): void {
    this.excelImportService.getImportConfig().subscribe({
      next: (config) => {
        if (config.maxFileSizeMB > 0) this.maxFileSizeMB = config.maxFileSizeMB;
        if (config.maxRecords > 0) this.maxRecords = config.maxRecords;
      },
      error: () => {
        // Fallback to configured defaults if offline
      }
    });
  }

  // Select Excel file
  onFileSelected(event: Event): void {

    const input = event.target as HTMLInputElement;

    const file = input.files?.[0];

    if (!file) {
      return;
    }

    this.setFile(file);
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

  clearFile(): void {
    if (this.isImporting) return;
    this.selectedFile = null;
    this.headers = [];
    this.detectionResult = null;
    this.errorMessage = '';
    this.successMessage = '';
    this.fileChanged.emit();
  }

  requestImport(): void {
    if (!this.selectedFile || !this.isMappingValid || this.isLoading || this.isImporting || this.importCompleted) return;
    this.importRequested.emit({
      file: this.selectedFile,
      headers: this.headers.map(header => ({ ...header }))
    });
  }

  formatFileSize(bytes: number): string {
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    return `${(bytes / (1024 * 1024)).toFixed(2)} MB`;
  }

  private setFile(file: File): void {
    this.errorMessage = '';
    this.successMessage = '';
    this.headers = [];
    this.detectionResult = null;
    this.fileChanged.emit();

    if (!file.name.toLowerCase().endsWith('.xlsx')) {
      this.selectedFile = null;
      this.errorMessage = 'Only .xlsx Excel files are allowed.';
      return;
    }

    const maxBytes = this.maxFileSizeMB * 1024 * 1024;
    if (file.size > maxBytes) {
      this.selectedFile = null;
      this.errorMessage = `File size must not exceed ${this.maxFileSizeMB} MB.`;
      return;
    }

    this.selectedFile = file;
    this.detectHeaders();
  }

  // Call .NET header detection API
  detectHeaders(): void {

    if (!this.selectedFile) {
      return;
    }

    this.isLoading = true;

    this.excelImportService
      .detectHeaders(this.selectedFile)
      .subscribe({

        next: (response) => {

          this.detectionResult = response;

          this.headers = response.headers.map(header => ({
            ...header
          }));

          this.isLoading = false;

          this.successMessage = 'Excel headers detected successfully.';

        },

        error: (error) => {
          this.errorMessage = this.excelImportService
            .getErrorMessages(error)
            .join(' ');

          this.isLoading = false;

        }

      });
  }

  // Check whether required fields are mapped
  get missingRequiredFields(): string[] {

    const mappedFields = this.headers
      .map(header => header.suggestedField)
      .filter((field): field is string => field !== null);

    return this.employeeFields.filter(
      field => !mappedFields.includes(field)
    );
  }

  // Check whether mappings are valid
  get isMappingValid(): boolean {

    const mappedFields = this.headers
      .map(header => header.suggestedField)
      .filter((field): field is string => field !== null);

    const hasDuplicates =
      new Set(mappedFields).size !== mappedFields.length;

    return this.missingRequiredFields.length === 0 &&
           !hasDuplicates;
  }

  isFieldMappedElsewhere(index: number, field: string): boolean {
    return this.headers.some(
      (header, headerIndex) =>
        headerIndex !== index && header.suggestedField === field
    );
  }

  // Change mapping manually
  updateMapping(index: number, field: string): void {

    this.headers[index].suggestedField = field || null;

    this.headers[index].isMatched = !!field;

    this.headers[index].isRequired = !!field &&
      this.employeeFields.includes(field);
  }

}
