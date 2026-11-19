import { Component } from '@angular/core';
import { EmployeeDepartmentChartComponent } from '../components/employee-department-chart/employee-department-chart.component';
import { EmployeeDepartmentPieChart } from '../components/employee-department-pie-chart/employee-department-pie-chart.component';
import { EmployeeHiringTrendsChart } from '../components/employee-hiring-trends-chart/employee-hiring-trends-chart.component';
import { EmployeeKpiCards } from '../components/employee-kpi-cards/employee-kpi-cards.component';
import { EmployeeSalaryChart } from '../components/employee-salary-chart/employee-salary-chart.component';

@Component({
  standalone: true,
  imports: [
    EmployeeDepartmentChartComponent,
    EmployeeDepartmentPieChart,
    EmployeeHiringTrendsChart,
    EmployeeKpiCards,
    EmployeeSalaryChart
  ],
  template: `
    <section class="charts-page">
      <header class="charts-heading">
        <div>
          <p class="eyebrow">People analytics / Overview</p>
          <h1>Charts &amp; insights</h1>
          <p>Explore your workforce by department, compensation, and hiring activity.</p>
        </div>
        <span class="charts-live"><i></i> Live workforce data</span>
      </header>

      <section class="kpi-section" aria-label="Workforce summary">
        <app-employee-kpi-cards></app-employee-kpi-cards>
      </section>

      <section class="charts-grid" aria-label="Employee analytics charts">
        <article class="chart-card chart-card-wide">
          <div class="chart-card-heading"><span class="chart-index">01</span><span>Team composition</span></div>
          <app-employee-department-chart></app-employee-department-chart>
        </article>
        <article class="chart-card">
          <div class="chart-card-heading"><span class="chart-index">02</span><span>Department share</span></div>
          <app-employee-department-pie-chart></app-employee-department-pie-chart>
        </article>
        <article class="chart-card">
          <div class="chart-card-heading"><span class="chart-index">03</span><span>Compensation</span></div>
          <app-employee-salary-chart></app-employee-salary-chart>
        </article>
        <article class="chart-card chart-card-wide">
          <div class="chart-card-heading"><span class="chart-index">04</span><span>Hiring momentum</span></div>
          <app-employee-hiring-trends-chart></app-employee-hiring-trends-chart>
        </article>
      </section>
    </section>
  `
})
export class EmployeeChartsPageComponent {}