import { Component, OnInit, inject } from '@angular/core';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration } from 'chart.js';

import { EmployeeAnalyticsServices } from 'src/app/services/employee-analytics.service';

@Component({
  selector: 'app-employee-department-pie-chart',
  standalone: true,
  imports: [BaseChartDirective],
  templateUrl: './employee-department-pie-chart.component.html',
  styleUrl: './employee-department-pie-chart.component.css'
})
export class EmployeeDepartmentPieChart implements OnInit {

  private analyticsService = inject(EmployeeAnalyticsServices);

  public pieChartType: 'pie' = 'pie';

  public pieChartData: ChartConfiguration<'pie'>['data'] = {
    labels: [],
    datasets: [
      {
        data: [],
        backgroundColor: ['#a3e635', '#65a30d', '#4ade80', '#22c55e', '#86efac', '#d9f99d'],
        borderColor: '#111a14',
        borderWidth: 3
      }
    ]
  };

  public pieChartOptions: ChartConfiguration<'pie'>['options'] = {
    responsive: true,
    plugins: {
      legend: {
        position: 'bottom',
        labels: { color: '#c4d2c7' }
      },
      title: {
        display: true,
        text: 'Employee Distribution by Department',
        color: '#edf7ef'
      }
    }
  };

  ngOnInit(): void {

    this.analyticsService.getEmployeeByDepartment().subscribe({

      next: (statistics) => {

        this.pieChartData = {
          labels: statistics.map(item => item.department),
          datasets: [
            {
              data: statistics.map(item => item.employeeCount),
              backgroundColor: ['#a3e635', '#65a30d', '#4ade80', '#22c55e', '#86efac', '#d9f99d'],
              borderColor: '#111a14',
              borderWidth: 3
            }
          ]
        };

      },

      error: (error) => {
        console.error('Error loading department distribution:', error);
      }

    });

  }
}