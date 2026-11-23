import { Injectable, inject } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { map, Observable } from "rxjs";

import { Employee } from "./analytics-api.service";

export const PREDEFINED_DEPARTMENTS: readonly string[] = [
  "Human Resources",
  "Information Technology",
  "Finance",
  "Marketing",
  "Sales",
  "Engineering",
  "Operations",
  "Legal",
  "Administration",
  "Customer Support",
] as const;

const DEPARTMENT_ALIASES: Record<string, string> = {
  // Human Resources
  humanresources: "Human Resources",
  humanresource: "Human Resources",
  hr: "Human Resources",
  hrdept: "Human Resources",
  hrdepartment: "Human Resources",
  personnel: "Human Resources",
  people: "Human Resources",
  peopleoperations: "Human Resources",
  peopleculture: "Human Resources",
  talent: "Human Resources",
  talentmanagement: "Human Resources",
  talentacquisition: "Human Resources",

  // Information Technology
  informationtechnology: "Information Technology",
  informationtech: "Information Technology",
  infotech: "Information Technology",
  it: "Information Technology",
  itdept: "Information Technology",
  itdepartment: "Information Technology",
  technology: "Information Technology",
  tech: "Information Technology",
  techsupport: "Information Technology",
  informationsystems: "Information Technology",
  is: "Information Technology",
  mis: "Information Technology",

  // Finance
  finance: "Finance",
  accounting: "Finance",
  account: "Finance",
  accounts: "Finance",
  financeaccounting: "Finance",
  fin: "Finance",
  fiscal: "Finance",
  treasury: "Finance",
  audit: "Finance",

  // Marketing
  marketing: "Marketing",
  mktg: "Marketing",
  digitalmarketing: "Marketing",
  marketresearch: "Marketing",
  marketingcommunications: "Marketing",
  brand: "Marketing",
  branding: "Marketing",
  advertising: "Marketing",
  pr: "Marketing",

  // Sales
  sales: "Sales",
  businessdevelopment: "Sales",
  commercial: "Sales",
  salesmarketing: "Sales",
  bizdev: "Sales",
  bd: "Sales",
  revenue: "Sales",

  // Engineering
  engineering: "Engineering",
  softwareengineering: "Engineering",
  development: "Engineering",
  softwaredevelopment: "Engineering",
  dev: "Engineering",
  software: "Engineering",
  rd: "Engineering",
  researchdevelopment: "Engineering",
  qa: "Engineering",
  qualityassurance: "Engineering",
  testing: "Engineering",

  // Operations
  operations: "Operations",
  ops: "Operations",
  operationsdepartment: "Operations",
  logistics: "Operations",
  supplychain: "Operations",
  production: "Operations",
  manufacturing: "Operations",

  // Legal
  legal: "Legal",
  compliance: "Legal",
  legalcompliance: "Legal",
  regulatory: "Legal",
  governance: "Legal",

  // Administration
  administration: "Administration",
  admin: "Administration",
  generaladministration: "Administration",
  facilities: "Administration",
  officemanagement: "Administration",

  // Customer Support
  customersupport: "Customer Support",
  customerservice: "Customer Support",
  support: "Customer Support",
  clientsupport: "Customer Support",
  customersuccess: "Customer Support",
  clientsuccess: "Customer Support",
  helpdesk: "Customer Support",
  servicedesk: "Customer Support",
  callcenter: "Customer Support",
};

