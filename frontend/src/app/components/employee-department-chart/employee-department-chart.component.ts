import { Component, OnInit, inject } from '@angular/core';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration } from 'chart.js';

import { EmployeeAnalyticsServices } from 'src/app/services/employee-analytics.service';

@Component({
  selector: 'app-employee-department-chart',
  imports: [BaseChartDirective],
  templateUrl: './employee-department-chart.component.html',
  styleUrl: './employee-department-chart.component.css'
})
export class EmployeeDepartmentChartComponent implements OnInit{
      
  private analyticsService = inject(EmployeeAnalyticsServices);

  public barChartType: 'bar' = 'bar';

  public barChartData: ChartConfiguration<'bar'>['data'] = {
    labels: [],
    datasets: [
      {
        label: 'Number of Employees',
        data: [],
        backgroundColor: 'rgba(163, 230, 53, 0.72)',
        borderColor: '#a3e635',
        borderWidth: 1,
        borderRadius: 6
      }
    ]
  };

  public barChartOption: ChartConfiguration<'bar'>['options'] = {
    responsive: true,
    plugins: {
      legend: {
        display: true,
        labels: { color: '#c4d2c7' }
      },
      title: {
        display: true,
        text: 'Employee by Department',
        color: '#edf7ef'
      }
    },
    scales: {
      x: { ticks: { color: '#9caf9f' }, grid: { color: 'rgba(156, 175, 159, 0.12)' } },
      y: { beginAtZero: true, ticks: { color: '#9caf9f', precision: 0 }, grid: { color: 'rgba(156, 175, 159, 0.12)' } }
    }
  };

  ngOnInit(): void {
    
    this.analyticsService.getEmployeeByDepartment().subscribe({

      next: (statistics) => {

        this.barChartData = {
          labels: statistics.map(item => item.department),
          datasets: [
            {
              label: 'Number of Employees',
              data: statistics.map(item => item.employeeCount),
              backgroundColor: 'rgba(163, 230, 53, 0.72)',
              borderColor: '#a3e635',
              borderWidth: 1,
              borderRadius: 6
            }
          ]
        };
      },

      error: (error) => {
        console.error('Error loading department statistics:', error);
      }
    });
  }
}
