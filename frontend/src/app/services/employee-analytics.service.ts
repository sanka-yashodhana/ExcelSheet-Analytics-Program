import { Injectable, inject } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { map, Observable, pipe } from "rxjs";

import { Employee } from "./analytics-api.service";

export interface DepartmentStatistic {
  department: string;
  employeeCount: number;
}

export interface DepartmentSalaryStatistic {
  department: string;
  totalSalary: number;
}

export interface HiringTrendStatistic {
  year: number;
  employeeCount: number;
}

@Injectable({
  providedIn: 'root'
})

export class EmployeeAnalyticsServices {

  private http = inject(HttpClient);

  private apiUrl = 'http://localhost:5239/api/Employees';

  getEmployeeByDepartment() : Observable<DepartmentStatistic[]> {

    return this.http.get<Employee[]>(this.apiUrl).pipe(
      map(employees => {
        const departmentCounts = new Map<string, number>();

        employees.forEach(employee => {

          const department = employee.department;

          const currentCount = departmentCounts.get(department) || 0;

          departmentCounts.set(department, currentCount + 1);
        });

        return Array.from(departmentCounts, ([department, employeeCount]) => ({
          department,
          employeeCount
        }));
      })
    )
  }

  getAllEmployees(): Observable<Employee[]> {
  return this.http.get<Employee[]>(this.apiUrl);
}

getSalaryByDepartment(): Observable<DepartmentSalaryStatistic[]> {

  return this.getAllEmployees().pipe(

    map(employees => {

      const salaryTotals = new Map<string, number>();

      employees.forEach(employee => {

        const department = employee.department;

        const currentTotal = salaryTotals.get(department) || 0;

        salaryTotals.set(
          department,
          currentTotal + employee.salary
        );

      });

      return Array.from(
        salaryTotals,
        ([department, totalSalary]) => ({
          department,
          totalSalary
        })
      );

    })

  );
}

getHiringTrends(): Observable<HiringTrendStatistic[]> {

  return this.getAllEmployees().pipe(

    map(employees => {

      const hiringCounts = new Map<number, number>();

      employees.forEach(employee => {

        const year = new Date(employee.hireDate).getFullYear();

        const currentCount = hiringCounts.get(year) || 0;

        hiringCounts.set(year, currentCount + 1);

      });

      return Array.from(
        hiringCounts,
        ([year, employeeCount]) => ({
          year,
          employeeCount
        })
      ).sort((a, b) => a.year - b.year);

    })

  );
}
}