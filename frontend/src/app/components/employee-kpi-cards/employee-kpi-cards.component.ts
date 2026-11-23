import { Component, OnInit, inject } from '@angular/core';
import { EmployeeAnalyticsServices, normalizeDepartment } from 'src/app/services/employee-analytics.service';
import { Employee } from 'src/app/services/analytics-api.service';

@Component({
  selector: 'app-employee-kpi-cards',
  standalone: true,
  imports: [],
  templateUrl: './employee-kpi-cards.component.html',
  styleUrl: './employee-kpi-cards.component.css'
})
export class EmployeeKpiCards implements OnInit {

  private analyticsService = inject(EmployeeAnalyticsServices);

  totalEmployees = 0;
  totalDepartments = 0;
  averageSalary = 0;

  get formattedAverageSalary(): string {
    if (!this.averageSalary || isNaN(this.averageSalary)) {
      return '0.00';
    }
    const parts = this.averageSalary.toFixed(2).split('.');
    parts[0] = parts[0].replace(/\B(?=(\d{3})+(?!\d))/g, ' ');
    return parts.join('.');
  }

  ngOnInit(): void {

    this.analyticsService.getAllEmployees().subscribe({

      next: (employees: Employee[]) => {

        this.totalEmployees = employees.length;

        const validDepartments = employees
          .map(employee => normalizeDepartment(employee.department))
          .filter((dept): dept is string => !!dept);

        this.totalDepartments = new Set(validDepartments).size;

        this.averageSalary = employees.length > 0
          ? employees.reduce((total, employee) => total + employee.salary, 0)
            / employees.length
          : 0;

      },

      error: (error) => {
        console.error('Error loading KPI information:', error);
      }

    });

  }
}