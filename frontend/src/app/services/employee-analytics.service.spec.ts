import { TestBed } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';

import {
  EmployeeAnalyticsServices,
  PREDEFINED_DEPARTMENTS,
  normalizeDepartment
} from './employee-analytics.service';
import { Employee } from './analytics-api.service';

describe('EmployeeAnalyticsServices', () => {
  let service: EmployeeAnalyticsServices;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [EmployeeAnalyticsServices]
    });
    service = TestBed.inject(EmployeeAnalyticsServices);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('normalizeDepartment', () => {
    it('should normalize HR variations to "Human Resources"', () => {
      expect(normalizeDepartment('HR')).toBe('Human Resources');
      expect(normalizeDepartment('hr')).toBe('Human Resources');
      expect(normalizeDepartment('H.R.')).toBe('Human Resources');
      expect(normalizeDepartment('HUman resources')).toBe('Human Resources');
      expect(normalizeDepartment('Human Resources')).toBe('Human Resources');
      expect(normalizeDepartment('Human Resource')).toBe('Human Resources');
      expect(normalizeDepartment('HR Dept')).toBe('Human Resources');
      expect(normalizeDepartment('People Operations')).toBe('Human Resources');
    });

    it('should normalize IT variations to "Information Technology"', () => {
      expect(normalizeDepartment('IT')).toBe('Information Technology');
      expect(normalizeDepartment('it')).toBe('Information Technology');
      expect(normalizeDepartment('I.T.')).toBe('Information Technology');
      expect(normalizeDepartment('Information Technology')).toBe('Information Technology');
      expect(normalizeDepartment('Information Tech')).toBe('Information Technology');
      expect(normalizeDepartment('Info Tech')).toBe('Information Technology');
      expect(normalizeDepartment('InfoTech')).toBe('Information Technology');
      expect(normalizeDepartment('IT Department')).toBe('Information Technology');
    });

    it('should normalize other standard departments', () => {
      expect(normalizeDepartment('Finance')).toBe('Finance');
      expect(normalizeDepartment('Accounting')).toBe('Finance');
      expect(normalizeDepartment('Marketing')).toBe('Marketing');
      expect(normalizeDepartment('Mktg')).toBe('Marketing');
      expect(normalizeDepartment('Sales')).toBe('Sales');
      expect(normalizeDepartment('Engineering')).toBe('Engineering');
      expect(normalizeDepartment('Software Development')).toBe('Engineering');
      expect(normalizeDepartment('Operations')).toBe('Operations');
      expect(normalizeDepartment('Legal')).toBe('Legal');
      expect(normalizeDepartment('Admin')).toBe('Administration');
      expect(normalizeDepartment('Customer Support')).toBe('Customer Support');
      expect(normalizeDepartment('Support')).toBe('Customer Support');
    });

    it('should return null for invalid or non-predefined departments', () => {
      expect(normalizeDepartment('UnknownDeptXYZ')).toBeNull();
      expect(normalizeDepartment('')).toBeNull();
      expect(normalizeDepartment(null)).toBeNull();
      expect(normalizeDepartment(undefined)).toBeNull();
    });
  });

  describe('getEmployeeByDepartment', () => {
    it('should remove repeating sections like HR and HUman resources, and merge them into predefined department', (done) => {
      const mockEmployees: Employee[] = [
        { id: 1, employeeCode: 'E001', firstName: 'Alice', lastName: 'Smith', department: 'HR', jobTitle: 'Manager', salary: 60000, hireDate: '2022-01-01', importHistoryId: 1, importFileName: 'test.xlsx' },
        { id: 2, employeeCode: 'E002', firstName: 'Bob', lastName: 'Jones', department: 'HUman resources', jobTitle: 'Recruiter', salary: 50000, hireDate: '2022-02-01', importHistoryId: 1, importFileName: 'test.xlsx' },
        { id: 3, employeeCode: 'E003', firstName: 'Charlie', lastName: 'Brown', department: 'IT', jobTitle: 'Developer', salary: 80000, hireDate: '2022-03-01', importHistoryId: 1, importFileName: 'test.xlsx' },
        { id: 4, employeeCode: 'E004', firstName: 'Diana', lastName: 'Prince', department: 'Information Technology', jobTitle: 'Lead', salary: 90000, hireDate: '2022-04-01', importHistoryId: 1, importFileName: 'test.xlsx' },
        { id: 5, employeeCode: 'E005', firstName: 'Evan', lastName: 'Wright', department: 'Finance', jobTitle: 'Analyst', salary: 70000, hireDate: '2022-05-01', importHistoryId: 1, importFileName: 'test.xlsx' },
        { id: 6, employeeCode: 'E006', firstName: 'Fiona', lastName: 'Gallagher', department: 'InvalidDept', jobTitle: 'Misc', salary: 40000, hireDate: '2022-06-01', importHistoryId: 1, importFileName: 'test.xlsx' }
      ];

      service.getEmployeeByDepartment().subscribe(stats => {
        // Should only have Human Resources, Information Technology, and Finance (3 departments, no duplicates!)
        expect(stats.length).toBe(3);

        const hrStat = stats.find(s => s.department === 'Human Resources');
        expect(hrStat).toBeDefined();
        expect(hrStat?.employeeCount).toBe(2);

        const itStat = stats.find(s => s.department === 'Information Technology');
        expect(itStat).toBeDefined();
        expect(itStat?.employeeCount).toBe(2);

        const finStat = stats.find(s => s.department === 'Finance');
        expect(finStat).toBeDefined();
        expect(finStat?.employeeCount).toBe(1);

        // Invalid department should NOT be present
        expect(stats.find(s => s.department === 'InvalidDept')).toBeUndefined();

        done();
      });

      const req = httpMock.expectOne('http://localhost:5239/api/Employees');
      expect(req.request.method).toBe('GET');
      req.flush(mockEmployees);
    });
  });

  describe('getSalaryByDepartment', () => {
    it('should aggregate salaries into predefined departments without repeating departments', (done) => {
      const mockEmployees: Employee[] = [
        { id: 1, employeeCode: 'E001', firstName: 'Alice', lastName: 'Smith', department: 'HR', jobTitle: 'Manager', salary: 60000, hireDate: '2022-01-01', importHistoryId: 1, importFileName: 'test.xlsx' },
        { id: 2, employeeCode: 'E002', firstName: 'Bob', lastName: 'Jones', department: 'HUman resources', jobTitle: 'Recruiter', salary: 50000, hireDate: '2022-02-01', importHistoryId: 1, importFileName: 'test.xlsx' },
        { id: 3, employeeCode: 'E003', firstName: 'Charlie', lastName: 'Brown', department: 'IT', jobTitle: 'Developer', salary: 80000, hireDate: '2022-03-01', importHistoryId: 1, importFileName: 'test.xlsx' },
        { id: 4, employeeCode: 'E004', firstName: 'Diana', lastName: 'Prince', department: 'Information Technology', jobTitle: 'Lead', salary: 90000, hireDate: '2022-04-01', importHistoryId: 1, importFileName: 'test.xlsx' }
      ];

      service.getSalaryByDepartment().subscribe(stats => {
        expect(stats.length).toBe(2);

        const hrSalary = stats.find(s => s.department === 'Human Resources');
        expect(hrSalary?.totalSalary).toBe(110000); // 60000 + 50000

        const itSalary = stats.find(s => s.department === 'Information Technology');
        expect(itSalary?.totalSalary).toBe(170000); // 80000 + 90000

        done();
      });

      const req = httpMock.expectOne('http://localhost:5239/api/Employees');
      expect(req.request.method).toBe('GET');
      req.flush(mockEmployees);
    });
  });
});
