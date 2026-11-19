import { Component, OnInit, inject } from '@angular/core';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration } from 'chart.js';

import { EmployeeAnalyticsServices } from 'src/app/services/employee-analytics.service';

@Component({
  selector: 'app-employee-salary-chart',
  standalone: true,
  imports: [BaseChartDirective],
  templateUrl: './employee-salary-chart.component.html',
  styleUrl: './employee-salary-chart.component.css'
})
export class EmployeeSalaryChart implements OnInit {

  private analyticsService = inject(EmployeeAnalyticsServices);

  public doughnutChartType: 'doughnut' = 'doughnut';

  public doughnutChartData: ChartConfiguration<'doughnut'>['data'] = {
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

  public doughnutChartOptions: ChartConfiguration<'doughnut'>['options'] = {
    responsive: true,
    plugins: {
      legend: {
        position: 'bottom',
        labels: { color: '#c4d2c7' }
      },
      title: {
        display: true,
        text: 'Salary Expenses by Department',
        color: '#edf7ef'
      },
      tooltip: {
        callbacks: {
          label: (context) => {
            return `${context.label}: ${Number(context.raw).toLocaleString()}`;
          }
        }
      }
    }
  };

  ngOnInit(): void {

    this.analyticsService.getSalaryByDepartment().subscribe({

      next: (statistics) => {

        this.doughnutChartData = {
          labels: statistics.map(item => item.department),
          datasets: [
            {
              label: 'Total Salary',
              data: statistics.map(item => item.totalSalary),
              backgroundColor: ['#a3e635', '#65a30d', '#4ade80', '#22c55e', '#86efac', '#d9f99d'],
              borderColor: '#111a14',
              borderWidth: 3
            }
          ]
        };

      },

      error: (error) => {
        console.error('Error loading salary statistics:', error);
      }

    });

  }
}