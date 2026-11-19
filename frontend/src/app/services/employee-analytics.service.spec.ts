import { TestBed } from '@angular/core/testing';

import { EmployeeAnalyticsService } from './employee-analytics.service';

describe('EmployeeAnalyticsService', () => {
  let service: EmployeeAnalyticsService;

  beforeEach(() => {
    TestBed.configureTestingModule({});
    service = TestBed.inject(EmployeeAnalyticsService);
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });
});
