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

  private readonly barColors = [
    '#6366f1', // Indigo
    '#3b82f6', // Blue
    '#06b6d4', // Cyan
    '#10b981', // Emerald
    '#f59e0b', // Amber
    '#f97316', // Orange
    '#ec4899', // Pink
    '#8b5cf6', // Violet
    '#14b8a6', // Teal
    '#e11d48'  // Rose
  ];

  private readonly barBorders = [
    '#818cf8',
    '#60a5fa',
    '#22d3ee',
    '#34d399',
    '#fbbf24',
    '#fb923c',
    '#f472b6',
    '#a78bfa',
    '#2dd4bf',
    '#f43f5e'
  ];

  public barChartType: 'bar' = 'bar';

  public barChartData: ChartConfiguration<'bar'>['data'] = {
    labels: [],
    datasets: [
      {
        label: 'Number of Employees',
        data: [],
        backgroundColor: this.barColors,
        borderColor: this.barBorders,
        borderWidth: 1.5,
        borderRadius: 6
      }
    ]
  };

  public barChartOption: ChartConfiguration<'bar'>['options'] = {
    responsive: true,
    plugins: {
      legend: {
        display: false
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
              backgroundColor: this.barColors.slice(0, statistics.length),
              borderColor: this.barBorders.slice(0, statistics.length),
              borderWidth: 1.5,
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