export function normalizeDepartment(
  rawDepartment: string | null | undefined,
): string | null {
  if (!rawDepartment) {
    return null;
  }

  const cleaned = rawDepartment.replace(/[^a-zA-Z0-9]/g, "").toLowerCase();

  if (DEPARTMENT_ALIASES[cleaned]) {
    return DEPARTMENT_ALIASES[cleaned];
  }

  const lower = rawDepartment.trim().toLowerCase();

  if (
    lower === "hr" ||
    lower.startsWith("hr ") ||
    lower.endsWith(" hr") ||
    (lower.includes("human") && lower.includes("resource"))
  ) {
    return "Human Resources";
  }

  if (
    lower === "it" ||
    lower.startsWith("it ") ||
    lower.endsWith(" it") ||
    (lower.includes("info") && lower.includes("tech"))
  ) {
    return "Information Technology";
  }

  if (lower.includes("financ") || lower.includes("account")) {
    return "Finance";
  }

  if (lower.includes("market") || lower.includes("branding")) {
    return "Marketing";
  }

  if (
    lower.includes("sale") ||
    lower.includes("bizdev") ||
    lower.includes("business dev")
  ) {
    return "Sales";
  }

  if (
    lower.includes("engineer") ||
    lower.includes("software") ||
    lower.includes("develop")
  ) {
    return "Engineering";
  }

  if (
    lower.includes("operat") ||
    lower.includes("logistics") ||
    lower.includes("supply chain")
  ) {
    return "Operations";
  }

  if (lower.includes("legal") || lower.includes("complian")) {
    return "Legal";
  }

  if (lower.includes("admin") || lower.includes("facilit")) {
    return "Administration";
  }

  if (
    lower.includes("support") ||
    lower.includes("customer") ||
    lower.includes("helpdesk")
  ) {
    return "Customer Support";
  }

  const direct = PREDEFINED_DEPARTMENTS.find((d) => d.toLowerCase() === lower);
  return direct || null;
}

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
  providedIn: "root",
})
export class EmployeeAnalyticsServices {
  private http = inject(HttpClient);
  //private apiUrl = 'http://localhost:5239/api/Employees';

  private apiUrl = window.location.origin.includes("localhost")
    ? "http://localhost:5239/api/Employees"
    : "/api"; // or your production  backend domain: 'https://your-api.azurewebsites.net/api'

  getEmployeeByDepartment(): Observable<DepartmentStatistic[]> {
    return this.getAllEmployees().pipe(
      map((employees) => {
        const departmentCounts = new Map<string, number>();

        employees.forEach((employee) => {
          const department = normalizeDepartment(employee.department);

          // Only add predefined departments into charts
          if (!department || !PREDEFINED_DEPARTMENTS.includes(department)) {
            return;
          }

          const currentCount = departmentCounts.get(department) || 0;
          departmentCounts.set(department, currentCount + 1);
        });

        return Array.from(departmentCounts, ([department, employeeCount]) => ({
          department,
          employeeCount,
        })).sort((a, b) => b.employeeCount - a.employeeCount);
      }),
    );
  }

  getAllEmployees(): Observable<Employee[]> {
    return this.http.get<Employee[]>(this.apiUrl);
  }

  getSalaryByDepartment(): Observable<DepartmentSalaryStatistic[]> {
    return this.getAllEmployees().pipe(
      map((employees) => {
        const salaryTotals = new Map<string, number>();

        employees.forEach((employee) => {
          const department = normalizeDepartment(employee.department);

          // Only add predefined departments into charts
          if (!department || !PREDEFINED_DEPARTMENTS.includes(department)) {
            return;
          }

          const currentTotal = salaryTotals.get(department) || 0;
          salaryTotals.set(department, currentTotal + employee.salary);
        });

        return Array.from(salaryTotals, ([department, totalSalary]) => ({
          department,
          totalSalary,
        })).sort((a, b) => b.totalSalary - a.totalSalary);
      }),
    );
  }

  getHiringTrends(): Observable<HiringTrendStatistic[]> {
    return this.getAllEmployees().pipe(
      map((employees) => {
        const hiringCounts = new Map<number, number>();

        employees.forEach((employee) => {
          const year = new Date(employee.hireDate).getFullYear();
          if (isNaN(year)) return;
          const currentCount = hiringCounts.get(year) || 0;
          hiringCounts.set(year, currentCount + 1);
        });

        return Array.from(hiringCounts, ([year, employeeCount]) => ({
          year,
          employeeCount,
        })).sort((a, b) => a.year - b.year);
      }),
    );
  }
}
