import { Routes } from '@angular/router';
import { UploadPageComponent } from './pages/upload-page.component';
import { EmployeesPageComponent } from './pages/employees-page.component';
import { ImportHistoryPageComponent } from './pages/import-history-page.component';
import { EmployeeChartsPageComponent } from './pages/employee-charts-page.component';

export const routes: Routes = [
  { path: '', component: UploadPageComponent, title: 'Import spreadsheet | Sheetwise' },
  { path: 'employees', component: EmployeesPageComponent, title: 'Employee directory | Sheetwise' },
  { path: 'imports', component: ImportHistoryPageComponent, title: 'Import history | Sheetwise' },
  { path: 'imports/:id', component: ImportHistoryPageComponent, title: 'Workbook details | Sheetwise' },
  { path: 'charts', component: EmployeeChartsPageComponent, title: 'Charts | Sheetwise' },
  { path: '**', redirectTo: '' }
];
