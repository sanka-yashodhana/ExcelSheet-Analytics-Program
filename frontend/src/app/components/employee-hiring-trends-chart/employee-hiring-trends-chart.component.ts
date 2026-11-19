import { Component, OnInit, inject } from '@angular/core';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration } from 'chart.js';

import { EmployeeAnalyticsServices } from 'src/app/services/employee-analytics.service';

@Component({
  selector: 'app-employee-hiring-trends-chart',
  standalone: true,
  imports: [BaseChartDirective],
  templateUrl: './employee-hiring-trends-chart.component.html',
  styleUrl: './employee-hiring-trends-chart.component.css'
})
export class EmployeeHiringTrendsChart implements OnInit {

  private analyticsService = inject(EmployeeAnalyticsServices);

  public lineChartType: 'line' = 'line';

  public lineChartData: ChartConfiguration<'line'>['data'] = {
    labels: [],
    datasets: [
      {
        label: 'Employees Hired',
        data: [],
        borderColor: '#a3e635',
        backgroundColor: 'rgba(163, 230, 53, 0.15)',
        pointBackgroundColor: '#a3e635',
        pointBorderColor: '#111a14',
        pointRadius: 4,
        tension: 0.3,
        fill: true
      }
    ]
  };

  public lineChartOptions: ChartConfiguration<'line'>['options'] = {
    responsive: true,
    plugins: {
      legend: {
        display: true,
        labels: { color: '#c4d2c7' }
      },
      title: {
        display: true,
        text: 'Employee Hiring Trends',
        color: '#edf7ef'
      }
    },
    scales: {
      y: {
        beginAtZero: true,
        ticks: {
          color: '#9caf9f',
          precision: 0
        },
        grid: { color: 'rgba(156, 175, 159, 0.12)' }
      },
      x: { ticks: { color: '#9caf9f' }, grid: { color: 'rgba(156, 175, 159, 0.12)' } }
    }
  };

  ngOnInit(): void {

    this.analyticsService.getHiringTrends().subscribe({

      next: (statistics) => {

        this.lineChartData = {
          labels: statistics.map(item => item.year.toString()),
          datasets: [
            {
              label: 'Employees Hired',
              data: statistics.map(item => item.employeeCount),
              borderColor: '#a3e635',
              backgroundColor: 'rgba(163, 230, 53, 0.15)',
              pointBackgroundColor: '#a3e635',
              pointBorderColor: '#111a14',
              pointRadius: 4,
              tension: 0.3,
              fill: true
            }
          ]
        };

      },

      error: (error) => {
        console.error('Error loading hiring trends:', error);
      }

    });

  }
}