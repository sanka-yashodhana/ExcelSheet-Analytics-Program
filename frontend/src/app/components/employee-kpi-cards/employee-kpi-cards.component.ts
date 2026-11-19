import { Component, OnInit, inject } from '@angular/core';
import { EmployeeAnalyticsServices } from 'src/app/services/employee-analytics.service';
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

  ngOnInit(): void {

    this.analyticsService.getAllEmployees().subscribe({

      next: (employees: Employee[]) => {

        this.totalEmployees = employees.length;

        this.totalDepartments = new Set(
          employees.map(employee => employee.department)
        ).size;

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